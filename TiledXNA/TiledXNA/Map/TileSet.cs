using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class TileSet
{
    //Tiled allows some image formats that XNA can't load from a stream (.tif, etc)
    private static readonly string[] validImageExtensions = { ".png", ".gif", ".jpg", ".jpeg" };

    public int FirstGID { get; private set; }
    public int TileWidthPx { get; private set; }
    public int TileHeightPx { get; private set; }
    public Color? TransparentColor { get; private set; }
    public Texture2D Texture { get; private set; }

    //width of tileset in tiles
    private int Width { get { return Texture.Width / TileWidthPx; } }
    private int Height { get { return Texture.Height / TileHeightPx; } }

    public TileSet(string tmxDirName, Tiled.tileset tileset)
    {
        if (tileset.source != null)
        {
            //TODO: implement external tilesets?
            throw new FormatException("External tilesets are not supported. Use a standard image tileset instead.");
        }

        FirstGID = int.Parse(tileset.firstgid);
        TileWidthPx = int.Parse(tileset.tilewidth);
        TileHeightPx = int.Parse(tileset.tileheight);

        //NOTE: only using the first image... Tiled Java allows multiple images in a single tileset, but Tiled Qt does not
        Tiled.tilesetImage tilesetImage = tileset.image[0];

        if (!string.IsNullOrWhiteSpace(tilesetImage.trans))
            TransparentColor = Util.ColorFromHexString(tilesetImage.trans);

        string imageFileExt = Path.GetExtension(tilesetImage.source);
        if (!validImageExtensions.Contains(imageFileExt))
            throw new Exception(String.Format("Unsupported source format \"{0}\" for tileset \"{1}\". Supported formats are {2}", 
                imageFileExt, tileset.name, string.Join(",", validImageExtensions)));

        //offset the tileset image's path by the tmx file's path (since the image is relative to the tmx)
        string pathToTilesetImage = Path.Combine(tmxDirName, tilesetImage.source);
        using (FileStream fstream = new FileStream(pathToTilesetImage, FileMode.Open))
        {
            GraphicsDevice gd = ServiceLocator.Get<GraphicsDevice>();
            Texture = Texture2D.FromStream(gd, fstream);
        }

        if (TransparentColor != null)
            Texture = Util.ApplyColorKeyTransparency(Texture, TransparentColor.Value);
    }

    //determine the crop rectangle of a tile by its GID offset
    public Rectangle DetermineTileCropRect(int gidOffset)
    {
        int x = (gidOffset - 1) % Width;
        int y = (gidOffset - 1) / Width;
        return new Rectangle(x * TileWidthPx, y * TileHeightPx, TileWidthPx, TileHeightPx);
    }

    ////crop out the tile texture at the given offset
    //public Texture2D GetTileByGIDOffset(int gidOffset)
    //{
    //    GraphicsDevice gd = Texture.GraphicsDevice;
    //    RenderTarget2D tile = new RenderTarget2D(gd, TileWidth, TileHeight);
    //    SpriteBatch sb = new SpriteBatch(gd);
    //    Rectangle tileCropRect = DetermineTileCropRect(gidOffset);

    //    gd.SetRenderTarget(tile);   //draw to the tile image, not the screen
    //    gd.Clear(Color.Black);

    //    sb.Begin();
    //    sb.Draw(Texture, tile.Bounds, tileCropRect, Color.White);
    //    sb.End();

    //    gd.SetRenderTarget(null);   //draw to the screen again

    //    return (Texture2D)tile;
    //}
}

public static class TileSetExtensions
{
    public static void ResolveTileGID(this List<TileSet> tilesets, uint tileGID, out TileSet tileset, out Rectangle tileRect)
    {
        foreach (TileSet ts in tilesets.OrderByDescending(t => t.FirstGID))
        {
            if (ts.FirstGID <= tileGID)
            {
                tileset = ts;
                tileRect = ts.DetermineTileCropRect((int)(tileGID - tileset.FirstGID + 1));
                return;
            }
        }

        tileRect = Rectangle.Empty;
        tileset = null;
    }
}