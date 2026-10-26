# Parkside Activities

Internal dashboard for the activities team at Parkside. Guests book onto sessions —
archery, aqua fit, kids' club, evening shows and the rest — and this shows the team how
those sessions are filling up and where the pressure is.

## Running it

You'll need the .NET 10 SDK and Node.

```
dotnet run --project Parkside.Seed
cd web
npm install
npm run dev
```

`dotnet run --project Parkside.Seed` creates `parkside.db` and fills it with data.
`npm run dev` starts the API on port 3001 and the dashboard on http://localhost:5173.

## Layout

- `Parkside.Api/` — ASP.NET Core minimal API. `Reporting.cs` holds the aggregation logic;
  the routes in `Program.cs` just call into it.
- `web/` — the React dashboard.
- `Parkside.Seed/` — schema and seed data.
- The backlog is in this repo's GitHub Issues.
