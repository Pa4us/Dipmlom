# Нагрузочное тестирование (NBomber)

Сценарии для нагрузочного тестирования ключевых endpoint-ов Web API.
Реализованы на NBomber — нагрузочный фреймворк для .NET, скрипты пишутся
на C# и запускаются в одной среде с проектом.

## Сценарии

| Файл | Endpoint | Роль | Что эмулирует |
|---|---|---|---|
| `AuthLoginLoadTests.cs` | `POST /api/auth/login` | по кругу 5 ролей | Массовый вход пользователей |
| `RepairRequestsLoadTests.cs` | `GET /api/repairrequests` | Мастер | Одновременный просмотр списка заявок |
| `InspectionsLoadTests.cs` | `GET /api/inspections` | Воспитатель | Одновременный просмотр результатов проверок |

Профиль нагрузки одинаков для всех сценариев:
- ramp-up до 20 RPS за 30 секунд,
- удержание 20 RPS в течение 1 минуты.

Пороги (Assert в конце каждого теста):
- доля ошибок < 1 %,
- p95 latency < 800 мс.

## Подготовка

1. Запустить Web API: `dotnet run --project WebAPI`.
2. Засеять тестовых пользователей в БД (`student_test`, `inspector_test`,
   `educator_test`, `manager_test`, `mechanic_test` с паролем `test1234`).
   Скрипт — `seed_data.sql` в корне репозитория.

## Запуск

Нагрузочные тесты по умолчанию **пропускаются** в обычном прогоне `dotnet test`,
иначе они утяжелят CI. Чтобы запустить, выставить переменную окружения
`RUN_LOAD_TESTS=1`:

```powershell
# Все три сценария
$env:RUN_LOAD_TESTS = "1"
dotnet test UnitTests --filter "FullyQualifiedName~Load" -c Release

# Только авторизация
$env:RUN_LOAD_TESTS = "1"
dotnet test UnitTests --filter "FullyQualifiedName~AuthLoginLoadTests" -c Release
```

Адрес API при необходимости меняется через переменную:

```powershell
$env:LOADTEST_BASE_URL = "http://server.local:5229"
```

## Отчёты

NBomber сам пишет HTML/CSV/MD-отчёты в подкаталог `./reports/` рядом
с запускаемой сборкой по окончании прогона. Сводный summary также
выводится в консоль: средние и перцентили latency, RPS, число запросов
и ошибок.
