﻿using System;
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
    public int TileWidth { get { return Map.TileWidth; } }
    public int TileHeight { get { return Map.TileWidth; } }
    public int WidthPx { get { return Map.WidthPx; } }
    public int HeightPx { get { return Map.HeightPx; } }
    public Layer CollisionLayer { get { return Map.CollisionLayer; } }
    public float ViewX { get; set; }
    public float ViewY { get; set; }
    public int ViewWidth { get; protected set; }
    public int ViewHeight { get; protected set; }
    public Rectangle ViewWindow { get { return new Rectangle((int)Math.Round(ViewX), (int)Math.Round(ViewY), ViewWidth, ViewHeight); } }
    public Vector2 ViewOffset{ get { return new Vector2(ViewX, ViewY); } }
    public List<Entity> Entities { get; protected set; }
    public bool Debug { get; set; }

    //singleton, so other classes (namely WorldEntity) don't need to keep copies
    public static World Current { get; protected set; }

    private World(Map map, Rectangle viewWindow, bool debug = false)
    {
        Map = map;
        ViewX = viewWindow.X;
        ViewY = viewWindow.Y;
        ViewWidth = viewWindow.Width;
        ViewHeight = viewWindow.Height;
        Debug = debug;

        Entities = SpawnEntities();
    }

    public static void Load(Map map, Rectangle viewWindow, bool debug = false)
    {
        Current = new World(map, viewWindow, debug);
        InitializeEntities(Current.Entities);
    }

    public void DrawBelowPlayer(SpriteBatch sb)
    {
        Map.Draw(sb, ViewWindow, Debug, 0, Map.PlayerLayerIndex);
    }

    public void DrawAbovePlayer(SpriteBatch sb)
    {
        Map.Draw(sb, ViewWindow, Debug, Map.PlayerLayerIndex + 1);
    }

    public void CenterViewOnPlayer(Player p)
    {
        ViewX = p.WorldPosition.X + (p.Width / 2) - (ViewWidth / 2);
        ViewY = p.WorldPosition.Y + (p.Height / 2) - (ViewHeight / 2);

        //constrain to map boundaries
        ViewX = Math.Max(Math.Min(ViewX, Map.WidthPx - ViewWidth), 0);
        ViewY = Math.Max(Math.Min(ViewY, Map.HeightPx - ViewHeight), 0);
    }

    //must adjust view window separately from player, as player may move independently of view
    public void ScrollViewWithinMapBounds(Player player, Vector2 scrollOffset)
    {
        if (scrollOffset.X < 0)
        {
            //scroll left
            float scrollDistX = MathHelper.Min(Math.Abs(scrollOffset.X), MathHelper.Distance(ViewX, 0));
            if (player.ScreenX + (player.Width / 2) < ViewWidth / 2)
                ViewX -= scrollDistX;
        }
        else if (scrollOffset.X > 0)
        {
            //scroll right
            float scrollDistX = MathHelper.Min(scrollOffset.X, MathHelper.Distance(ViewX + ViewWidth, WidthPx));
            if (player.ScreenX + (player.Width / 2) >= ViewWidth / 2)
                ViewX += scrollDistX;
        }

        if (scrollOffset.Y < 0)
        {
            //scroll up
            float scrollDistY = MathHelper.Min(Math.Abs(scrollOffset.Y), MathHelper.Distance(ViewY, 0));
            if (player.ScreenY + (player.Height / 2) < ViewHeight / 2)
                ViewY -= scrollDistY;
        }
        else if (scrollOffset.Y > 0)
        {
            //scroll down
            float scrollDistY = MathHelper.Min(scrollOffset.Y, MathHelper.Distance(ViewY + ViewHeight, HeightPx));
            if (player.ScreenY + (player.Height / 2) >= ViewHeight / 2)
                ViewY += scrollDistY;
        }
    }

    //maps a point from map pixel coordinates to screen coordinates by offsetting the current view
    public Vector2 WorldToScreenCoordinates(Vector2 worldCoords)
    {
        return worldCoords - ViewOffset;
    }

    //maps a point from screen coordinates to map pixel coordinates by offsetting the current view
    public Vector2 ScreenToWorldCoordinates(Vector2 screenCoords)
    {
        return screenCoords + ViewOffset;
    }

    public Rectangle ScreenToWorldRectangle(Rectangle rect)
    {
        rect.Offset(-(int)ViewX, -(int)ViewY);
        return rect;
    }

    //get all tile coordinates that the given pixel coordinate rectangle occupies
    public List<Point> GetOccupyingTiles(Rectangle rect)
    {
        int tileUpperLeftX = rect.Left / TileWidth;
        int tileUpperLeftY = rect.Top / TileHeight;
        int tileBottomRightX = (rect.Right - 1) / TileWidth;
        int tileBottomRightY = (rect.Bottom - 1) / TileHeight;
        List<Point> occupiedTiles = new List<Point>();

        for (int x = tileUpperLeftX; x <= tileBottomRightX; x++)
        {
            for (int y = tileUpperLeftY; y <= tileBottomRightY; y++)
            {
                occupiedTiles.Add(new Point(x, y));
            }
        }

        return occupiedTiles;
    }

    //returns true if there is a solid WorldEntity entirely covering the given tile coords
    public bool EntityCollision(Point tileCoords)
    {
        Rectangle worldTileRect = new Rectangle(tileCoords.X * TileWidth, tileCoords.Y * TileHeight, TileWidth, TileHeight);
        foreach (WorldEntity wEnt in Entities.OfType<WorldEntity>().Where(w => w.Solid))
        {
            if (wEnt.WorldRect.Contains(worldTileRect))
                return true;
        }

        return false;
    }

    //first pass: instantiate all entities by type
    private List<Entity> SpawnEntities()
    {
        List<Entity> entities = new List<Entity>();

        foreach (ObjectGroup objGroup in Map.ObjectGroups)
        {
            foreach (Object obj in objGroup.Objects)
            {
                Type t = Entity.GetEntityType(obj.Type);
                //if (t == null) throw new Exception("No Entity class found for Object type \"" + obj.Type + "\".");
                if (t == null) continue;
                entities.Add((Entity)Activator.CreateInstance(t, obj));
            }
        }

        return entities;
    }

    //second pass: wire up properties for specific entity types
    private static void InitializeEntities(List<Entity> entities)
    {
        foreach (Entity e in entities)
        {
            e.Initialize();
        }
    }
}