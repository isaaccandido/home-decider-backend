FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
RUN useradd -u 10001 -m appuser
USER appuser
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["HomeDecider.Api.csproj", "./"]
RUN dotnet restore "HomeDecider.Api.csproj"

COPY . .
RUN dotnet build "HomeDecider.Api.csproj" -c ${BUILD_CONFIGURATION} -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "HomeDecider.Api.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HomeDecider.Api.dll"]
