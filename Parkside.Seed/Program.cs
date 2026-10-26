using System.Globalization;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(RepoRoot(), "parkside.db");

// Fixed seed so the sample data is the same every time it's rebuilt.
var rand = Mulberry32(20260902);

T Pick<T>(IReadOnlyList<T> items) => items[(int)Math.Floor(rand() * items.Count)];

int RandomInt(int min, int max) => min + (int)Math.Floor(rand() * (max - min + 1));

ActivityDefinition[] activities =
[
    new("Archery", 12, 60, ["09:00", "11:00", "15:00"], (0.2, 1)),
    new("Aqua Fit", 20, 45, ["09:30", "17:30"], (0.2, 0.95)),
    new("Kids' Club", 25, 120, ["09:00", "10:30"], (0.3, 1)),
    new("Evening Show", 120, 90, ["20:00"], (0.15, 0.5)),
    new("Pottery", 10, 90, ["14:00"], (0.15, 0.9)),
    new("Tennis Coaching", 8, 60, ["08:30", "16:00"], (0.15, 1)),
    new("Bike Hire Tour", 15, 120, ["10:00", "14:30"], (0.2, 0.9)),
    new("Crazy Golf", 30, 60, ["11:30", "16:30"], (0.15, 0.7)),
];

string[] guestNames =
[
    "Lauren Whitfield", "Michael Osei", "Rhiannon Davies", "Sanjay Ahmed",
    "Fiona Balfour", "Callum Reid", "Nadia Hussain", "Gareth Pritchard",
    "Imogen Clarke", "Dermot Kelly", "Priya Nair", "Stuart Menzies",
    "Bethan Lloyd", "Owen Trethewey", "Amara Okonkwo", "Hamish Sinclair",
    "Josie Bramwell", "Tomasz Nowak", "Ellie Fairbairn", "Raj Chandra",
    "Morag Buchanan", "Dylan Hargreaves", "Sofia Marchetti", "Neil Cathcart",
    "Kirsty Dunlop", "Abdul Rahman", "Verity Ashcombe", "Liam Doherty",
    "Chloe Nesbitt", "Peter Grantham", "Yasmin Farouk", "Duncan Erskine",
    "Harriet Vaughan", "Joseph Adeyemi", "Megan Tudor", "Ciaran Boyle",
    "Alice Pemberton", "Ravi Deshpande", "Sian Morgan", "Alan Widdowson",
];

// Party sizes skew small, with enough larger groups that counting bookings and
// counting people give noticeably different answers.
int[] partySizes = [1, 1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 5, 6];

