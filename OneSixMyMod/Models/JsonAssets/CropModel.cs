namespace OneSixMyMod.Models.JsonAssets;
public record CropModel(
    string Name,
    string? EnableWithMod,
    string? DisableWithMod,
    object? Product,
    string? SeedName,
    string? SeedDescription,
    CropType CropType = CropType.Normal,
    string[]? Seasons = null,
    int[]? Phases = null)
    : BaseIdModel(Name, EnableWithMod, DisableWithMod);