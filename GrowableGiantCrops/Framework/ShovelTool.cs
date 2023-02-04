﻿using System.Xml.Serialization;

using AtraCore.Utilities;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// A shovel.
/// </summary>
[XmlType("Mods_atravita_Shovel")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Like methods are grouped together.")]
public sealed class ShovelTool : GenericTool
{
    public ShovelTool()
        : base(I18n.Shovel_Name(), I18n.Shovel_Description(), 0, 0, 0)
    {
        this.Stackable = false;
    }

    /// <inheritdoc />
    public override Item getOne()
    {
        ShovelTool newShovel = new();
        newShovel._GetOneFrom(this);
        return newShovel;
    }

    #region functionality

    /// <inheritdoc />
    public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
    {
        // use the watering can arms.
        who.jitterStrength = 0.25f;
        switch (who.FacingDirection)
        {
            case Game1.up:
                who.FarmerSprite.setCurrentFrame(180);
                break;
            case Game1.right:
                who.FarmerSprite.setCurrentFrame(172);
                break;
            case Game1.down:
                who.FarmerSprite.setCurrentFrame(164);
                break;
            case Game1.left:
                who.FarmerSprite.setCurrentFrame(188);
                break;
        }
        this.Update(who.FacingDirection, 0, who);
        return false;
    }

    /// <inheritdoc />
    public override void endUsing(GameLocation location, Farmer who)
    {
        who.stopJittering();
        who.canReleaseTool = false;

        // use the watering can arms.
        switch (who.FacingDirection)
        {
            case 2:
                ((FarmerSprite)who.Sprite).animateOnce(164, 125f, 3);
                break;
            case 1:
                ((FarmerSprite)who.Sprite).animateOnce(172, 125f, 3);
                break;
            case 0:
                ((FarmerSprite)who.Sprite).animateOnce(180, 125f, 3);
                break;
            case 3:
                ((FarmerSprite)who.Sprite).animateOnce(188, 125f, 3);
                break;
        }
    }

    /// <summary>
    /// Does the actual tool function.
    /// </summary>
    /// <param name="location">The game location.</param>
    /// <param name="x">pixel x.</param>
    /// <param name="y">pixel y.</param>
    /// <param name="power">The power level of the tool</param>
    /// <param name="who">Last farmer to use.</param>
    public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
    {
        base.DoFunction(location, x, y, power, who);

        Vector2 pickupTile = new(x / 64, y / 64);
        if (ModEntry.GrowableBushesAPI?.TryPickUpBush(location, pickupTile) is SObject bush)
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Picking up bush {bush.Name}", LogLevel.Info);
            if (!who.addItemToInventoryBool(bush))
            {
                location.debris.Add(new Debris(bush, who.Position));
            }
            ModEntry.GrowableBushesAPI.DrawPickUpGraphics(bush, location, bush.TileLocation);
            who.Stamina -= ModEntry.Config.ShovelEnergy;
            return;
        }

        for (int i = location.resourceClumps.Count - 1; i >= 0; i--)
        {
            ResourceClump? clump = location.resourceClumps[i];
            if (clump is null || !clump.getBoundingBox(clump.tile.Value).Contains(x,y))
            {
                continue;
            }
            SObject? item = null;
            switch (clump)
            {
                case GiantCrop giant:
                {
                    InventoryGiantCrop? inventoryGiantCrop = null;
                    if (giant.modData.TryGetValue(InventoryGiantCrop.GiantCropTweaksModDataKey, out string? stringID)
                        && ModEntry.GiantCropTweaksAPI?.GiantCrops.ContainsKey(stringID) == true)
                    {
                        inventoryGiantCrop = new InventoryGiantCrop(stringID, giant.parentSheetIndex.Value, 1);
                    }
                    else if (InventoryGiantCrop.IsValidGiantCropIndex(giant.parentSheetIndex.Value))
                    {
                        inventoryGiantCrop = new InventoryGiantCrop(giant.parentSheetIndex.Value, 1);
                    }

                    item = inventoryGiantCrop;
                    if (inventoryGiantCrop is not null)
                    {
                        AddAnimations(location, giant.tile.Value, inventoryGiantCrop.TexturePath, inventoryGiantCrop.SourceRect, inventoryGiantCrop.TileSize);
                    }
                    break;
                }
                case ResourceClump resource:
                    ResourceClumpIndexes idx = (ResourceClumpIndexes)resource.parentSheetIndex.Value;
                    if (idx != ResourceClumpIndexes.Invalid && ResourceClumpIndexesExtensions.IsDefined(idx))
                    {
                        InventoryResourceClump inventoryResourceClump = new(idx, 1);
                        item = inventoryResourceClump;
                        AddAnimations(location, resource.tile.Value, Game1.objectSpriteSheetName, inventoryResourceClump.SourceRect, new Point(2, 2));
                    }
                    break;
            }

            if (item is not null)
            {
                ModEntry.ModMonitor.DebugOnlyLog($"Picking up {item.Name}", LogLevel.Info);
                if (!who.addItemToInventoryBool(item))
                {
                    location.debris.Add(new Debris(item, who.Position));
                }
                who.Stamina -= ModEntry.Config.ShovelEnergy;
                location.resourceClumps[i].performToolAction(this, 0, pickupTile, location);
                location.resourceClumps.RemoveAt(i);
                break;
            }
        }
    }

    /// <inheritdoc />
    public override bool onRelease(GameLocation location, int x, int y, Farmer who) => false;

    #endregion

    #region display

    /// <inheritdoc />
    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        spriteBatch.Draw(
            texture: AssetManager.ToolTexture,
            position: location + new Vector2(32f, 32f),
            new Rectangle(96, 16, 16, 16),
            color: color * transparency,
            rotation: 0f,
            new Vector2(8f, 8f),
            scale: 4f * scaleSize,
            effects: SpriteEffects.None,
            layerDepth);
    }

    /// <inheritdoc />
    protected override string loadDisplayName() => I18n.Shovel_Name();

    /// <inheritdoc />
    protected override string loadDescription() => I18n.Shovel_Description();

    #endregion

    #region misc

    /// <inheritdoc />
    /// <remarks>disallow forging.</remarks>
    public override bool CanForge(Item item) => false;

    /// <inheritdoc />
    /// <remarks>disallow stacking.</remarks>
    public override int maximumStackSize() => -1;

    /// <inheritdoc />
    /// <remarks>nop this.</remarks>
    public override void actionWhenClaimed()
    {
    }

    /// <inheritdoc />
    /// <remarks>forbid attachments.</remarks>
    public override int attachmentSlots() => 0;

    /// <inheritdoc />
    /// <remarks>forbid attachments.</remarks>
    public override bool canThisBeAttached(SObject o) => false;

    /// <inheritdoc />
    /// <remarks>forbid attachments.</remarks>
    public override SObject attach(SObject o) => o;

    #endregion

    #region helpers
    internal static void AddAnimations(GameLocation loc, Vector2 tile, string? texturePath, Rectangle sourceRect, Point tileSize)
    {
        if (texturePath is null)
        {
            return;
        }

        Multiplayer mp = MultiplayerHelpers.GetMultiplayer();

        const float deltaY = -90;
        const float gravity = 0.0025f;

        float velocity = -0.7f - MathF.Sqrt(2 * 60f * gravity);
        float time = (MathF.Sqrt((velocity * velocity) - (gravity * deltaY * 2f)) / gravity) - (velocity / gravity);

        Vector2 landingPos = new Vector2(tile.X + (tileSize.X / 2f) - 1, tile.Y + tileSize.Y - 1) * 64f;

        TemporaryAnimatedSprite objTas = new(
            textureName: texturePath,
            sourceRect: sourceRect,
            position: tile * 64f,
            flipped: false,
            alphaFade: 0f,
            color: Color.White)
        {
            totalNumberOfLoops = 1,
            interval = time,
            acceleration = new Vector2(0f, gravity),
            motion = new Vector2(0f, velocity),
            scale = Game1.pixelZoom,
            timeBasedMotion = true,
            rotation = 0.1f,
            rotationChange = 0.1f,
            scaleChange = -0.0015f,
            layerDepth = (landingPos.Y + 32f) / 10000f,
        };

        TemporaryAnimatedSprite? dustTas = new(
            textureName: Game1.mouseCursorsName,
            sourceRect: new Rectangle(464, 1792, 16, 16),
            animationInterval: 120f,
            animationLength: 5,
            numberOfLoops: 0,
            position: landingPos,
            flicker: false,
            flipped: Game1.random.NextDouble() < 0.5,
            layerDepth: (landingPos.Y + 40f) / 10000f,
            alphaFade: 0.01f,
            color: Color.White,
            scale: Game1.pixelZoom,
            scaleChange: 0.02f,
            rotation: 0f,
            rotationChange: 0f)
        {
            light = true,
            delayBeforeAnimationStart = Math.Max((int)time - 10, 0),
        };

        mp.broadcastSprites(loc, objTas, dustTas);
    }

    #endregion
}
