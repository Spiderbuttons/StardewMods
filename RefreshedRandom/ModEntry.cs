﻿using HarmonyLib;

using RefreshedRandom.Framework;
using RefreshedRandom.HarmonyPatches;

using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;

namespace RefreshedRandom;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// Gets the cached data.
    /// </summary>
    internal static ModData? Data { get; private set; }

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;

        helper.Events.Specialized.LoadStageChanged += this.OnLoadSaveChanged;
        helper.Events.GameLoop.SaveCreated += this.OnSaveCreate;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStart;
        helper.Events.GameLoop.Saving += this.OnSaving;

        // multiplayer
        helper.Events.Multiplayer.PeerConnected += this.PeerConnected;
        helper.Events.Multiplayer.ModMessageReceived += this.ModMessageReceived;

        Harmony harmony = new(this.ModManifest.UniqueID);
        DaySaveRandomPatch.ApplyPatch(harmony);
        IntervalRandomPatch.ApplyPatch(harmony);
        GameLocationForagePatch.ApplyPatch(harmony);
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveCreated"/>
    private void OnSaveCreate(object? sender, SaveCreatedEventArgs e)
    {
        // On new game, we need to make sure Data is populated with...something.
        // to keep re-creates of new games consistent, we will not be using a randomly generated number for LastSeed.
        if (Context.IsMainPlayer)
        {
            Data = new()
            {
                LastSteps = 0,
                LastMilliseconds = 0,
                LastSeed = (int)Game1.uniqueIDForThisGame,
            };
        }
    }

    /// <inheritdoc cref="ISpecializedEvents.LoadStageChanged"/>
    private void OnLoadSaveChanged(object? sender, LoadStageChangedEventArgs e)
    {
        if (e.NewStage is LoadStage.SaveParsed)
        {
            Data = this.Helper.Data.ReadSaveData<ModData>(nameof(Data)) ?? new();
            Data.PopulateIfBlank(SaveGame.loaded.player);
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.Saving"/>
    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            Data ??= new();
            Data.Update(Game1.player);

            this.Helper.Data.WriteSaveData(nameof(Data), Data);
            this.Broadcast();
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            Data ??= this.Helper.Data.ReadSaveData<ModData>(nameof(Data)) ?? new();
            Data.PopulateIfBlank(Game1.player);

            this.Broadcast();
        }
    }

    /// <inheritdoc cref="IMultiplayerEvents.PeerConnected"/>
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
                nameof(Data),
                [this.ModManifest.UniqueID],
                [e.Peer.PlayerID]
                );
        }
    }

    private void Broadcast()
    {
        this.Helper.Multiplayer.SendMessage(
            Data,
            nameof(Data),
            [this.ModManifest.UniqueID],
            this.Helper.Multiplayer.GetConnectedPlayers().Where(player => !player.IsSplitScreen).Select(player => player.PlayerID).ToArray()
        );
    }

    /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
    private void ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != this.ModManifest.UniqueID || e.Type != nameof(Data))
        {
            return;
        }

        Data = e.ReadAs<ModData>();
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    /// <remarks>At this point, because Game1.random can be influenced by player behavior, it's "safe" to replace with an unseeded version.</remarks>
    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        // fish in C will attempt to "rewind" the random. Thus, we give it a new instance of Random it can use without affecting Random.Shared.
        Game1.random = this.Helper.ModRegistry.IsLoaded("focustense.FishinC") ? new Random() : Random.Shared;
        GameLocationForagePatch.Reset();
    }
}