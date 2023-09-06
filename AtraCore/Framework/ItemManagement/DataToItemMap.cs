﻿using System.Runtime.InteropServices;

using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Wrappers;

using CommunityToolkit.Diagnostics;

using StardewValley.GameData.Pants;
using StardewValley.GameData.Shirts;

namespace AtraCore.Framework.ItemManagement;

/// <summary>
/// Handles looking up the id of an item by its name and type.
/// </summary>
public static class DataToItemMap
{
    private static readonly SortedList<ItemTypeEnum, IAssetName> enumToAssetMap = new(8);

    private static readonly SortedList<ItemTypeEnum, Lazy<Dictionary<string, (string id, bool repeat)>>> nameToIDMap = new(9);

    /// <summary>
    /// Given an ItemType and a name, gets the id.
    /// </summary>
    /// <param name="type">type of the item.</param>
    /// <param name="name">name of the item.</param>
    /// <param name="resolveRecipesSeparately">Whether or not to ignore the recipe bit.</param>
    /// <returns>string ID, or null if not found.</returns>
    public static string? GetID(ItemTypeEnum type, string name, bool resolveRecipesSeparately = false)
    {
        Guard.IsNotNullOrWhiteSpace(name, nameof(name));

        if (!resolveRecipesSeparately)
        {
            type &= ~ItemTypeEnum.Recipe;
        }
        if (type == ItemTypeEnum.ColoredSObject)
        {
            type = ItemTypeEnum.SObject;
        }
#pragma warning disable CS0618 // Type or member is obsolete - special handling for obsolete former member.
        if (type == ItemTypeEnum.Clothing)
        {
            ModEntry.ModMonitor.LogOnce($"Searches for clothing are deprecated as of Stardew 1.6. Please specify Shirts or Pants separately.", LogLevel.Warn);
            return GetID(ItemTypeEnum.Pants, name, resolveRecipesSeparately) ?? GetID(ItemTypeEnum.Shirts, name, resolveRecipesSeparately);
        }
#pragma warning restore CS0618 // Type or member is obsolete
        if (nameToIDMap.TryGetValue(type, out Lazy<Dictionary<string, (string, bool)>>? asset)
            && asset.Value.TryGetValue(name, out (string id, bool repeat) pair))
        {
            if (pair.repeat)
            {
                ModEntry.ModMonitor.LogOnce($"Internal name '{name}' corresponds to multiple {type} and may not be resolved correctly.", LogLevel.Warn);
            }
            return pair.id;
        }
        return null;
    }

    /// <summary>
    /// Sets up various maps.
    /// </summary>
    /// <param name="helper">GameContentHelper.</param>
    internal static void Init(IGameContentHelper helper)
    {
        // Populate item-to-asset-enumToAssetMap.
        // Note: Rings are in ObjectInformation, because
        // nothing is nice. So are boots, but they have their own data asset as well.
        enumToAssetMap.Add(ItemTypeEnum.BigCraftable, helper.ParseAssetName(@"Data\BigCraftablesInformation"));
        enumToAssetMap.Add(ItemTypeEnum.Boots, helper.ParseAssetName(@"Data\Boots"));
        enumToAssetMap.Add(ItemTypeEnum.Shirts, helper.ParseAssetName(@"Data\Shirts"));
        enumToAssetMap.Add(ItemTypeEnum.Pants, helper.ParseAssetName(@"Data\Pants"));
        enumToAssetMap.Add(ItemTypeEnum.Furniture, helper.ParseAssetName(@"Data\Furniture"));
        enumToAssetMap.Add(ItemTypeEnum.Hat, helper.ParseAssetName(@"Data\hats"));
        enumToAssetMap.Add(ItemTypeEnum.SObject, helper.ParseAssetName(@"Data\ObjectInformation"));
        enumToAssetMap.Add(ItemTypeEnum.Weapon, helper.ParseAssetName(@"Data\weapons"));

        // load the lazies.
        Reset();
    }

    /// <summary>
    /// Resets the requested name-to-id maps.
    /// </summary>
    /// <param name="assets">Assets to reset, or null for all.</param>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        bool ShouldReset(IAssetName name) => assets is null || assets.Contains(name);

