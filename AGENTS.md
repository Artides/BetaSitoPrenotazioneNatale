# Repository Guidelines

## Project Structure & Module Organization

This repository provides a static Italian appointment-booking site and a C# .NET 10 backend using Azure Functions v4 isolated.

- Root HTML files: landing, booking, confirmation, and privacy pages.
- `assets/css/` and `assets/js/`: styles, public configuration, ES modules, API calls, and booking interactions.
- `api/Booking.Api/`: Functions entry points, configuration, models, and application services.
- `api/Booking.LocalHost/`: development host sharing API sources, with demo and live modes.
- `api/Booking.Api.Tests/`: automated xUnit tests; `api/Booking.IntegrationTests/`: real Calendar verification runner.
- `scripts/`: frontend server, secrets setup, and restricted-environment test runner. `siteExamples/` contains design references.

## Build, Test, and Development Commands

Run these commands from the repository root:

- `python3 scripts/serve-site.py`: serve the frontend at `http://localhost:8080`; use HTTP for ES modules.
- `ASPNETCORE_ENVIRONMENT=Development dotnet run --project api/Booking.LocalHost -- --demo`: start an in-memory backend on port 7071 without credentials.
- `dotnet restore api/Booking.Api.sln`: restore backend dependencies.
- `dotnet build api/Booking.Api/Booking.Api.csproj`: build the Azure Functions project.
- `dotnet test api/Booking.Api.Tests/Booking.Api.Tests.csproj`: run automated tests.
- `./scripts/test-in-process.sh`: build and run the same tests without VSTest sockets.

The frontend requires no npm installation or build step.

## Coding Style & Naming Conventions

Follow surrounding code: two-space JavaScript indentation and four-space C# block indentation. Use camelCase for JavaScript identifiers, PascalCase for C# types and methods, and descriptive `*Service`, `*Options`, and `*Tests` names. Preserve relative frontend URLs and CSS variables in `assets/css/variables.css`. No formatter or linter configuration is checked in; avoid unrelated reformatting.

## Testing Guidelines

Use xUnit `[Fact]` and `[Theory]` methods with descriptive PascalCase behavior names, such as `SameKeyDifferentPayloadConflicts`. Cover booking validation, timezone handling, availability buffers, and idempotent retries. Automated tests use simulated providers; no numeric coverage threshold is configured. Manually verify frontend changes in demo mode. Live mode and the integration runner write real Calendar events; use them only when explicitly intended.

## Commit & Pull Request Guidelines

History uses short plain-language subjects, such as `added api url`; no strict commit prefix convention is established. Keep commits focused. PRs should explain behavior changes, list validation performed, link relevant issues, and include screenshots for visible changes.

## Security & Configuration

Keep credentials in .NET User Secrets or Azure Application Settings. Never place secrets in `assets/js/config.js` or commit `local.settings.json`. See `README.md` for configuration and booking rules.
