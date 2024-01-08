# Warcraft Source 2

## Requirements
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)
- [MetaMod:Source](http://www.sourcemm.net/)

## Installation
1. Download and set up the required dependencies.
2. Get the latest DLLs Zip from the [releases page](https://github.com/Vassili-Dev/Warcraft-Source-2/releases/)
3. Extract the contents of the zip to your server's `csgo/addons/counterstrikesharp/plugins` folder.
4. (Optional) Add any race packs to `csgo/addons/counterstrikesharp/WCS/RacePacks` (Race pack folder name must be the same as the `.dll` file name)
5. Get Playing!

## About RacePacks

This plugin Dynamically loads races from `WCS/RacePacks/{RacePackName}/{RacePackName}.dll`. In order for it to be loaded, it must have a metadata class that inherits from `IWarcraftRacePack`. It automatically loads all classes that inherit from `IWarcraftRace` if the metadata class is found.
Race packs try to use any shared dependencies that also exist in the `WCS` plugin. This means the racepack folder can be quite slim (In some cases, only a `.dll` file is required) if only shared dependencies are used.
