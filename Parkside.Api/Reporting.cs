using System.Data;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace Parkside.Api;

// All the dashboard's aggregation logic lives in this one file. Routes in
// Program.cs just call these methods and return the result — no query building
// or number-crunching there.

public record SessionWithBookings(
    int Id,
    string Activity,
    string Date,
    string StartTime,
    int DurationMinutes,
    int Capacity,
    int BookingCount,
    int BookedPlaces);

public record HeadlineStats(
    int SessionCount,
    int BookingCount,
    int TotalCapacity,
    int BookedPlaces,
    int OccupancyPct,
    int CancelledCount,
    double AveragePartySize);

public record ScheduleEntry(
    int Id,
    string Activity,
    [property: JsonPropertyName("start_time")] string StartTime,
    [property: JsonPropertyName("duration_minutes")] int DurationMinutes,
    int Capacity,
	[property: JsonPropertyName("booked_places")] int BookedPlaces);

public record ActivityTotal(
    string Activity,
    int SessionCount,
    int BookingCount,
    int BookedPlaces);

public record BookingListEntry(
    int Id,
    [property: JsonPropertyName("guest_name")] string GuestName,
    [property: JsonPropertyName("party_size")] int PartySize,
    string Status,
    [property: JsonPropertyName("created_at")] string CreatedAt,
    string Activity,
    [property: JsonPropertyName("start_time")] string StartTime);

public static class Reporting
{
    // Sessions for a single day, with booking totals attached via a single
    // grouped query rather than N+1 lookups.
    private static List<SessionWithBookings> SessionsWithBookings(SqliteConnection db, string date)
    {
        return db.Query(
            """
            SELECT
              s.id,
              s.activity,
              s.date,
              s.start_time,
              s.duration_minutes,
              s.capacity,
              COUNT(b.id) AS bookingCount,
              COALESCE(SUM(b.party_size), 0) AS bookedPlaces
            FROM sessions s
            LEFT JOIN bookings b ON b.session_id = s.id AND b.status <> 'cancelled'
            WHERE s.date = $date
            GROUP BY s.id
            ORDER BY s.start_time, s.activity
            """,
            r => new SessionWithBookings(
                r.GetInt32("id"),
                r.GetString("activity"),
                r.GetString("date"),
                r.GetString("start_time"),
                r.GetInt32("duration_minutes"),
                r.GetInt32("capacity"),
                r.GetInt32("bookingCount"),
                r.GetInt32("bookedPlaces")),
            ("$date", date));
    }

    // Whole-number percentage, guarding against division by zero.
    private static int Pct(int part, int whole)
    {
        if (whole == 0) return 0;
        return (int)Math.Round((double)part / whole * 100, MidpointRounding.AwayFromZero);
    }

    // Top-row summary stats for the dashboard.
    public static HeadlineStats HeadlineStats(SqliteConnection db, string date)
    {
        var sessions = SessionsWithBookings(db, date);

        var sessionCount = sessions.Count;
        var totalCapacity = sessions.Sum(s => s.Capacity);
        var bookingCount = sessions.Sum(s => s.BookingCount);
        var bookedPlaces = sessions.Sum(s => s.BookedPlaces);

        // Cancellations get their own figure in the summary row.
        var cancelledCount = db.Query(
            """
            SELECT COUNT(*) AS count
            FROM bookings b
            JOIN sessions s ON s.id = b.session_id
            WHERE s.date = $date AND b.status = 'cancelled'
            """,
            r => r.GetInt32("count"),
            ("$date", date)).Single();

        var avgPartySize = db.Query(
            """
            SELECT AVG(b.party_size) AS avgPartySize
            FROM bookings b
            JOIN sessions s ON s.id = b.session_id
            WHERE s.date = $date AND b.status <> 'cancelled'
            """,
            r => r.IsDBNull("avgPartySize") ? (double?)null : r.GetDouble("avgPartySize"),
            ("$date", date)).Single();

        var averagePartySize = avgPartySize is { } avg && avg != 0
            ? Math.Round(avg * 10, MidpointRounding.AwayFromZero) / 10
            : 0;

        return new HeadlineStats(
            sessionCount,
            bookingCount,
            totalCapacity,
            bookedPlaces,
            Pct(bookedPlaces, totalCapacity),
            cancelledCount,
            averagePartySize);
    }

	// Rows for the schedule table. Each session reports places booked (the sum of
	// party sizes), not the number of bookings, so it compares with capacity.
    public static List<ScheduleEntry> ScheduleForDay(SqliteConnection db, string date)
    {
        return SessionsWithBookings(db, date)
            .Select(s => new ScheduleEntry(
                s.Id,
                s.Activity,
                s.StartTime,
                s.DurationMinutes,
                s.Capacity,
				s.BookedPlaces))
            .ToList();
    }

    // Per-activity totals for the day, busiest activity (by places booked) first.
    public static List<ActivityTotal> ActivityTotals(SqliteConnection db, string date)
    {
        return db.Query(
            """
            SELECT
              s.activity AS activity,
              COUNT(DISTINCT s.id) AS sessionCount,
              COUNT(b.id) AS bookingCount,
              COALESCE(SUM(b.party_size), 0) AS bookedPlaces
            FROM sessions s
            LEFT JOIN bookings b ON b.session_id = s.id AND b.status <> 'cancelled'
            WHERE s.date = $date
            GROUP BY s.activity
            ORDER BY bookedPlaces DESC
            """,
            r => new ActivityTotal(
                r.GetString("activity"),
                r.GetInt32("sessionCount"),
                r.GetInt32("bookingCount"),
                r.GetInt32("bookedPlaces")),
            ("$date", date));
    }

    // The full bookings list for a day, across all sessions.
    public static List<BookingListEntry> BookingsForDay(SqliteConnection db, string date)
    {
        return db.Query(
            """
            SELECT
              b.id,
              b.guest_name,
              b.party_size,
              b.status,
              b.created_at,
              s.activity,
              s.start_time
            FROM bookings b
            JOIN sessions s ON s.id = b.session_id
            WHERE s.date = $date
            ORDER BY s.start_time, b.guest_name
            """,
            r => new BookingListEntry(
                r.GetInt32("id"),
                r.GetString("guest_name"),
                r.GetInt32("party_size"),
                r.GetString("status"),
                r.GetString("created_at"),
                r.GetString("activity"),
                r.GetString("start_time")),
            ("$date", date));
    }
}
