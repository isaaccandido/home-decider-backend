pipeline {
    agent any

    parameters {
        choice(
            name: 'CONTAINER_ENGINE',
            choices: ['podman', 'docker'],
            description: 'Container engine to use'
        )

        booleanParam(
            name: 'RUN_DB_DEPLOY',
            defaultValue: false,
            description: 'Deploy or refresh the shared PostgreSQL stack before deploying the app'
        )
    }

    environment {
        APP_NAME                  = 'HomeDecider'
        PROJECT_DIR               = 'HomeDecider.Api'
        API_PROJECT               = 'HomeDecider.Api/HomeDecider.Api.csproj'
        SHARED_DB_CONTAINER       = 'shared-postgres'
        DB_NAME_BASE              = 'homedecider'

        BACKEND_CONTAINER_PORT    = '8080'
        BACKEND_HOST_PORT         = '8010'

        DB_COMPOSE_PROJECT        = 'home-decider-db'
    }

    stages {
        stage('Restore & Publish') {
            steps {
                sh '''
                    set +x
                    set -eu

                    dotnet restore "HomeDecider.slnx"
                    dotnet publish "${API_PROJECT}" -c Release -o out
                '''
            }
        }

        stage('Deploy Shared DB') {
            when {
                expression { params.RUN_DB_DEPLOY }
            }
            steps {
                withFolderProperties {
                    sh '''
                        set +x
                        set -eu

                        cd "${PROJECT_DIR}"

                        : "${DB_PORT:?DB_PORT is required}"
                        : "${DB_USERNAME:?DB_USERNAME is required}"
                        : "${DB_PASSWORD:?DB_PASSWORD is required}"

                        cat > jenkins.db.env <<EOL
DB_PORT=${DB_PORT}
DB_USERNAME=${DB_USERNAME}
DB_PASSWORD=${DB_PASSWORD}
EOL

                        echo "===== SHARED DB DEPLOY ====="
                        echo "Container engine: ${CONTAINER_ENGINE}"
                        echo "DB compose project: ${DB_COMPOSE_PROJECT}"
                        echo "DB port: ${DB_PORT}"
                        echo "============================"

                        if [ "${CONTAINER_ENGINE}" = "docker" ]; then
                            docker compose -p "${DB_COMPOSE_PROJECT}" -f compose.db.yaml --env-file jenkins.db.env up -d
                            docker container inspect "${SHARED_DB_CONTAINER}" >/dev/null 2>&1 || {
                                echo "Shared DB container '${SHARED_DB_CONTAINER}' was not created."
                                exit 1
                            }
                        else
                            sudo podman compose -p "${DB_COMPOSE_PROJECT}" -f compose.db.yaml --env-file jenkins.db.env up -d
                            sudo podman container inspect "${SHARED_DB_CONTAINER}" >/dev/null 2>&1 || {
                                echo "Shared DB container '${SHARED_DB_CONTAINER}' was not created."
                                exit 1
                            }
                        fi
                    '''
                }
            }
        }

        stage('Build & Deploy') {
            steps {
                script {
                    def rawGitBranch = (env.GIT_BRANCH ?: '').trim()

                    if (!rawGitBranch) {
                        error("GIT_BRANCH is not available. This pipeline expects Jenkins Git plugin branch metadata, e.g. origin/main.")
                    }

                    def gitBranch = rawGitBranch
                        .replaceFirst(/^origin\//, '')
                        .replaceFirst(/^refs\/heads\//, '')

                    if (gitBranch != 'main') {
                        error("Unsupported branch '${gitBranch}' (raw: '${rawGitBranch}'). Only 'main' is supported.")
                    }

                    withEnv([
                        "GIT_BRANCH_EFFECTIVE=${gitBranch}",
                        "HOST_PORT=${env.BACKEND_HOST_PORT}",
                        "COMPOSE_PROJECT_NAME=home-decider-api",
                        "APP_CONTAINER_NAME=home-decider-api",
                        "APP_POD_NAME=home-decider-api"
                    ]) {
                        withFolderProperties {
                            sh '''
                                set +x
                                set -eu

                                cd "${PROJECT_DIR}"

                                : "${DB_HOST:?DB_HOST is required}"
                                : "${DB_PORT:?DB_PORT is required}"
                                : "${DB_USERNAME:?DB_USERNAME is required}"
                                : "${DB_PASSWORD:?DB_PASSWORD is required}"
                                : "${JWT_SECRET:?JWT_SECRET is required}"
                                : "${DEFAULT_ADMIN_USERNAME:?DEFAULT_ADMIN_USERNAME is required}"
                                : "${DEFAULT_ADMIN_PASSWORD:?DEFAULT_ADMIN_PASSWORD is required}"
                                : "${ALLOWED_ORIGIN:?ALLOWED_ORIGIN is required}"

                                cat > jenkins.env <<EOL
COMPOSE_PROJECT_NAME=${COMPOSE_PROJECT_NAME}
APP_CONTAINER_NAME=${APP_CONTAINER_NAME}
HOST_PORT=${HOST_PORT}
DB_HOST=${DB_HOST}
DB_PORT=${DB_PORT}
DB_NAME=${DB_NAME_BASE}
DB_USERNAME=${DB_USERNAME}
DB_PASSWORD=${DB_PASSWORD}
JWT_SECRET=${JWT_SECRET}
DEFAULT_ADMIN_USERNAME=${DEFAULT_ADMIN_USERNAME}
DEFAULT_ADMIN_PASSWORD=${DEFAULT_ADMIN_PASSWORD}
ALLOWED_ORIGIN=${ALLOWED_ORIGIN}
EOL

                                echo "===== DEPLOYMENT INFO ====="
                                echo "App name: ${APP_NAME}"
                                echo "Git branch: ${GIT_BRANCH_EFFECTIVE}"
                                echo "Compose project: ${COMPOSE_PROJECT_NAME}"
                                echo "Container engine: ${CONTAINER_ENGINE}"
                                echo "App container name: ${APP_CONTAINER_NAME}"
                                echo "Host port: ${HOST_PORT}"
                                echo "Container port: ${BACKEND_CONTAINER_PORT}"
                                echo "DB host: ${DB_HOST}"
                                echo "DB name: ${DB_NAME_BASE}"
                                echo "Allowed origin: ${ALLOWED_ORIGIN}"
                                echo "==========================="

                                if [ "${CONTAINER_ENGINE}" = "docker" ]; then
                                    docker container inspect "${SHARED_DB_CONTAINER}" >/dev/null 2>&1 || {
                                        echo "Shared DB container '${SHARED_DB_CONTAINER}' not found. Run with RUN_DB_DEPLOY checked, or deploy compose.db.yaml separately first."
                                        exit 1
                                    }

                                    docker exec -i "${SHARED_DB_CONTAINER}" \
                                        psql -U "${DB_USERNAME}" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME_BASE}'" \
                                        | grep -q 1 \
                                        || docker exec -i "${SHARED_DB_CONTAINER}" \
                                            psql -U "${DB_USERNAME}" -d postgres -c "CREATE DATABASE \\"${DB_NAME_BASE}\\""
                                else
                                    sudo podman container inspect "${SHARED_DB_CONTAINER}" >/dev/null 2>&1 || {
                                        echo "Shared DB container '${SHARED_DB_CONTAINER}' not found. Run with RUN_DB_DEPLOY checked, or deploy compose.db.yaml separately first."
                                        exit 1
                                    }

                                    sudo podman exec -i "${SHARED_DB_CONTAINER}" \
                                        psql -U "${DB_USERNAME}" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME_BASE}'" \
                                        | grep -q 1 \
                                        || sudo podman exec -i "${SHARED_DB_CONTAINER}" \
                                            psql -U "${DB_USERNAME}" -d postgres -c "CREATE DATABASE \\"${DB_NAME_BASE}\\""
                                fi

                                echo "===== EF CORE DATABASE UPDATE ====="
                                dotnet tool restore
                                EF_CONN=$(printf 'Host=%s;Port=%s;Database=%s;Username=%s;Password=%s' \
                                    "${DB_HOST}" "${DB_PORT}" "${DB_NAME_BASE}" "${DB_USERNAME}" "${DB_PASSWORD}")
                                dotnet ef database update \
                                    --project "HomeDecider.Api.csproj" \
                                    --startup-project "HomeDecider.Api.csproj" \
                                    --connection "${EF_CONN}"
                                echo "===== EF CORE DATABASE UPDATE OK ====="

                                if [ "${CONTAINER_ENGINE}" = "docker" ]; then
                                    echo "Removing old container if it exists: ${APP_CONTAINER_NAME}"
                                    docker rm -f "${APP_CONTAINER_NAME}" >/dev/null 2>&1 || true

                                    docker compose -p "${COMPOSE_PROJECT_NAME}" -f compose.deploy.yaml --env-file jenkins.env down || true
                                    docker compose -p "${COMPOSE_PROJECT_NAME}" -f compose.deploy.yaml --env-file jenkins.env up -d --build
                                else
                                    echo "Removing old container if it exists: ${APP_CONTAINER_NAME}"
                                    sudo podman rm -f "${APP_CONTAINER_NAME}" >/dev/null 2>&1 || true

                                    echo "Removing old pod if it exists: ${APP_POD_NAME}"
                                    sudo podman pod rm -f "${APP_POD_NAME}" >/dev/null 2>&1 || true

                                    sudo podman compose -p "${COMPOSE_PROJECT_NAME}" -f compose.deploy.yaml --env-file jenkins.env down || true
                                    sudo podman compose -p "${COMPOSE_PROJECT_NAME}" -f compose.deploy.yaml --env-file jenkins.env up -d --build
                                fi
                            '''
                        }
                    }
                }
            }
        }
    }

    post {
        always {
            cleanWs()
        }
        success {
            echo '✅ HomeDecider pipeline completed successfully'
        }
        failure {
            echo '❌ HomeDecider pipeline failed'
        }
    }
}