        if (ShouldReset(enumToAssetMap[ItemTypeEnum.SObject]))
        {
            if (!nameToIDMap.TryGetValue(ItemTypeEnum.SObject, out var sobj) || sobj.IsValueCreated)
            {
                nameToIDMap[ItemTypeEnum.SObject] = new(() =>
                {
                    ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve normal objects.", LogLevel.Info);

                    Dictionary<string, (string id, bool duplicate)> mapping = new(Game1Wrappers.ObjectInfo.Count)
                    {
                        // Special cases
                        ["Brown Egg"] = ("180", false),
                        ["Large Brown Egg"] = ("182", false),
                        ["Strange Doll 2"] = ("127", false),
                    };

                    HashSet<string> preAdded = mapping.Values.Select(pair => pair.id).ToHashSet();

                    // Processing from the data.
                    foreach ((string id, string data) in Game1Wrappers.ObjectInfo)
                    {
                        if (ItemHelperUtils.ObjectFilter(id, data) || preAdded.Contains(id))
                        {
                            continue;
                        }

                        string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                        if (name.Length == 0)
                        {
                            ModEntry.ModMonitor.Log($"Object with id {id} has no internal name.");
                            continue;
                        }
                        var val = CollectionsMarshal.GetValueRefOrAddDefault(mapping, name, out bool exists);
                        if (exists)
                        {
                            val.duplicate = true;
                        }
                        else
                        {
                            val = new(id, false);
                        }
                    }
                    return mapping;
                });
            }
            if (!nameToIDMap.TryGetValue(ItemTypeEnum.Ring, out var rings) || rings.IsValueCreated)
            {
                nameToIDMap[ItemTypeEnum.Ring] = new(() =>
                {
                    ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve rings.", LogLevel.Info);

                    Dictionary<string, (string id, bool duplicate)> mapping = new(10);
                    foreach ((string id, string data) in Game1Wrappers.ObjectInfo)
                    {
                        ReadOnlySpan<char> cat = data.GetNthChunk('/', 3);

                        // wedding ring (801) isn't a real ring.
                        // JA rings are registered as "Basic -96"
                        if (id == "801" || (!cat.Equals("Ring", StringComparison.Ordinal) && !cat.Equals("Basic -96", StringComparison.Ordinal)))
                        {
                            continue;
                        }

                        string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                        if (name.Length == 0)
                        {
                            ModEntry.ModMonitor.Log($"Ring with id {id} has no internal name.");
                            continue;
                        }
                        var val = CollectionsMarshal.GetValueRefOrAddDefault(mapping, name, out bool exists);
                        if (exists)
                        {
                            val.duplicate = true;
                        }
                        else
                        {
                            val = new(id, false);
                        }
                    }
                    return mapping;
                });
            }
        }

