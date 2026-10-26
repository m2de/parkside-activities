using System.Globalization;
using Parkside.Api;

var app = WebApplication.CreateBuilder(args).Build();

app.MapGet("/api/stats", (string? date) =>
{
    using var db = Db.Open();
    return Reporting.HeadlineStats(db, date ?? Today());
});
app.MapGet("/api/schedule", (string? date) =>
{
    using var db = Db.Open();
    return Reporting.ScheduleForDay(db, date ?? Today());
});
app.MapGet("/api/activities", (string? date) =>
{
    using var db = Db.Open();
    return Reporting.ActivityTotals(db, date ?? Today());
});
app.MapGet("/api/bookings", (string? date) =>
{
    using var db = Db.Open();
    return Reporting.BookingsForDay(db, date ?? Today());
});

app.Run();

// Today in local time as 'yyyy-MM-dd'. Not DateTime.UtcNow, which rolls over to
// tomorrow while the team are still on shift.
static string Today() => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
