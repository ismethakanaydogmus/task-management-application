# Görev Yönetim Uygulaması (Task Manager)

Full stack öğrenme ve şablon projesi.

| Katman | Teknoloji |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10, Controller tabanlı) |
| Veritabanı | PostgreSQL (Docker) + Entity Framework Core |
| Frontend | React + Vite + TypeScript |
| Kimlik doğrulama | JWT |

## Klasör yapısı

```
gorev_yonetim_uygulamasi/
├── backend/              # .NET solution
│   ├── TaskManager.slnx
│   ├── src/
│   │   ├── TaskManager.Domain/          # Entity'ler (hiçbir şeye bağımlı değil)
│   │   ├── TaskManager.Application/     # Service'ler, DTO'lar, interface'ler
│   │   ├── TaskManager.Infrastructure/  # EF Core, PostgreSQL, migration'lar
│   │   └── TaskManager.Api/             # Controller'lar, Program.cs (composition root)
│   └── tests/
│       ├── TaskManager.UnitTests/         # xUnit, fake bağımlılıklar, FakeTimeProvider
│       └── TaskManager.IntegrationTests/  # WebApplicationFactory + Testcontainers (gerçek PostgreSQL)
├── frontend/             # React uygulaması (Aşama 5)
├── docs/                 # Her aşamanın ders notu (neden/alternatif/sorun/mülakat)
└── README.md
```

## İlk kurulum (yeni bir bilgisayarda bir kez)

Gerekenler: .NET 10 SDK, Docker Desktop, Node.js (frontend için).

```bash
# 1) Ortam değişkenleri: örnek dosyayı kopyala, POSTGRES_PASSWORD'ü değiştir
cp .env.example .env

# 2) Veritabanını başlat (Docker'da PostgreSQL, bilgisayarda 5433 portu)
docker compose up -d

# 3) Backend araçları (dotnet-ef) ve connection string (.env'deki değerlerle aynı olmalı)
cd backend
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Port=5433;Database=taskmanager;Username=taskmanager;Password=<.env'deki şifre>" \
  --project src/TaskManager.Api

# 4) JWT imza anahtarı (GİZLİ, en az 32 bayt). Rastgele üret:
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 64)" --project src/TaskManager.Api
#    (openssl yoksa PowerShell: [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64)))

# 5) Tabloları oluştur
dotnet ef database update --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api
```

## Çalıştırma

```bash
# Backend (terminal 1)
docker compose up -d          # veritabanı çalışmıyorsa
cd backend
dotnet run --project src/TaskManager.Api
# -> http://localhost:5263/api/tasks

# Frontend (terminal 2)
cd frontend
npm install                   # ilk seferde
npm run dev
# -> http://localhost:5173
```

## Testler

```bash
# Backend: unit (~0.3 sn) + integration (Docker açık olmalı; geçici PostgreSQL container'ı açar, ~5 sn)
cd backend
dotnet test
dotnet test tests/TaskManager.UnitTests          # sadece unit testler (Docker gerekmez)

# Frontend: Vitest + Testing Library
cd frontend
npm test              # bir kez çalıştır
npm run test:watch    # dosya değiştikçe yeniden çalıştır
```

## Sık kullanılan komutlar

```bash
# Model değişti → yeni migration
#   --project         : migration'ların ve DbContext'in olduğu proje
#   --startup-project : configuration'ı (user-secrets) ve DI'ı kuran çalıştırılabilir proje
dotnet ef migrations add <Isim> --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api

# Veritabanına psql ile bağlan
docker exec -it taskmanager-db psql -U taskmanager -d taskmanager

# Veritabanını tamamen sıfırla (TÜM VERİ SİLİNİR)
docker compose down -v && docker compose up -d && dotnet ef database update --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api
```

## Ders notları

| # | Konu | Durum |
|---|---|---|
| 00 | [Proje iskeleti](docs/00-proje-iskeleti.md) | ✅ |
| 01 | [İlk Web API (Task CRUD)](docs/01-ilk-web-api.md) | ✅ |
| 02 | [PostgreSQL + EF Core](docs/02-postgresql-ef-core.md) | ✅ |
| 03 | [Katmanlı mimari, DTO](docs/03-katmanli-mimari.md) | ✅ |
| 04 | [Validasyon ve hata yönetimi](docs/04-validasyon-hata-yonetimi.md) | ✅ |
| 05 | [React + Vite + TypeScript](docs/05-react-vite-typescript.md) | ✅ |
| 06 | [Frontend ↔ Backend (CORS, TanStack Query)](docs/06-frontend-backend-baglantisi.md) | ✅ |
| 07 | [JWT kimlik doğrulama](docs/07-jwt-kimlik-dogrulama.md) | ✅ |
| 08 | [Kullanıcıya özel veri, filtreleme, sayfalama](docs/08-kullaniciya-ozel-veri-sayfalama.md) | ✅ |
| 09 | [Test](docs/09-test.md) | ✅ |
| 10 | Docker ve deploy | ⏳ |