        if (ShouldReset(enumToAssetMap[ItemTypeEnum.Boots])
            && (!nameToIDMap.TryGetValue(ItemTypeEnum.Boots, out var boots) || boots.IsValueCreated))
        {
            nameToIDMap[ItemTypeEnum.Boots] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve Boots", LogLevel.Info);

                Dictionary<string, (string id, bool duplicate)> mapping = new(20);
                foreach ((string id, string data) in Game1.content.Load<Dictionary<string, string>>(enumToAssetMap[ItemTypeEnum.Boots].BaseName))
                {
                    string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                    if (name.Length == 0)
                    {
                        ModEntry.ModMonitor.Log($"Boots with id {id} has no internal name.");
                        continue;
                    }
                    var val = CollectionsMarshal.GetValueRefOrAddDefault(mapping, name, out bool exists);
                    if (exists)
                    {
                        val.duplicate = true;
                    }
                    else
                    {
                        val = new(id, false);
                    }
                }
                return mapping;
            });
        }
        if (ShouldReset(enumToAssetMap[ItemTypeEnum.BigCraftable])
            && (!nameToIDMap.TryGetValue(ItemTypeEnum.BigCraftable, out var bc) || bc.IsValueCreated))
        {
            nameToIDMap[ItemTypeEnum.BigCraftable] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve BigCraftables", LogLevel.Info);

                Dictionary<string, (string id, bool duplicate)> mapping = new(Game1.bigCraftablesInformation.Count)
                {
                    // special cases
                    ["Tub o' Flowers"] = Game1.season is Season.Fall or Season.Winter ? ("109", false) : ("108", false),
                    ["Rarecrow 1"] = ("110", false),
                    ["Rarecrow 2"] = ("113", false),
                    ["Rarecrow 3"] = ("126", false),
                    ["Rarecrow 4"] = ("136", false),
                    ["Rarecrow 5"] = ("137", false),
                    ["Rarecrow 6"] = ("138", false),
                    ["Rarecrow 7"] = ("139", false),
                    ["Rarecrow 8"] = ("140", false),
                    ["Seasonal Plant 1"] = ("188", false),
                    ["Seasonal Plant 2"] = ("192", false),
                    ["Seasonal Plant 3"] = ("196", false),
                    ["Seasonal Plant 4"] = ("200", false),
                    ["Seasonal Plant 5"] = ("204", false),
                };

                // House plants :P
                for (int i = 1; i <= 7; i++)
                {
                    mapping["House Plant " + i.ToString()] = (i.ToString(), false);
                }
                HashSet<string> preAdded = mapping.Values.Select(pair => pair.id).ToHashSet();
                preAdded.Add("108");
                preAdded.Add("109");

                foreach ((string id, string data) in Game1.bigCraftablesInformation)
                {
                    if (preAdded.Contains(id) || ItemHelperUtils.BigCraftableFilter(id, data))
                    {
                        continue;
                    }

                    string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                    if (name.Length == 0)
                    {
                        ModEntry.ModMonitor.Log($"BigCraftable with id {id} has no internal name.");
                        continue;
                    }
                    var val = CollectionsMarshal.GetValueRefOrAddDefault(mapping, name, out bool exists);
                    if (exists)
                    {
                        val.duplicate = true;
                    }
                    else
                    {
                        val = new(id, false);
                    }
                }
                return mapping;
            });
        }
        if (ShouldReset(enumToAssetMap[ItemTypeEnum.Shirts])
            && (!nameToIDMap.TryGetValue(ItemTypeEnum.Shirts, out var shirts) || shirts.IsValueCreated))
        {
            nameToIDMap[ItemTypeEnum.Shirts] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve Shirts.", LogLevel.Info);

                Dictionary<string, (string id, bool duplicate)> mapping = new(Game1.shirtData.Count)
                {
                    ["Shirt 1"] = ("1022", false),
                    ["Shirt 2"] = ("1023", false),
                    ["Dark Prismatic Shirt"] = ("1998", false),
                };
                HashSet<string> preAdded = mapping.Values.Select(pair => pair.id).ToHashSet();

                foreach ((string id, ShirtData? data) in Game1.shirtData)
                {
                    if (Game1.pantsData.ContainsKey(id))
                    {
                        ModEntry.ModMonitor.LogOnce($"ID '{id}' is shared between pants and shirts, this is likely to cause issues.", LogLevel.Warn);
                    }

                    if (preAdded.Contains(id))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(data.Name))
                    {
                        ModEntry.ModMonitor.Log($"Shirt with id {id} has no internal name.");
                        continue;
                    }
                    var val = CollectionsMarshal.GetValueRefOrAddDefault(mapping, data.Name, out bool exists);
                    if (exists)
                    {
                        val.duplicate = true;
                    }
                    else
                    {
                        val = new(id, false);
                    }
                }
                return mapping;
            });
        }
        if (ShouldReset(enumToAssetMap[ItemTypeEnum.Pants])
            && (!nameToIDMap.TryGetValue(ItemTypeEnum.Pants, out var pants) || pants.IsValueCreated))
        {
            nameToIDMap[ItemTypeEnum.Pants] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve Pants.", LogLevel.Info);

                Dictionary<string, (string id, bool duplicate)> mapping = new(Game1.pantsData.Count);

                foreach ((string id, PantsData? data) in Game1.pantsData)
                {
                    if (string.IsNullOrEmpty(data.Name))
                    {
                        ModEntry.ModMonitor.Log($"Pants with id {id} has no internal name.");
                        continue;
                    }
                    var val = CollectionsMarshal.GetValueRefOrAddDefault(mapping, data.Name, out bool exists);
                    if (exists)
                    {
                        val.duplicate = true;
                    }
                    else
                    {
                        val = new(id, false);
                    }
                }
                return mapping;
            });
        }
        if (ShouldReset(enumToAssetMap[ItemTypeEnum.Furniture])
            && (!nameToIDMap.TryGetValue(ItemTypeEnum.Furniture, out var furniture) || furniture.IsValueCreated))
        {
            nameToIDMap[ItemTypeEnum.Furniture] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve Furniture", LogLevel.Info);

                Dictionary<string, (string id, bool duplicate)> mapping = new(300);
                foreach ((string id, string data) in Game1.content.Load<Dictionary<string, string>>(enumToAssetMap[ItemTypeEnum.Furniture].BaseName))
                {
                    string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                    if (name.Length == 0)
                    {
                        ModEntry.ModMonitor.Log($"Furniture with id {id} has no internal name.");
                        continue;
                    }
                    var val = CollectionsMarshal.GetValueRefOrAddDefault(mapping, name, out bool exists);
                    if (exists)
                    {
                        val.duplicate = true;
                    }
                    else
                    {
                        val = new(id, false);
                    }
                }
                return mapping;
            });
        }
        if (ShouldReset(enumToAssetMap[ItemTypeEnum.Hat])
            && (!nameToIDMap.TryGetValue(ItemTypeEnum.Hat, out var hats) || hats.IsValueCreated))
        {
            nameToIDMap[ItemTypeEnum.Hat] = new(() =>
            {
                ModEntry.ModMonitor.DebugOnlyLog("Building map to resolve Hats", LogLevel.Info);

                Dictionary<string, (string id, bool duplicate)> mapping = new(100);

                foreach ((string id, string data) in Game1.content.Load<Dictionary<string, string>>(enumToAssetMap[ItemTypeEnum.Hat].BaseName))
                {
                    string name = data.GetNthChunk('/', SObject.objectInfoNameIndex).ToString();
                    if (name.Length == 0)
                    {
                        ModEntry.ModMonitor.Log($"Hat with id {id} has no internal name.");
                        continue;
                    }
                    var val = CollectionsMarshal.GetValueRefOrAddDefault(mapping, name, out bool exists);
                    if (exists)
                    {
                        val.duplicate = true;
                    }
                    else
                    {
                        val = new(id, false);
                    }
                }
                return mapping;
            });
        }
    }
}
