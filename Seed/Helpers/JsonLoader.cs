using System.Text.Json;
using Seed.Models;

namespace Seed.Helpers;

public static class JsonLoader
{
    public static List<UniversityJsonModel> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "universities.json");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find file '{path}'.");

        var json = File.ReadAllText(path);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var root = JsonSerializer.Deserialize<UniversityJsonRoot>(json, options);

        if (root == null || root.Universities.Count == 0)
            throw new InvalidOperationException("No universities found in JSON.");

        return root.Universities;
    }
}
