Random, Refreshed
=================================
So I got nerdsniped, okay?

It's long bothered me how weirdly streaky Stardew's RNG seems to be. So I finally did something about it.

## Install

1. Install the latest version of [SMAPI](https://smapi.io).
2. Download this mod and unzip it into `Stardew Valley/Mods`.
3. Run the game using SMAPI.

## Config

## Usage

**NOTE**: Many features will automatically disable themselves if `UseLegacyRNG` is selected in the game options. After all, that's the way to opt into an intentionally shoddy RNG that's more manipulatable.

## API

This mod overrides most of the 

## Compatibility

* Works with Stardew Valley 1.6.9+ on Linux/macOS/Windows.
* In multiplayer: It is best to install on all players.
* If you have a mod that tries to predict the outcome of anything, this mod may cause that mod to mispredict. Also, this mod is not compatible with [Stardew Predictor](https://mouseypounds.github.io/stardew-predictor/), for obvious reasons.

## Analysis

### It's probably not the PRNG.

There are two previous mods that try to "fix" the Stardew PRNG. Both do so by replacing `Game1.random` with a `Random` of their own implementation. They do this because the NET random, pre-NET-6.0, is notoriously bad. But it's...probably fine for `Game1.random`. After all, it's mostly used for generating doubles (where the lowest order bits do not matter).

The actual problem is when the game doesn't use `Game1.random`. In order to have consistency between loads of the same day, the game quite often creates new `Random` instances, seeded with some combination of the save's uniqueID, the current number of days played, and maybe a few other factors. It will then use the random once or a few times before discarding it.

## See also

* [Changelog](docs/Changelog.md)
