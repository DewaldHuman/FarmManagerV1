using Farm.Web.Core.Registry;

namespace Farm.Web.Core.Irrigation;

/// <summary>
/// In-memory design store. On first use it seeds the first zone returned by
/// the API with a demo layout (main pipe + 3 laterals + 15 sprinklers + pump +
/// valve + water source) so the populated designer state is visible without
/// backend persistence existing yet.
/// </summary>
public class InMemoryZoneDesignStore : IZoneDesignStore
{
    private readonly IFarmService _farmService;
    private readonly Dictionary<Guid, ZoneDesign> _designs = new();
    private bool _seeded;

    public InMemoryZoneDesignStore(IFarmService farmService)
    {
        _farmService = farmService;
    }

    public async Task<ZoneDesign?> GetAsync(Guid zoneId)
    {
        await EnsureSeededAsync();
        return _designs.TryGetValue(zoneId, out var design) ? design : null;
    }

    public async Task SaveAsync(Guid zoneId, ZoneDesign design)
    {
        await EnsureSeededAsync();
        _designs[zoneId] = design;
    }

    public async Task<bool> HasDesignAsync(Guid zoneId)
    {
        await EnsureSeededAsync();
        return _designs.ContainsKey(zoneId);
    }

    private async Task EnsureSeededAsync()
    {
        if (_seeded)
        {
            return;
        }
        _seeded = true; // set first: a failed zone fetch shouldn't retry forever

        try
        {
            var zones = await _farmService.ListZonesAsync();
            if (zones.Count > 0)
            {
                _designs[zones[0].Id] = BuildDemoDesign();
            }
        }
        catch
        {
            // Best-effort seeding only — an unreachable API just means no demo layout.
        }
    }

    /// <summary>
    /// Demo layout on the default 80×56 m canvas: water source → pump → valve on a
    /// main line running up the left side, three 60 m laterals branching right at
    /// y = 14/28/42, five sprinklers per lateral at 12 m spacing.
    /// </summary>
    private static ZoneDesign BuildDemoDesign()
    {
        var design = new ZoneDesign();

        design.Points.Add(new DesignPoint { Kind = DesignPointKind.WaterSource, X = 4, Y = 52 });
        design.Points.Add(new DesignPoint { Kind = DesignPointKind.Pump, X = 8, Y = 52 });
        design.Points.Add(new DesignPoint { Kind = DesignPointKind.Valve, X = 8, Y = 46 });

        design.Pipes.Add(new DesignPipe
        {
            IsMain = true,
            Vertices = new List<DesignVertex>
            {
                new() { X = 8, Y = 52 },
                new() { X = 8, Y = 14 },
            },
        });

        foreach (var y in new[] { 14.0, 28.0, 42.0 })
        {
            design.Pipes.Add(new DesignPipe
            {
                IsMain = false,
                Vertices = new List<DesignVertex>
                {
                    new() { X = 8, Y = y },
                    new() { X = 68, Y = y },
                },
            });

            for (var i = 0; i < 5; i++)
            {
                design.Sprinklers.Add(new DesignSprinkler { X = 14 + i * 12, Y = y });
            }
        }

        return design;
    }
}
