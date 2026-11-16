namespace TengriTrails.Services;

/// <summary>
/// Looks for photos in wwwroot/img/tours: "{slug}.jpg" is the cover, "{slug}-2.jpg", "{slug}-3.jpg"... fill the gallery.
/// If there are none, the tour keeps its built-in SVG illustration.
/// </summary>
public sealed class TourImageResolver(IWebHostEnvironment environment)
{
    private static readonly string[] Extensions = [".jpg", ".jpeg", ".png", ".webp", ".avif"];

    public IReadOnlyList<string> Find(string slug)
    {
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var directory = Path.Combine(root, "img", "tours");
        if (!Directory.Exists(directory))
            return [];

        var found = new List<(int Order, string Name)>();
        foreach (var path in Directory.EnumerateFiles(directory))
        {
            var name = Path.GetFileName(path);
            if (!Extensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
                continue;

            var stem = Path.GetFileNameWithoutExtension(name);
            if (stem.Equals(slug, StringComparison.OrdinalIgnoreCase))
                found.Add((0, name));
            else if (stem.StartsWith(slug + "-", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(stem[(slug.Length + 1)..], out var number))
                found.Add((number, name));
        }

        return found
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => "/img/tours/" + Uri.EscapeDataString(item.Name))
            .ToArray();
    }
}
