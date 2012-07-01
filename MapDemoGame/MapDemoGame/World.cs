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
/// The map is partially rendered based on this window every frame.
/// The view scrolls around the map at the player's control, providing movement.
/// The view "locks" and will not move beyond the edges of the map.
/// </summary>
public class World
{
    public Map Map { get; protected set; }
    public float TileWidth { get { return Map.TileWidth; } }
    public float TileHeight { get { return Map.TileWidth; } }
    public float WidthPx { get { return Map.WidthPx; } }
    public float HeightPx { get { return Map.HeightPx; } }
    public int ViewWidth { get; protected set; }
    public int ViewHeight { get; protected set; }

    public float ViewX { get; set; }
    public float ViewY { get; set; }
    public Rectangle ViewWindow { get { return new Rectangle((int)ViewX, (int)ViewY, ViewWidth, ViewHeight); } }
    public Vector2 ViewOffset{ get { return new Vector2(ViewX, ViewY); } }

    public World(Map map, Rectangle viewWindow)
    {
        Map = map;
        ViewX = viewWindow.X;
        ViewY = viewWindow.Y;
        ViewWidth = viewWindow.Width;
        ViewHeight = viewWindow.Height;
    }

    public void Draw(SpriteBatch sb)
    {
        Map.Draw(sb, ViewWindow);
    }

    public void CenterViewOnPlayer(Player p)
    {
        ViewX = Math.Max(p.WorldPosition.X - (ViewWidth / 2), 0);
        ViewY = Math.Max(p.WorldPosition.Y - (ViewHeight / 2), 0);
    }

    //maps a point from map pixel coordinates to screen coordinates by offsetting the current view
    public Vector2 WorldToScreenCoordinates(Vector2 worldCoords)
    {
        return worldCoords - ViewOffset;
    }
}