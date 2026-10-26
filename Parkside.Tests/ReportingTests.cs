using Microsoft.Data.Sqlite;
using Parkside.Api;
using Xunit;

namespace Parkside.Tests;

public class ReportingTests : IDisposable
{
    private readonly SqliteConnection _db;

    public ReportingTests()
    {
        _db = new SqliteConnection("Data Source=:memory:");
        _db.Open();

        Exec("""
            CREATE TABLE sessions (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              activity TEXT NOT NULL, date TEXT NOT NULL, start_time TEXT NOT NULL,
              duration_minutes INTEGER NOT NULL, capacity INTEGER NOT NULL);
            CREATE TABLE bookings (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              session_id INTEGER NOT NULL REFERENCES sessions(id),
              guest_name TEXT NOT NULL, party_size INTEGER NOT NULL,
              status TEXT NOT NULL, created_at TEXT NOT NULL);
            """);

        Exec("""
            INSERT INTO sessions (id, activity, date, start_time, duration_minutes, capacity) VALUES
              (1, 'Archery', '2026-03-14', '09:00', 60, 12),
              (2, 'Aqua Fit', '2026-03-14', '09:30', 45, 20),
              (3, 'Pottery', '2026-03-14', '14:00', 90, 10),
              (4, 'Archery', '2026-03-15', '09:00', 60, 12);
            """);

        Exec("""
            INSERT INTO bookings (session_id, guest_name, party_size, status, created_at) VALUES
              (1, 'Alice Brennan', 4, 'confirmed', '2026-03-01T10:00:00Z'),
              (1, 'Ben Carlisle', 3, 'confirmed', '2026-03-02T11:00:00Z'),
              (1, 'Cara Dunn', 2, 'cancelled', '2026-03-03T12:00:00Z'),
              (1, 'Dai Ellis', 5, 'confirmed', '2026-03-04T13:00:00Z'),
              (2, 'Eve Fraser', 5, 'confirmed', '2026-03-05T09:00:00Z'),
              (2, 'Finn Gale', 1, 'cancelled', '2026-03-06T09:30:00Z'),
              (3, 'Gwen Harris', 2, 'confirmed', '2026-03-07T14:00:00Z'),
              (4, 'Huw Idris', 6, 'confirmed', '2026-03-08T08:00:00Z');
            """);
    }

    public void Dispose() => _db.Dispose();

    private void Exec(string sql)
    {
        using var command = _db.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    // HeadlineStats

    [Fact]
    public void HeadlineStats_TotalsTheSessionsRunningAndTheirCombinedCapacity()
    {
        var stats = Reporting.HeadlineStats(_db, "2026-03-14");
        Assert.Equal(3, stats.SessionCount);
        Assert.Equal(42, stats.TotalCapacity);
    }

    [Fact]
    public void HeadlineStats_TotalsTheBookingsAndPlacesBookedAcrossTheDay()
    {
        var stats = Reporting.HeadlineStats(_db, "2026-03-14");
        Assert.Equal(7, stats.BookingCount);
        Assert.Equal(22, stats.BookedPlaces);
    }

    [Fact]
    public void HeadlineStats_CalculatesOccupancyAsAWholeNumberPercentageOfCapacity()
    {
        var stats = Reporting.HeadlineStats(_db, "2026-03-14");
        Assert.Equal(52, stats.OccupancyPct);
    }

    [Fact]
    public void HeadlineStats_CountsCancellationsAndAveragesPartySizeAcrossAllBookings()
    {
        var stats = Reporting.HeadlineStats(_db, "2026-03-14");
        Assert.Equal(2, stats.CancelledCount);
        Assert.Equal(3.1, stats.AveragePartySize);
    }

    [Fact]
    public void HeadlineStats_ReturnsZeroedFiguresForADayWithNothingScheduled()
    {
        var stats = Reporting.HeadlineStats(_db, "2026-03-20");
        Assert.Equal(
            new HeadlineStats(
                SessionCount: 0,
                BookingCount: 0,
                TotalCapacity: 0,
                BookedPlaces: 0,
                OccupancyPct: 0,
                CancelledCount: 0,
                AveragePartySize: 0),
            stats);
    }

    // ScheduleForDay

    [Fact]
    public void ScheduleForDay_ListsTheDaysSessionsInStartTimeOrderWithTheirBookingCounts()
    {
        var schedule = Reporting.ScheduleForDay(_db, "2026-03-14");
        Assert.Equal(
            [
                ("Archery", "09:00", 12, 4),
                ("Aqua Fit", "09:30", 20, 2),
                ("Pottery", "14:00", 10, 1),
            ],
            schedule.Select(s => (s.Activity, s.StartTime, s.Capacity, s.Bookings)));
    }

    [Fact]
    public void ScheduleForDay_DoesNotIncludeSessionsFromOtherDays()
    {
        var schedule = Reporting.ScheduleForDay(_db, "2026-03-14");
        Assert.Contains(schedule, entry => entry.Activity == "Archery" && entry.StartTime == "09:00" && entry.Capacity == 12);
        Assert.Equal(3, schedule.Count);
        Assert.NotEqual(schedule, Reporting.ScheduleForDay(_db, "2026-03-15"));
    }

    // ActivityTotals

    [Fact]
    public void ActivityTotals_TotalsSessionsBookingsAndPlacesBookedPerActivityBusiestFirst()
    {
        var totals = Reporting.ActivityTotals(_db, "2026-03-14");
        Assert.Equal(
            [
                new ActivityTotal(Activity: "Archery", SessionCount: 1, BookingCount: 4, BookedPlaces: 14),
                new ActivityTotal(Activity: "Aqua Fit", SessionCount: 1, BookingCount: 2, BookedPlaces: 6),
                new ActivityTotal(Activity: "Pottery", SessionCount: 1, BookingCount: 1, BookedPlaces: 2),
            ],
            totals);
    }

    // BookingsForDay

    [Fact]
    public void BookingsForDay_ListsBookingsOrderedBySessionStartTimeThenGuestName()
    {
        var bookings = Reporting.BookingsForDay(_db, "2026-03-14");
        Assert.Equal(
            [
                "Alice Brennan",
                "Ben Carlisle",
                "Cara Dunn",
                "Dai Ellis",
                "Eve Fraser",
                "Finn Gale",
                "Gwen Harris",
            ],
            bookings.Select(b => b.GuestName));
    }

    [Fact]
    public void BookingsForDay_ExcludesBookingsForSessionsOnOtherDays()
    {
        var bookings = Reporting.BookingsForDay(_db, "2026-03-14");
        Assert.DoesNotContain(bookings, b => b.GuestName == "Huw Idris");
    }

    [Fact]
    public void BookingsForDay_CarriesTheSessionsActivityAndStartTimeThroughOnEachRow()
    {
        var bookings = Reporting.BookingsForDay(_db, "2026-03-14");
        var alice = bookings.Single(b => b.GuestName == "Alice Brennan");
        Assert.Equal(("Archery", "09:00"), (alice.Activity, alice.StartTime));
    }

    [Fact]
    public void BookingsForDay_IncludesCancelledBookingsAlongsideConfirmedOnes()
    {
        var bookings = Reporting.BookingsForDay(_db, "2026-03-14");
        var cara = bookings.Single(b => b.GuestName == "Cara Dunn");
        Assert.Equal("cancelled", cara.Status);
    }
}
