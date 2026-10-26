---
name: verify
description: Run the full Parkside check suite (CI builds, type-check, web build, Biome lint, and xUnit tests) and report what passed or failed. Use after making changes and before committing or opening a PR.
---

Run these checks from the repo root, in this order, and keep going after a failure so the report is complete:

1. `dotnet build Parkside.Api`
2. `dotnet build Parkside.Seed`
3. `dotnet test Parkside.Tests`
4. In `web/`: if `node_modules` is missing, run `npm ci` first. Then run `npm run typecheck`, `npm run lint` and `npm run build`.

Steps 1, 2 and the web type-check and build mirror `.github/workflows/ci.yml`. Step 3 mirrors `.github/workflows/tests.yml`, which CI runs only when the API or tests change. Lint is extra, since CI doesn't run it.

Report one line per check (pass/fail). For each failure, quote the relevant error lines and name the file:line. Don't fix anything unless asked. If `npm run lint` only fails on formatting in files you didn't touch, say so rather than calling it a regression.
