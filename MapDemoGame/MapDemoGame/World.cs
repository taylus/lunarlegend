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

    public World(Map map, Rectangle viewWindow, bool debug = false)
    {
        Map = map;
        ViewX = viewWindow.X;
        ViewY = viewWindow.Y;
        ViewWidth = viewWindow.Width;
        ViewHeight = viewWindow.Height;
        Debug = debug;

        Entities = SpawnEntities();
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

    private List<Entity> SpawnEntities()
    {
        List<Entity> entities = new List<Entity>();

        //first pass: instantiate all entities by type
        foreach (ObjectGroup objGroup in Map.ObjectGroups)
        {
            foreach (Object obj in objGroup.Objects)
            {
                Type t = Entity.GetEntityType(obj.Type);
                if (t == null) throw new Exception("No Entity class found for Object type \"" + obj.Type + "\".");
                entities.Add((Entity)Activator.CreateInstance(t, obj));
            }
        }

        //second pass: wire up properties for specific entity types
        //TODO: move this into overriding functions?
        foreach (Entity e in entities)
        {
            if (e.GetType() == typeof(TeleportEntrance))
            {
                TeleportEntrance teleporter = (TeleportEntrance)e;

                string destEntityName = e.Object.Properties.GetValue("target");
                if (string.IsNullOrWhiteSpace(destEntityName))
                {
                    //TODO: warn that teleport entrance has no destination specified!
                    continue;
                }

                Entity destEntity = entities.GetByName(destEntityName);
                if (destEntity.GetType() != typeof(TeleportDestination))
                {
                    //TODO: warn that teleport destination is the wrong type of entity!
                    continue;
                }

                teleporter.Destination = (TeleportDestination)destEntity;
            }
        }

        return entities;
    }
}