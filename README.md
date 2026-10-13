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
│   ├── src/TaskManager.Api/
│   └── tests/            # (Aşama 9)
├── frontend/             # React uygulaması (Aşama 5)
├── docs/                 # Her aşamanın ders notu (neden/alternatif/sorun/mülakat)
└── README.md
```

## Çalıştırma

```bash
cd backend
dotnet run --project src/TaskManager.Api
# -> http://localhost:5263/openapi/v1.json
```

## Ders notları

| # | Konu | Durum |
|---|---|---|
| 00 | [Proje iskeleti](docs/00-proje-iskeleti.md) | ✅ |
| 01 | İlk Web API (Task CRUD) | ⏳ |
| 02 | PostgreSQL + EF Core | ⏳ |
| 03 | Katmanlı mimari, DTO | ⏳ |
| 04 | Validasyon ve hata yönetimi | ⏳ |
| 05 | React + Vite + TypeScript | ⏳ |
| 06 | Frontend ↔ Backend (CORS, TanStack Query) | ⏳ |
| 07 | JWT kimlik doğrulama | ⏳ |
| 08 | Kullanıcıya özel veri, filtreleme, sayfalama | ⏳ |
| 09 | Test | ⏳ |
| 10 | Docker ve deploy | ⏳ |
