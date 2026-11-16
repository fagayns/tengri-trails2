using TengriTrails.Models;

namespace TengriTrails.Services;

public sealed class TourSeedCatalog
{
    public IReadOnlyList<Tour> All { get; } =
    [
        new("song-kol-horseback", "tour.test_song_kol", "region.naryn", "tour.test_song_kol_desc",
            Image("song-kol-horseback.svg"), 26000, 3, NextDay(DayOfWeek.Saturday), "equestrian", 2,
            Gallery("song-kol-horseback.svg"), Days(3, "song_kol"), Included("guide", "lodging", "meals", "horse"),
            Excluded("flights", "insurance", "personal"), Packing("layers", "sunhat", "sunglasses", "personal_medication"), 2, 8),
        new("ala-archa-day-hike", "tour.test_ala_archa", "region.chuy", "tour.test_ala_archa_desc",
            Image("ala-archa-gorge.svg"), 4500, 1, NextDay(DayOfWeek.Sunday), "trekking", 1,
            Gallery("ala-archa-gorge.svg"), Days(1, "ala_archa"), Included("guide", "transfers"),
            Excluded("flights", "insurance", "personal"), Packing("walking_shoes", "layers", "water_bottle"), 2, 10),
        new("issyk-kul-road-trip", "tour.test_issyk_kul_road", "region.issyk_kul", "tour.test_issyk_kul_road_desc",
            Image("issyk-kul-shore.svg"), 14000, 2, NextDay(DayOfWeek.Friday), "road", 1,
            Gallery("issyk-kul-shore.svg"), Days(2, "issyk_kul_road"), Included("guide", "lodging", "transfers"),
            Excluded("flights", "insurance", "personal"), Packing("layers", "sunscreen", "sunglasses"), 2, 8),
        new("karakol-jeti-oguz", "tour.test_karakol_jeti_oguz", "region.issyk_kul", "tour.test_karakol_jeti_oguz_desc",
            Image("jeti-oguz-red-rocks.svg"), 21000, 3, NextDay(DayOfWeek.Saturday), "trekking", 2,
            Gallery("jeti-oguz-red-rocks.svg"), Days(3, "karakol_jeti_oguz"), Included("guide", "lodging", "meals"),
            Excluded("flights", "insurance", "personal"), Packing("boots", "layers", "raincoat"), 2, 8),
        new("sary-chelek", "tour.test_sary_chelek", "region.jalal_abad", "tour.test_sary_chelek_desc",
            Image("sary-chelek-lake.svg"), 24000, 3, NextDay(DayOfWeek.Sunday), "lakes", 1,
            Gallery("sary-chelek-lake.svg"), Days(3, "sary_chelek"), Included("guide", "lodging", "meals", "transfers"),
            Excluded("flights", "insurance", "personal"), Packing("walking_shoes", "layers", "water_bottle"), 2, 8),
        new("osh-city-day", "tour.test_osh_city", "region.osh", "tour.test_osh_city_desc",
            Image("osh-sulaiman-too.svg"), 5000, 1, NextDay(DayOfWeek.Friday), "cultural", 1,
            Gallery("osh-sulaiman-too.svg"), Days(1, "osh_city"), Included("guide"),
            Excluded("flights", "insurance", "personal"), Packing("walking_shoes", "sunhat", "water_bottle"), 2, 12)
    ];

    private static readonly (string Slug, DayOfWeek Day, string Time, string MeetingKey, int Capacity)[] DeparturePlans =
    [
        ("song-kol-horseback", DayOfWeek.Saturday, "07:30", "meeting.song_kol", 8),
        ("ala-archa-day-hike", DayOfWeek.Sunday, "08:00", "meeting.ala_archa", 8),
        ("issyk-kul-road-trip", DayOfWeek.Friday, "08:00", "meeting.issyk_kul", 8),
        ("karakol-jeti-oguz", DayOfWeek.Saturday, "08:00", "meeting.karakol", 8),
        ("sary-chelek", DayOfWeek.Sunday, "08:00", "meeting.sary_chelek", 8),
        ("osh-city-day", DayOfWeek.Friday, "09:00", "meeting.osh", 8)
    ];

    /// <summary>Weekly departures for the next <paramref name="weeks"/> weeks, counted from today (Bishkek time).</summary>
    public IReadOnlyList<TourDeparture> UpcomingDepartures(int weeks = 8)
    {
        var departures = new List<TourDeparture>();
        foreach (var plan in DeparturePlans)
        {
            var tour = Find(plan.Slug) ?? throw new InvalidOperationException($"Unknown tour {plan.Slug}.");
            var first = NextDay(plan.Day);
            for (var week = 0; week < weeks; week++)
            {
                var date = first.AddDays(week * 7);
                var id = $"{plan.Slug}-{date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}";
                departures.Add(new TourDeparture(id, plan.Slug, date, plan.Time, plan.MeetingKey, plan.Capacity, tour.Price));
            }
        }

        return departures;
    }

    private static DateTime NextDay(DayOfWeek day)
    {
        var today = DateTime.UtcNow.AddHours(6).Date;
        var delta = ((int)day - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(delta == 0 ? 7 : delta);
    }

    public Tour? Find(string slug) => All.FirstOrDefault(tour => tour.Slug == slug);

    private static string Image(string fileName) => $"/img/places/{fileName}";
    private static IReadOnlyList<string> Gallery(string fileName) => [Image(fileName)];
    private static IReadOnlyList<TourDay> Days(int count, string key) =>
        Enumerable.Range(1, count).Select(day => new TourDay(day, $"itinerary.test_{key}.day{day}")).ToArray();
    private static IReadOnlyList<string> Included(params string[] items) => items.Select(item => $"included.{item}").ToArray();
    private static IReadOnlyList<string> Excluded(params string[] items) => items.Select(item => $"excluded.{item}").ToArray();
    private static IReadOnlyList<string> Packing(params string[] items) => items.Select(item => $"packing.{item}").ToArray();
}
