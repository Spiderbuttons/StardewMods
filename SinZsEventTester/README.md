Event Tester
=================================

Helps you test events by running them automatically.

Basically, 1.6 has made event parsing much, much stricter. And there's no good way to test if your events work unless you just...run all of them. Which can take some time. But we can automate it! This mod simply plays events automatically, including every branch, to check for parsing errors. It does so at configurable speed.

## Install

1. Install the latest version of [SMAPI](https://smapi.io).
2. Download this mod and unzip it into `Stardew Valley/Mods`.
3. Run the game using SMAPI.

## Config

1. `EventSpeedRatio` defines how fast the mod should speed up events it launched.
2. `FastForwardRatio` defines how fast the entire game state should be sped up if fastforward is enabled.
1. `FastForwardKeybind` defines the button used for the fast forward state.
1. `AllowCheats` enables the game's debug commands to be used via the chatbox.

## Usage

The mod provides the following console commands

Command | Usage | Example
--------|-------|-----------
`sinz.playevents [location]`| Plays the events in: <ul><li>The current location, if no location is given.<li>All locations, if the keyword `all` is given.<li>The specified location, if a specific location name is given (a fuzzy search).<li>A single partial match token `*` is supported (ie, you can do `Island*` to queue events for all locations that start with `Island`).</ul> | <ul><li>`sinz.playevents ALL` plays all events.<li>`sinz.playevents Island*` plays events in locations that start with `Island`<li>`sinz.playevents Forest` plays events in the forest.</ul>
`sinz.eventbyid [event_key]` | Like the game's `debug ebi`, but runs automatically, at faster speeds, and also selects every choice. End the event key with `*` to play all events that START WITH a specific event key.| <ul><li>`sinz.eventbyid 4` to play the event with ID `4`. <li>`sinz.eventbyid atravita.CP.PamTries*` to play all events that have an event key that starts with `atravita.CP.PamTries`</ul>
|`sinz.empty_event_queue` | Empties the event queue to stop autoplay.
`sinz.check_preconditions` | Checks over all the event preconditions in the game to make sure they're formatted correctly
`sinz.check_gsq` | Checks over all the GSQ in the game to make sure they're formatted correctly
`sinz.forget_mail <mail>` | Removes the mail key from the player's `mailRecieved`.
`sinz.forget_event <eventID>` | Removes the event key from the player's `eventsSeen`
`sinz.forget_triggers <triggerID>` | Removes the trigger ID from the player's `triggerActionsRun`
`sinz.fast_forward` |  Toggles fast forward state.
`sinz.fast_forward <int>` | Sets the fast forward rate to the given amount. If 1 or lower, disables fast forward. | `sinz.fast_forward 7`
`sinz.gc` | Prints memory usage to the log.
`sinz.gc true` | Prints memory usage to the log, and calls the dotnet gc.
`sinz.monitor_performance` | Toggles the presence of the performance monitor.
`sinz.check_dialogue` | Warps the player around to every NPC, displaying the dialogue they have today, and writing that to the log.
`sinz.check_dialogue <npc names>+` | Warps the player to the listed NPC and plays every dialogue they have in their file, in order to check for parsing issues | `sinz.check_dialogue Pam`

Additionally the mod adds the following commands to the game's own debug commands. With `AllowCheats` enabled, these are available via the chatbox.

Command | Usage | Example
--------|-------|--------
`smapicommand` | Runs any other console command. Mostly useful for chat box debugging, also allows for console commands in general to be used in the game's `\runmacro`.
`fastforward` | Toggles fast forward mode.
`fastforward <int>` | Sets the fast forward to a specific rate. If 1 or less, disables fast forward.
`gc` | Prints memory usage
`gc true` | Prints memory usage, and calls the dotnet gc.

## C# API
As of version 0.1.2, mods can add their own assets to the GSQ checker. Please see documentation at [the API interface](../IEventTesterAPI.cs).

## Compatibility

* Works with Stardew Valley 1.6 +on Linux/macOS/Windows.
* Honestly, if you're testing mods, you're probably playing singleplayer and launching the game every third minute or something. Thus, only really tested in singleplayer. Very likely fine in multiplayer, however, very likely will do something weird in splitscreen.

## See also

* [Changelog](docs/Changelog.md)
