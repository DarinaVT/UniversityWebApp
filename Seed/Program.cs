using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seed.Seed;
using Seed.Services;
using Seed;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection")));

services.AddTransient<DatabaseSeeder>();

services.AddHttpClient<WikimediaClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("UniversitySeeder/1.0");
});
services.AddTransient<UniversityImageEnrichmentService>();

var provider = services.BuildServiceProvider();


var command = args.FirstOrDefault()?.ToLowerInvariant();

switch (command)
{
    case "seed":
        Console.WriteLine("Starting BULK database seed...");
        var seeder = provider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        break;

    case "images":
        Console.WriteLine("Starting image enrichment...");
        var imageService = provider.GetRequiredService<UniversityImageEnrichmentService>();
        await imageService.RunAsync(batchSize: 100);
        break;

    case "extract-translations":
        Console.WriteLine("Extracting all translations from universities.json...");
        await ExtractAllTranslations.Run();
        break;

    case "translate":
        Console.WriteLine("Translating all entries to Bulgarian...");
        await TranslateToBulgarian.Run();
        break;

    default:
        Console.WriteLine("Invalid command.");
        Console.WriteLine("Use:");
        Console.WriteLine("  seed   -> bulk seed database");
        Console.WriteLine("  images -> enrich university images");
        Console.WriteLine("  extract-translations -> extract all names for translation");
        Console.WriteLine("  translate -> translate all entries to Bulgarian");
        break;
}