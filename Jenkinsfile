pipeline {
    agent any

    parameters {
        choice(
            name: 'CONTAINER_ENGINE',
            choices: ['podman', 'docker'],
            description: 'Container engine to use'
        )
    }

    environment {
        APP_NAME               = 'HomeDecider'
        PROJECT_DIR            = 'HomeDecider.Api'
        API_PROJECT            = 'HomeDecider.Api/HomeDecider.Api.csproj'

        BACKEND_CONTAINER_PORT = '8080'
        BACKEND_HOST_PORT      = '8010'
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
                        "APP_CONTAINER_NAME=home-decider-api"
                    ]) {
                        withFolderProperties {
                            sh '''
                                set +x
                                set -eu

                                cd "${PROJECT_DIR}"

                                : "${DB_NAME:?DB_NAME is required}"
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
DB_NAME=${DB_NAME}
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
                                echo "DB name: ${DB_NAME}"
                                echo "Allowed origin: ${ALLOWED_ORIGIN}"
                                echo "==========================="

                                if [ "${CONTAINER_ENGINE}" = "docker" ]; then
                                    docker rm -f "${APP_CONTAINER_NAME}" >/dev/null 2>&1 || true
                                    docker compose -p "${COMPOSE_PROJECT_NAME}" -f compose.deploy.yaml --env-file jenkins.env down || true
                                    docker compose -p "${COMPOSE_PROJECT_NAME}" -f compose.deploy.yaml --env-file jenkins.env up -d --build
                                else
                                    sudo podman rm -f "${APP_CONTAINER_NAME}" >/dev/null 2>&1 || true
                                    sudo podman pod rm -f "pod_${COMPOSE_PROJECT_NAME}" >/dev/null 2>&1 || true
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
