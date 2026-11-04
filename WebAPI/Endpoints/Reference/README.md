# Reference: альтернативные подходы к API

Здесь лежат два подхода, которые **не используются** в текущем приложении:

- **BaseAPI** — `ControllerBase` + `[Route]` + `[HttpPost]` + `BaseApiController.ExecuteCommand/ExecuteQuery`
- **MinimalAPI** — `app.MapGroup(...).MapPost(...)` с ручным резолвом команд через `MinimalApiController`

Текущий подход — **HybridAPI** (`Endpoints/HybridAPI/`): методы класса с `[HttpPost]` и DTO в параметрах, резолвятся через `RequestDelegateFactory`, Swagger видит параметры и схемы. См. `HybridEndpointExtensions.cs`.

## Зачем оставлено

Как референс для будущих проектов: сравнить три подхода, посмотреть, как выглядел код до перехода на Hybrid.

## Правила

- **Не мапить в `Program.cs`.** `MapControllers()` и `MapEndpoints()` — закомментированы.
- **Не удалять при рефакторинге** контрактов (DTO, `ICommand`, `[Authorize]`). Если код перестал компилироваться — значит, референс устарел и его нужно обновить, а не удалить. Компиляция — единственная защита от гниения.
- **Не покрывается тестами** и не участвует в CI-проверке API-совместимости.

## Если понадобится `TreatWarningsAsErrors`

На файлах `Reference/` есть `[Obsolete]`-предупреждения (`CS0618`). Локально гасим их через `#pragma warning disable CS0618` в начале файлов, **не** через глобальный `NoWarn` — иначе зашумим предупреждения по всему `WebAPI`.