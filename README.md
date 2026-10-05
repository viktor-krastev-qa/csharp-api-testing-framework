# C# API Testing Framework

A QA Automation portfolio project with C#, NUnit, HttpClient and a bundled ASP.NET Core TaskBoard API. Tests issue real HTTP requests through a loopback Kestrel server, not mocked HttpClient responses. Each test starts a new application and isolated in-memory store.

## Quick start
Requires .NET 9 SDK. No browser, database, Docker or API keys are required. NuGet restore needs internet access.
```powershell
git clone https://github.com/viktor-krastev-qa/csharp-api-testing-framework.git
cd csharp-api-testing-framework
dotnet restore tests/ApiTests/ApiTests.csproj
dotnet test tests/ApiTests/ApiTests.csproj --logger "trx;LogFileName=api-tests.trx" --results-directory TestResults
```
If already cloned, skip clone/cd. The fixture hosts the API automatically on an available loopback port and stops it after each case.

Generate HTML from actual TRX results (Python 3 required):
```powershell
py -3.13 scripts/trx_to_html.py --input TestResults/api-tests.trx --output TestResults/report.html
Start-Process TestResults/report.html
```

## Coverage
Create/read/update/delete, 201 Location header, default values, JSON content type, exact persisted fields, empty list, boundary validation, incorrect types, unknown/duplicate JSON fields, malformed JSON, unsupported media type, case-insensitive title conflicts, missing resources, invalid route ID, filtering/search, no mutation on rejected requests and a full CRUD lifecycle.

## API contract
| Method | Endpoint | Success |
|---|---|---|
| GET | /health | 200 healthy |
| GET | /api/tasks | 200 array |
| GET | /api/tasks/{guid} | 200 task |
| POST | /api/tasks | 201 task and Location |
| PUT | /api/tasks/{guid} | 200 replaced task |
| DELETE | /api/tasks/{guid} | 204 empty body |

Request fields: title (required, trimmed 1..70), description (default empty, max 400), priority (low/normal/high, default normal), isCompleted (boolean, default false). Unknown fields and duplicate JSON properties are rejected. PUT replaces all editable fields using the same defaults; it is not PATCH. Titles must be unique ignoring case. IDs are server-generated GUIDs.

GET collection filters: search (case-insensitive title substring), completed (boolean), priority (allowed value). Filters combine with AND. Unknown query parameters are ignored. Collection order is not a public sorting contract.

JSON errors: 400 validation_error / invalid_json / invalid_query, 404 not_found for missing valid GUID, 409 duplicate_title, 415 unsupported_media_type. Route misses such as invalid GUID strings return ASP.NET's 404 and are not promised a JSON error body. Input validation precedes resource lookup on PUT.

## Optional manual exploration
In a separate terminal:
```powershell
dotnet run --project src/TaskBoard.Api/TaskBoard.Api.csproj -- --urls http://localhost:5080
```
Then use PowerShell or Postman:
```powershell
Invoke-RestMethod http://localhost:5080/health
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/tasks -ContentType "application/json" -Body '{"title":"Explore API","priority":"high"}'
Invoke-RestMethod http://localhost:5080/api/tasks
```
This manual server is independent of the test servers. Stopping it deletes its data.

## Structure and evidence
`src/TaskBoard.Api/`: actual local API, parser and locked in-memory store. `tests/ApiTests/Infrastructure/`: host/client lifecycle and synthetic request/response evidence on failures. `tests/ApiTests/Tests/`: NUnit contract assertions. `scripts/`: TRX-to-HTML converter. GitHub Actions runs tests on Ubuntu and uploads TRX/failure evidence. Results are ignored until intentionally copied into examples.

39 tests passed in the Linux build environment with .NET SDK 9.0.318. [Actual HTML report](examples/build/report.html) (download/open), [TRX evidence](examples/build/api-tests.trx).

Validation status: [docs/VALIDATION.md](docs/VALIDATION.md). Local Windows execution with .NET 9: 39 passed, 0 failed, 0 skipped.

- [Windows HTML report](examples/local/report.html) — download and open.
- [Windows TRX evidence](examples/local/api-tests.trx).

GitHub Actions validation: passed.

## Limits
The API is a controlled demo authored alongside its tests, not an independent production service. No authentication, authorization, database, persistence, pagination, TLS deployment, performance/SLA testing or third-party integration is implemented. The store is isolated per application instance and guarded with a lock, but concurrent-load correctness is not claimed by these sequential tests. Request/response evidence uses synthetic data. No real credentials or personal records are needed.

Targets the user's .NET 9 installation. Support ends November 10, 2026; migrate both projects and CI to .NET 10 and rerun tests before that date. Source: https://dotnet.microsoft.com/en-us/platform/support/policy

## License
MIT — see [LICENSE](LICENSE).
