using HarmonyLib;

using RefreshedRandom.Framework;
using RefreshedRandom.HarmonyPatches;

using StardewModdingAPI.Events;

namespace RefreshedRandom;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private const string MODDATAMESSAGE = "MOD_DATA_MESSAGE";

    /// <summary>
    /// Gets the cached data.
    /// </summary>
    internal static ModData? Data { get; private set; }

    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStart;
        helper.Events.GameLoop.Saving += this.OnSaving;

        // multiplayer
        helper.Events.Multiplayer.PeerConnected += this.PeerConnected;
        helper.Events.Multiplayer.ModMessageReceived += this.Multiplayer_ModMessageReceived;

        Harmony harmony = new(this.ModManifest.UniqueID);
        DaySaveRandomPatch.ApplyPatch(harmony);
        IntervalRandomPatch.ApplyPatch(harmony);
        GameLocationForagePatch.ApplyPatch(harmony);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        Data ??= new();
        Data.LastDayMilliseconds = (int)(Game1.player.millisecondsPlayed ^ (Game1.player.millisecondsPlayed << 32));
        Data.LastDaySteps = (int)Game1.player.stats.StepsTaken;
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Data = this.Helper.Data.ReadSaveData<ModData>("data") ?? new();
        if (Data.LastDayMilliseconds == -1)
        {
            Data.LastDayMilliseconds = (int)(Game1.player.millisecondsPlayed ^ (Game1.player.millisecondsPlayed << 32));
        }

        if (Data.LastDaySteps == -1)
        {
            Data.LastDaySteps = (int)Game1.player.stats.StepsTaken;
        }

        this.Broadcast(Data);
    }

    private void PeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (Data is null)
        {
            this.Monitor.Log($"Somehow got request for data before data is loaded for {e.Peer.PlayerID}. This will likely behave weirdly.", LogLevel.Error);
            return;
        }

        if (Context.IsMainPlayer)
        {
            this.Helper.Multiplayer.SendMessage(
                Data,
                MODDATAMESSAGE,
                [this.ModManifest.UniqueID],
                [e.Peer.PlayerID]
                );
        }
    }

    private void Broadcast(ModData data)
    {
        this.Helper.Multiplayer.SendMessage(
            data,
            MODDATAMESSAGE,
            [this.ModManifest.UniqueID],
            this.Helper.Multiplayer.GetConnectedPlayers().Where(player => !player.IsSplitScreen).Select(player => player.PlayerID).ToArray()
        );
    }

    private void Multiplayer_ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != this.ModManifest.UniqueID || e.Type != MODDATAMESSAGE)
        {
            return;
        }

        Data = e.ReadAs<ModData>();
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    /// <remarks>We set Game1.random here to use Xoshiro-256 over the legacy NET implementation.</remarks>
    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        Game1.random = Random.Shared;
        GameLocationForagePatch.Reset();
    }
}