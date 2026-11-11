# LibraryApp
Библиотечная система (ASP.NET Core + Blazor WASM + PostgreSQL).
## Стек
- **Core** — сущности, DTO, политики валидации (SSOT)
- **Infrastructure** — EF Core (Npgsql), репозитории, Unit of Work
- **Application** — команды, валидаторы, сервисы
- **WebAPI** — REST API (Hybrid Minimal API)
- **BlazorWeb** — BFF (Identity + JWT для WebAPI)
- **BlazorWeb.Client** — WASM-клиент
## Запуск через Docker
1. Скопировать `.env.example` в `.env` и задать значения:

```
JWT_SECRET_KEY=<случайная строка, минимум 32 символа>  
ADMIN_PASSWORD=<пароль админа, минимум 8 символов с заглавной/строчной/цифрой/спецсимволом>
```

2. Запустить:

```
docker-compose up --build
```


3. Открыть:
- **UI**: http://localhost:5271
- **Swagger WebAPI**: http://localhost:5096/swagger
- **Логин**: `admin@library.local` / пароль из `ADMIN_PASSWORD`
При первом старте миграции применяются автоматически (Development), создаётся seed-админ.
## Локальный запуск (без Docker)
1. Postgres:

```
docker-compose up -d postgres
```

2. Секреты в user-secrets:

```
dotnet user-secrets set "ApplicationSettings:Jwt:SecretKey" "<32+ символа>" --project WebAPI  
dotnet user-secrets set "ApplicationSettings:Jwt:SecretKey" "<32+ символа>" --project BlazorWeb/BlazorWeb  
dotnet user-secrets set "Seed:AdminPassword" "<пароль>" --project BlazorWeb/BlazorWeb
```

3. Запустить `WebAPI` и `BlazorWeb` (в двух терминалах, F5 в Visual Studio или `dotnet run`).
## Тесты

```
dotnet test
```

## Статус
Проект в активной разработке. Аудит команд, проверки инвариантов БД, Identity.
Раздел «Production» (нагрузочное, бэкапы, CI/CD, локализация) — в бэклоге.
