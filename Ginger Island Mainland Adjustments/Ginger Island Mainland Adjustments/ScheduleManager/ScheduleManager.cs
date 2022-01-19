﻿using StardewModdingAPI.Utilities;
using StardewValley.Network;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

/// <summary>
/// Class that helps select the right GI remainder schedule.
/// </summary>
internal class ScheduleManager
{
    private const string BASE_SCHEDULE_KEY = "GIRemainder";
    private const string POST_GI_START_TIME = "1800"; // all GI schedules must start at 1800

    /// <summary>
    /// Find the correct schedule for an NPC for a given date. Looks into the schedule assets first
    /// then sees if there's a GOTO statement. Resolve that if necessary.
    /// </summary>
    /// <param name="npc">NPC to look for.</param>
    /// <param name="date">Date to search.</param>
    /// <returns>A schedule string if it can, null if it can't find one.</returns>
    public string? FindProperGISchedule(NPC npc, SDate date)
    {
        string? scheduleEntry = null;
        string scheduleKey = BASE_SCHEDULE_KEY;
        if (npc.isMarried())
        {
            scheduleKey += "_married";
        }
        int hearts = Utility.GetAllPlayerFriendshipLevel(npc) / 250;

        // GIRemainder_Season_Day
        string checkKey = $"{scheduleKey}_{date.Season}_{date.Day}";
        if (npc.hasMasterScheduleEntry(checkKey)
            && this.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIRemainder_intDay_heartlevel
        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel -= 2)
        {
            checkKey = $"{BASE_SCHEDULE_KEY}_{date.Day}_{heartLevel}";
            if (npc.hasMasterScheduleEntry(checkKey)
                && this.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainder_Day
        checkKey = $"{BASE_SCHEDULE_KEY}_{Game1.dayOfMonth}";
        if (npc.hasMasterScheduleEntry(checkKey)
            && this.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIRemainder_rain
        if (Game1.IsRainingHere(npc.currentLocation))
        {
            checkKey = $"{BASE_SCHEDULE_KEY}_rain";
            if (npc.hasMasterScheduleEntry(checkKey)
                && this.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainderHearts
        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel -= 2)
        {
            checkKey = $"{BASE_SCHEDULE_KEY}_{date.Day}_{heartLevel}";
            if (npc.hasMasterScheduleEntry(checkKey)
                && this.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainder_DayOfWeek
        checkKey = $"{BASE_SCHEDULE_KEY}_{Game1.shortDayNameFromDayOfSeason(date.Day)}";
        if (npc.hasMasterScheduleEntry(checkKey)
            && this.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIREmainder
        if (npc.hasMasterScheduleEntry(BASE_SCHEDULE_KEY)
            && this.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(BASE_SCHEDULE_KEY), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        Globals.ModMonitor.Log(I18n.NOGISCHEDULEFOUND(npc: npc.Name));
        return scheduleEntry;
    }

    /// <summary>
    /// Given a raw schedule string, returns a new raw schedule string, after following the GOTO/MAIL/NOT friendship keys in the game.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="date">The data to analyze.</param>
    /// <param name="rawData">The raw schedule string.</param>
    /// <param name="scheduleString">A raw schedule string, stripped of MAIL/GOTO/NOT elements. Ready to be parsed.</param>
    /// <returns>True if successful, false for error (skip to next schedule entry).</returns>
    private bool TryFindGOTOschedule(NPC npc, SDate date, string rawData, out string scheduleString)
    {
        scheduleString = string.Empty;
        string[] splits = rawData.Split(
            separator: '/',
            count: 3,
            options: StringSplitOptions.TrimEntries);
        string[] command = splits[0].Split();
        switch (command[0])
        {
            case "GOTO":
                // GOTO NO_SCHEDULE
                if (command[1].Equals("NO_SCHEDULE", StringComparison.Ordinal))
                {
                    return false;
                }
                string newKey = command[1];
                // GOTO season
                if (newKey.Equals("Season", StringComparison.OrdinalIgnoreCase))
                {
                    newKey = date.Season.ToLowerInvariant();
                }
                // GOTO newKey
                if (npc.hasMasterScheduleEntry(newKey))
                {
                    string newscheduleKey = npc.getMasterScheduleEntry(newKey);
                    if(newscheduleKey.Equals(rawData, StringComparison.InvariantCulture))
                    {
                        Globals.ModMonitor.Log(I18n.GOTOINFINITELOOP(), LogLevel.Warn);
                        return false;
                    }
                    return this.TryFindGOTOschedule(npc, date, newscheduleKey, out scheduleString);
                }
                else
                {
                    Globals.ModMonitor.Log(I18n.GOTOSCHEDULENOTFOUND(newKey, npc.Name), LogLevel.Warn);
                    return false;
                }
            case "NOT":
                // NOT friendship NPCName heartLevel
                if (command[1].Equals("friendship"))
                {
                    int hearts = Utility.GetAllPlayerFriendshipLevel(Game1.getCharacterFromName(command[2])) / 250;
                    if (!int.TryParse(command[3], out int heartLevel))
                    {
                        // ill formed friendship check string, warn
                        Globals.ModMonitor.Log(I18n.GOTOILLFORMEDFRIENDSHIP(splits[0], npc.Name, rawData), LogLevel.Warn);
                        return false;
                    }
                    else if (hearts > heartLevel)
                    {
                        // hearts above what's allowed, skip to next schedule.
                        Globals.ModMonitor.Log(I18n.GOTOSCHEDULEFRIENDSHIP(npc.Name, rawData), LogLevel.Trace);
                        return false;
                    }
                }
                scheduleString = rawData;
                return true;
            case "MAIL":
                // MAIL mailkey
                return Game1.MasterPlayer.mailReceived.Contains(command[1]) || NetWorldState.checkAnywhereForWorldStateID(command[1])
                    ? this.TryFindGOTOschedule(npc, date, splits[2], out scheduleString)
                    : this.TryFindGOTOschedule(npc, date, splits[1], out scheduleString);
            default:
                scheduleString = rawData;
                return true;
        }
    }
}