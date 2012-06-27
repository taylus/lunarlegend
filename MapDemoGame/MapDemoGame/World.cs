using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Property = System.Collections.Generic.KeyValuePair<string, string>;

/// <summary>
/// Represents a concept of a sliding window "view" on top of a map.
/// The view scrolls around the map at the player's control, providing movement.
/// The view "locks" and will not move beyond the edges of the map.
/// 
/// The underlying map is also rendered to an image, which is able to be scaled.
/// </summary>
public class World
{
    private GraphicsDevice graphicsDevice;

    public Map Map { get; protected set; }
    public Texture2D MapSurface { get; protected set; }
    public float Scale { get; set; }
    public float TileWidth { get { return Map.TileWidth * Scale; } }
    public float TileHeight { get { return Map.TileWidth * Scale; } }
    public float WidthPx { get { return Map.WidthPx * Scale; } }
    public float HeightPx { get { return Map.HeightPx * Scale; } }
    public int ViewWidth { get { return graphicsDevice.Viewport.Width; } }
    public int ViewHeight { get { return graphicsDevice.Viewport.Height; } }

    public float ViewX { get; set; }
    public float ViewY { get; set; }
    public Rectangle ViewWindow { get { return new Rectangle((int)ViewX, (int)ViewY, ViewWidth, ViewHeight); } }
    public Vector2 ViewOffset
    {
        get
        {
            return new Vector2(ViewX, ViewY);
        }
        set
        {
            ViewX = value.X;
            ViewY = value.Y;
        }
    }

    public World(Map map, float scale, Vector2 viewOffset, GraphicsDevice gd)
    {
        Map = map;
        Scale = scale;
        ViewOffset = viewOffset;
        graphicsDevice = gd;
    }

    //render the map to the temporary surface
    public void RenderMap(SpriteBatch sb)
    {
        //TODO: get rid of this step, see note in Draw()
        //render map to a temporary surface at normal scale
        GraphicsDevice gd = sb.GraphicsDevice;
        RenderTarget2D mapSurf = new RenderTarget2D(gd, Map.WidthPx, Map.HeightPx);
        gd.SetRenderTarget(mapSurf);
        gd.Clear(Color.Transparent);
        sb.Begin();
        Map.Draw(sb, false);
        sb.End();

        //rescale the rendered map image
        RenderTarget2D mapSurfScaled = new RenderTarget2D(gd, (int)Math.Ceiling(Map.WidthPx * Scale), (int)(Math.Ceiling(Map.HeightPx * Scale)));
        gd.SetRenderTarget(mapSurfScaled);
        gd.Clear(Color.Transparent);
        sb.Begin();
        sb.Draw(mapSurf, Vector2.Zero, null, Color.White, 0.0f, Vector2.Zero, Scale, SpriteEffects.None, 0);
        sb.End();

        //draw to the screen again
        gd.SetRenderTarget(null);

        MapSurface = (Texture2D)mapSurfScaled;
    }

    public void Draw(SpriteBatch sb)
    {
        //TODO: zoom by making view window a smaller ratio of the viewport bounds!
        sb.Draw(MapSurface, sb.GraphicsDevice.Viewport.Bounds, ViewWindow, Color.White);
    }

    public void CenterViewOnPlayer(Player p)
    {

    }

    //maps a point from map pixel coordinates to screen coordinates using the given scale and view
    public Vector2 WorldToScreenCoordinates(Vector2 worldCoords)
    {
        return (worldCoords * Scale) - ViewOffset;
    }
}