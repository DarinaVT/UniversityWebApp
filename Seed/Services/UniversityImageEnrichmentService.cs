using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Seed.Services;

public class UniversityImageEnrichmentService
{
    private readonly AppDbContext _db;
    private readonly WikimediaClient _wikimedia;

    public UniversityImageEnrichmentService(
        AppDbContext db,
        WikimediaClient wikimedia)
    {
        _db = db;
        _wikimedia = wikimedia;
    }

    public async Task RunAsync(int batchSize = 100)
    {
        var total = await _db.Universities.CountAsync();
        int updatedTotal = 0;

        while (true)
        {
            var batch = await _db.Universities
                .Where(u => u.ImageUrl.Contains("placeholder"))
                .OrderBy(u => u.Id)
                .Take(batchSize)
                .ToListAsync();

            if (batch.Count == 0)
                break;

            int updatedThisBatch = 0;

            foreach (var uni in batch)
            {
                var image = await _wikimedia.TryGetImageAsync(uni.Name);
                if (!string.IsNullOrWhiteSpace(image))
                {
                    uni.ImageUrl = image;
                    updatedThisBatch++;
                    updatedTotal++;
                }
            }

            // 🔴 КЛЮЧОВИЯТ FIX
            if (updatedThisBatch == 0)
            {
                Console.WriteLine("No more images found. Stopping enrichment.");
                break;
            }

            await _db.SaveChangesAsync();
            Console.WriteLine($"Updated {updatedTotal}/{total} universities...");
        }

        Console.WriteLine("Image enrichment completed ✅");
    }

}