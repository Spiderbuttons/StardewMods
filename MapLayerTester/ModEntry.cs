using System.Buffers;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using xTile;
using xTile.Dimensions;

namespace MapLayerTester;
public class ModEntry : Mod
{
    private Texture2D? magenta;

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += this.GameLoop_GameLaunched;
        helper.Events.Content.AssetRequested += this.Content_AssetRequested;
    }

    private void Content_AssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Maps/MapTest/Magenta"))
        {
            e.LoadFrom(() => this.magenta!, AssetLoadPriority.Exclusive);
        }

        if (e.DataType != typeof(Map))
            return;

        if (this.magenta is null)
            return;

        e.Edit(
            (asset) =>
            {
                var map = asset.AsMap().Data;
                if (!map.TileSheets.Any(static ts => ts.Id == "0"))
                {
                    this.Monitor.Log($"Adding magenta to {map.assetPath}");
                    map.AddTileSheet(new xTile.Tiles.TileSheet("0", map, "Maps/MapTest/Magenta", new Size(16, 16), new Size(16, 16)));
                }
            }, 
            (AssetEditPriority)int.MaxValue);
    }

    private void GameLoop_GameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.magenta = new (Game1.game1.GraphicsDevice, 16, 16);

        var array = ArrayPool<Color>.Shared.Rent(16 * 16);
        Array.Fill(array, Color.Magenta);

        this.magenta.SetData(array, 0, 16 * 16);
        ArrayPool<Color>.Shared.Return(array);
    }
}
