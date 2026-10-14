# =============================================================================
# TEK CONTAINER image'ı (Azure Container Apps için): React + ASP.NET Core API birlikte.
#
#   web-build : Node ile React build → dist/
#   api-build : .NET SDK ile API publish
#   final     : ASP.NET runtime + API + wwwroot (React dosyaları)
#
# Build bağlamı = repo kökü (hem backend/ hem frontend/ gerekli):
#   docker build -t taskmanager-app .
#
# Not: docker-compose.prod.yml ayrı image'lar (nginx + api) kullanır; bu dosya bulut dağıtımı içindir.
# Migration image'ı için: backend/Dockerfile (--target migrator).
# =============================================================================

ARG DOTNET_VERSION=10.0

# ----------------------------------------------------------------------------- frontend
FROM node:24-alpine AS web-build
WORKDIR /web
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
# .env.production → VITE_API_URL boş → istekler "/api/..." (aynı origin)
RUN npm run build

# ----------------------------------------------------------------------------- backend
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS api-build
WORKDIR /src
COPY backend/src/TaskManager.Domain/TaskManager.Domain.csproj                 src/TaskManager.Domain/
COPY backend/src/TaskManager.Application/TaskManager.Application.csproj       src/TaskManager.Application/
COPY backend/src/TaskManager.Infrastructure/TaskManager.Infrastructure.csproj src/TaskManager.Infrastructure/
COPY backend/src/TaskManager.Api/TaskManager.Api.csproj                       src/TaskManager.Api/
RUN dotnet restore src/TaskManager.Api/TaskManager.Api.csproj
COPY backend/src/ src/
RUN dotnet publish src/TaskManager.Api/TaskManager.Api.csproj \
      --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ----------------------------------------------------------------------------- final
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
COPY --from=api-build /app/publish .
# React build'i wwwroot'a: SpaHosting bunu görünce statik dosyaları ve SPA fallback'i açar.
COPY --from=web-build /web/dist ./wwwroot

# Görüntüye kaynak kodun nereden geldiğini yaz (GitHub Container Registry image'ı repoya bağlar).
LABEL org.opencontainers.image.source="https://github.com/ismethakanaydogmus/gorev-yonetim-uygulamasi"

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
