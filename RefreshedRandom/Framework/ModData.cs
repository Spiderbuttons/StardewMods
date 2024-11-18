namespace RefreshedRandom.Framework;

/// <summary>
/// The data for this mod.
/// </summary>
public sealed class ModData
{
    /// <summary>
    /// The cached number of ms the player has played.
    /// </summary>
    public int LastDayMilliseconds { get; set; } = -1;

    /// <summary>
    /// The cached number of steps the player has taken.
    /// </summary>
    public int LastDaySteps { get; set; } = -1;

}