string ToIsoDate(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

DateTime DateOffsetFromToday(int offset) => DateTime.Today.AddDays(offset);

ActivityDefinition Activity(string name) =>
    activities.FirstOrDefault(a => a.Name == name) ?? throw new InvalidOperationException($"Unknown activity: {name}");

// The current day is laid out by hand so the dashboard always opens on a
// useful spread — a couple of sessions under real pressure, a couple barely
// touched, and the evening show comfortably busy.
List<PlannedSession> SessionsForToday(string date)
{
    (string Name, string StartTime, int ConfirmedPlaces, int CancelledBookings)[] plan =
    [
        // activity, start time, confirmed places, cancelled bookings
        ("Tennis Coaching", "08:30", 1, 0),
        ("Archery", "09:00", 12, 3),
        ("Kids' Club", "09:00", 24, 1),
        ("Aqua Fit", "09:30", 13, 2),
        ("Bike Hire Tour", "10:00", 9, 1),
        ("Pottery", "14:00", 2, 0),
        ("Evening Show", "20:00", 52, 4),
    ];
    return plan
        .Select(p =>
        {
            var a = Activity(p.Name);
            return new PlannedSession(a.Name, date, p.StartTime, a.Duration, a.Capacity, p.ConfirmedPlaces, p.CancelledBookings);
        })
        .ToList();
}

List<PlannedSession> SessionsForOrdinaryDay(string date)
{
    var count = RandomInt(5, 7);
    var chosen = new List<PlannedSession>();
    var taken = new HashSet<string>();

    while (chosen.Count < count)
    {
        var a = Pick(activities);
        var startTime = Pick(a.Times);
        var key = $"{a.Name}@{startTime}";
        if (!taken.Add(key)) continue;

        var (minFill, maxFill) = a.Fill;
        var fraction = minFill + rand() * (maxFill - minFill);
        var confirmedPlaces = Math.Max(1, Math.Min(a.Capacity, (int)Math.Round(a.Capacity * fraction, MidpointRounding.AwayFromZero)));

        chosen.Add(new PlannedSession(
            a.Name,
            date,
            startTime,
            a.Duration,
            a.Capacity,
            confirmedPlaces,
            rand() < 0.5 ? RandomInt(1, 3) : 0));
    }

    // OrderBy is a stable sort, so sessions sharing a start time keep the order they were picked in.
    return chosen.OrderBy(s => s.StartTime, StringComparer.Ordinal).ToList();
}

List<PlannedSession> PlanSessions()
{
    var planned = new List<PlannedSession>();

    for (var offset = -3; offset <= 10; offset += 1)
    {
        var date = ToIsoDate(DateOffsetFromToday(offset));

        // Nothing on at all.
        if (offset == 9) continue;

        // A very thin day — one session and little else.
        if (offset == 3)
        {
            var a = Activity("Crazy Golf");
            planned.Add(new PlannedSession(a.Name, date, "11:30", a.Duration, a.Capacity, 4, 0));
            continue;
        }

        if (offset == 0)
        {
            planned.AddRange(SessionsForToday(date));
            continue;
        }

        planned.AddRange(SessionsForOrdinaryDay(date));
    }

    return planned;
}

// Party sizes adding up to exactly the requested number of places.
List<int> PartySizesFor(int places)
{
    var sizes = new List<int>();
    var remaining = places;
    while (remaining > 0)
    {
        var next = Pick(partySizes);
        var size = next > remaining ? remaining : next;
        sizes.Add(size);
        remaining -= size;
    }
    return sizes;
}

// Stored as a UTC ISO timestamp, e.g. 2026-10-12T14:37:00.000Z.
string CreatedAtBefore(string sessionDate)
{
    var day = DateTime.ParseExact(sessionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(-RandomInt(1, 28));
    var hour = RandomInt(7, 21);
    var minute = RandomInt(0, 59);
    var created = new DateTime(day.Year, day.Month, day.Day, hour, minute, 0, DateTimeKind.Local);
    return created.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
}

using var db = new SqliteConnection($"Data Source={dbPath}");
db.Open();

using (var schema = db.CreateCommand())
{
    schema.CommandText = """
        DROP TABLE IF EXISTS bookings;
        DROP TABLE IF EXISTS sessions;

        CREATE TABLE sessions (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          activity TEXT NOT NULL,
          date TEXT NOT NULL,
          start_time TEXT NOT NULL,
          duration_minutes INTEGER NOT NULL,
          capacity INTEGER NOT NULL
        );

        CREATE TABLE bookings (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          session_id INTEGER NOT NULL REFERENCES sessions(id),
          guest_name TEXT NOT NULL,
          party_size INTEGER NOT NULL,
          status TEXT NOT NULL,
          created_at TEXT NOT NULL
        );
        """;
    schema.ExecuteNonQuery();
}

var planned = PlanSessions();
var bookingTotal = 0;

using (var transaction = db.BeginTransaction())
{
    using var insertSession = db.CreateCommand();
    insertSession.Transaction = transaction;
    insertSession.CommandText = """
        INSERT INTO sessions (activity, date, start_time, duration_minutes, capacity)
        VALUES ($activity, $date, $startTime, $duration, $capacity)
        RETURNING id
        """;

    using var insertBooking = db.CreateCommand();
    insertBooking.Transaction = transaction;
    insertBooking.CommandText = """
        INSERT INTO bookings (session_id, guest_name, party_size, status, created_at)
        VALUES ($sessionId, $guestName, $partySize, $status, $createdAt)
        """;

    void InsertBooking(long sessionId, string guestName, int partySize, string status, string createdAt)
    {
        insertBooking.Parameters.Clear();
        insertBooking.Parameters.AddWithValue("$sessionId", sessionId);
        insertBooking.Parameters.AddWithValue("$guestName", guestName);
        insertBooking.Parameters.AddWithValue("$partySize", partySize);
        insertBooking.Parameters.AddWithValue("$status", status);
        insertBooking.Parameters.AddWithValue("$createdAt", createdAt);
        insertBooking.ExecuteNonQuery();
    }

    foreach (var session in planned)
    {
        insertSession.Parameters.Clear();
        insertSession.Parameters.AddWithValue("$activity", session.Activity);
        insertSession.Parameters.AddWithValue("$date", session.Date);
        insertSession.Parameters.AddWithValue("$startTime", session.StartTime);
        insertSession.Parameters.AddWithValue("$duration", session.Duration);
        insertSession.Parameters.AddWithValue("$capacity", session.Capacity);
        var sessionId = (long)insertSession.ExecuteScalar()!;

        foreach (var size in PartySizesFor(session.ConfirmedPlaces))
        {
            InsertBooking(sessionId, Pick(guestNames), size, "confirmed", CreatedAtBefore(session.Date));
            bookingTotal += 1;
        }

        for (var i = 0; i < session.CancelledBookings; i += 1)
        {
            InsertBooking(sessionId, Pick(guestNames), Pick(partySizes), "cancelled", CreatedAtBefore(session.Date));
            bookingTotal += 1;
        }
    }

    transaction.Commit();
}

var dates = planned.Select(s => s.Date).Order(StringComparer.Ordinal).ToList();
Console.WriteLine($"Seeded {planned.Count} sessions and {bookingTotal} bookings covering {dates[0]} to {dates[^1]}.");

// A port of the mulberry32 generator, so the same seed gives the same data
// as it always has.
static Func<double> Mulberry32(int seed)
{
    var a = seed;
    return () =>
    {
        unchecked
        {
            a += 0x6d2b79f5;
            var t = (a ^ (int)((uint)a >> 15)) * (1 | a);
            t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
            return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
        }
    };
}

// Walks up from the build output until it finds the folder holding Parkside.sln.
static string RepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Parkside.sln")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? throw new InvalidOperationException("Could not find the repo root (no Parkside.sln above the build output).");
}

record ActivityDefinition(
    string Name,
    int Capacity,
    int Duration,
    string[] Times,
    // Typical share of capacity taken up, as (min, max).
    (double Min, double Max) Fill);

record PlannedSession(
    string Activity,
    string Date,
    string StartTime,
    int Duration,
    int Capacity,
    // Places to fill with confirmed bookings. Never more than capacity.
    int ConfirmedPlaces,
    // Extra bookings added on top and marked cancelled.
    int CancelledBookings);
