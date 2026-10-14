# =============================================================================
# Backend Dockerfile — MULTI-STAGE BUILD
#
#   build     : SDK (~900 MB) ile restore + publish            → son image'a GİRMEZ
#   migrator  : EF migration bundle (tek çalıştırılabilir dosya) → deploy'da BİR KEZ çalışıp çıkar
#   final     : sadece ASP.NET runtime (~230 MB) + derlenmiş dll'ler → canlıda çalışan image
#
# Build:  docker build -t taskmanager-api --target final .
# (Bağlam = backend/ klasörü; .dockerignore bin/obj/tests'i dışarıda bırakır.)
# =============================================================================

# Sürümler SABİT ("latest" değil): build bugün ve 6 ay sonra aynı sonucu versin.
ARG DOTNET_VERSION=10.0

# ----------------------------------------------------------------------------- build
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# KATMAN ÖNBELLEĞİ: Önce SADECE proje dosyalarını kopyala ve restore et.
# Kaynak kod değişince bu katmanlar önbellekten gelir; NuGet paketleri TEKRAR İNDİRİLMEZ.
# (Tüm kodu önce kopyalasaydık, her satır değişikliğinde restore baştan çalışırdı.)
COPY dotnet-tools.json ./
COPY src/TaskManager.Domain/TaskManager.Domain.csproj                 src/TaskManager.Domain/
COPY src/TaskManager.Application/TaskManager.Application.csproj       src/TaskManager.Application/
COPY src/TaskManager.Infrastructure/TaskManager.Infrastructure.csproj src/TaskManager.Infrastructure/
COPY src/TaskManager.Api/TaskManager.Api.csproj                       src/TaskManager.Api/
RUN dotnet restore src/TaskManager.Api/TaskManager.Api.csproj

# Şimdi kaynak kod
COPY src/ src/
RUN dotnet publish src/TaskManager.Api/TaskManager.Api.csproj \
      --configuration Release \
      --no-restore \
      --output /app/publish \
      /p:UseAppHost=false

# ----------------------------------------------------------------------------- migrator (bundle üretimi)
FROM build AS migrator-build
RUN dotnet tool restore
# Bundle self-contained (içinde .NET runtime'ı da var) → belirli bir platform için derlenir.
# İlk restore platform belirtilmeden yapıldığı için linux-x64 paket bilgisi yok:
#   "NETSDK1047: Assets file doesn't have a target for 'net10.0/linux-x64'"
# Bu yüzden bu platform için ayrıca restore. (ARM sunucu, ör. Apple Silicon/Graviton, için linux-arm64.)
RUN dotnet restore src/TaskManager.Api/TaskManager.Api.csproj --runtime linux-x64
# Migration BUNDLE: migration'ları + DbContext'i içeren TEK bir çalıştırılabilir dosya.
# Canlı sunucuda SDK/dotnet-ef/kaynak kod olmadan "./efbundle" ile migration uygulanır.
#
# ConnectionStrings__Default SAHTE: EF, DbContext'i nasıl kuracağını öğrenmek için Program.cs'i çalıştırıyor
# (servisleri kaydediyor ama uygulamayı başlatmıyor). AddInfrastructure, connection string yoksa bilerek hata
# fırlatıyor (fail fast). Bundle OLUŞTURMAK veritabanına bağlanmayı gerektirmez → gizli olmayan bir yer tutucu yeterli.
# Gerçek değer, bundle ÇALIŞIRKEN container'ın ortam değişkeninden okunur. (Image'a hiçbir secret girmez.)
RUN ConnectionStrings__Default="Host=build-time-placeholder" \
    dotnet ef migrations bundle \
      --project src/TaskManager.Infrastructure \
      --startup-project src/TaskManager.Api \
      --configuration Release \
      --runtime linux-x64 \
      --self-contained \
      --output /app/efbundle

# ----------------------------------------------------------------------------- migrator (çalışan image)
# runtime-deps: .NET'in kendisi bile yok, sadece işletim sistemi kütüphaneleri (bundle self-contained).
FROM mcr.microsoft.com/dotnet/runtime-deps:${DOTNET_VERSION} AS migrator
WORKDIR /app
COPY --from=migrator-build /app/efbundle .
USER $APP_UID
# Bağlantı bilgisi ortam değişkeninden (ConnectionStrings__Default) okunur; image'da secret YOK.
ENTRYPOINT ["./efbundle"]

# ----------------------------------------------------------------------------- final (API)
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app
COPY --from=build /app/publish .

# ROOT OLMAYAN KULLANICI: .NET 8+ image'larında hazır "app" kullanıcısı (APP_UID=1654) var.
# Root olarak çalışsaydı, uygulamadaki bir açık container içinde tam yetki demek olurdu.
USER $APP_UID

# .NET 8+ image'ları varsayılan olarak 8080'i dinler (ASPNETCORE_HTTP_PORTS=8080).
# 80 numaralı port root yetkisi ister; root olmayan kullanıcı için 8080.
EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
