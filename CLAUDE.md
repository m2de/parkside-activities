# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Running and checking

- `dotnet run --project Parkside.Seed` drops and rebuilds `parkside.db` at the repo root. The data covers today -3 to +10 days, so re-seed when the dashboard looks empty. Today's sessions are hand-planned in `SessionsForToday`.
- `npm run dev` from `web/` starts both the API (`dotnet watch`, port 3001) and Vite (5173, `strictPort`). Vite proxies `/api` to 3001. Don't start the API separately as well.
- Any page or API call takes `?date=yyyy-MM-dd` to look at a specific day.
- CI (`ci.yml`) runs `dotnet build Parkside.Api`, `dotnet build Parkside.Seed`, `npm run typecheck` and `npm run build`. A separate workflow (`tests.yml`) runs `dotnet test Parkside.Tests`, but only when `Parkside.Api/`, `Parkside.Tests/` or the solution change. Still run the tests yourself before pushing, and add or update tests in `Parkside.Tests/ReportingTests.cs` for new behaviour. `/verify` runs the full set.
- Run a single test with `dotnet test Parkside.Tests --filter "FullyQualifiedName~HeadlineStats_"`.

## Code conventions

- Formatting follows the config files, even where existing code doesn't yet: tabs in C# (`.editorconfig`), and tabs plus double quotes in TS/TSX (`web/biome.json`). `npm run format` in `web/` applies Biome, and `npm run lint` checks it.
- All aggregation and SQL lives in `Parkside.Api/Reporting.cs`. Routes in `Program.cs` only open a connection and call into it.
- Data access is raw SQLite through `Db.Query` with named `$param` parameters. There's no ORM and no migrations: the schema lives in `Parkside.Seed/Program.cs` and is duplicated in the test setup, so change both together.
- JSON casing is mixed on purpose. `HeadlineStats` and `ActivityTotal` serialize as camelCase, while `ScheduleEntry` and `BookingListEntry` use snake_case via `[JsonPropertyName]`. Keep the TS interfaces in `web/src/components/*` in sync with them.
- Dates are local-time `yyyy-MM-dd` strings (`DateTime.Now`, not `UtcNow`, because the team works past UTC midnight). `bookings.created_at` is a UTC ISO timestamp.
- The seed data comes from a fixed-seed Mulberry32 generator. Adding or reordering `rand()` calls changes all the generated data.

## Workflow

- The backlog is in GitHub Issues. Work on a branch per issue and open a PR that references it (`Fixes #n`).
- Lefthook pre-commit builds the C# projects and type-checks `web/`. The hooks are installed via the `lefthook` npm devDependency.
