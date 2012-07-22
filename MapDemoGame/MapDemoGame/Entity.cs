using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Property = System.Collections.Generic.KeyValuePair<string, string>;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//represents a basic entity that can be triggered by some means (player touch, worldspawn, etc)
//this is essentially how Tiled Objects are incorporated into the game world
//all entities must implement a constructor that only takes a Tiled Object due to the way they are dynamically instantiated on worldspawn
public abstract class Entity
{
    public Object Object { get; protected set; }
    public bool Active { get; set; }

    //ties Tiled Objects to their corresponding Entity classes
    //called during worldspawn to initialize entities of the proper type
    public static string GetEntityTypeName() { return null; }

    //called when/if this entity is touched by the player
    public virtual void Touch(Player p) {}

    //called when/if this entity is used by the player
    public virtual void Use(Player p) { }

    //called when/if this entity is triggered by something
    public virtual void Fire() {}

    //returns the inheriting entity type that represents the given Tiled Object type
    public static Type GetEntityType(string type)
    {
        return (from Type t in Assembly.GetExecutingAssembly().GetTypes() 
                where t != typeof(Entity) &&
                    typeof(Entity).IsAssignableFrom(t) && !t.IsAbstract &&
                    (string)t.GetMethod("GetEntityTypeName").Invoke(null, null) == type 
                select t).FirstOrDefault();
    }

    public Entity(Object obj)
    {
        Object = obj;
        Active = true;
    }
}

//describes anything that has a position in the world (and thus the screen)
public interface IWorldEntity
{
    int Width { get; set; }
    int Height { get; set; }

    float WorldX { get; set; }
    float WorldY { get; set; }
    Vector2 WorldPosition { get; }
    Rectangle WorldRect { get; }

    float ScreenX { get; }
    float ScreenY { get; }
    Vector2 ScreenPosition { get; }
    Rectangle ScreenRect { get; }
    bool IsOnScreen { get; }
}

//represents an entity that has a position in the world
public abstract class WorldEntity : Entity, IWorldEntity
{
    public int Width { get; set; }
    public int Height { get; set; }

    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public Vector2 WorldPosition { get { return new Vector2(WorldX, WorldY); } }
    public Rectangle WorldRect { get { return new Rectangle((int)Math.Round(WorldX), (int)Math.Round(WorldY), Width, Height); } }

    public float ScreenX { get { return ScreenPosition.X; } }
    public float ScreenY { get { return ScreenPosition.Y; } }
    public Vector2 ScreenPosition { get { return World.Current.WorldToScreenCoordinates(WorldPosition); } }
    public Rectangle ScreenRect { get { return new Rectangle((int)Math.Round(ScreenX), (int)Math.Round(ScreenY), Width, Height); } }
    public bool IsOnScreen { get { return TiledDemoGame.GameWindow.Contains((int)ScreenX, (int)ScreenY); } }

    public WorldEntity(Object obj) : base(obj) { }
}

//TODO: make me derive from WorldEntity
//add some distinction to WorldEntity to separate entities that can be drawn but are debug (solid colored rect)
//vs entities that can be drawn and have images (NPC, Player, etc)
public class PlayerSpawn : Entity
{
    new public static string GetEntityTypeName() { return "info_player_start"; }
    public Vector2 WorldPosition { get { return Object.Position; } }
    public PlayerSpawn(Object obj) : base(obj) { }
}

public class TeleportEntrance : Entity
{
    public TeleportDestination Destination { get; set; }

    new public static string GetEntityTypeName() { return "trigger_teleport"; }

    public override void Touch(Player p)
    {
        if (Destination != null)
        {
            p.TeleportTo(Destination.Object.Position);
        }
    }

    public TeleportEntrance(Object obj) : base(obj) { }
}

public class TeleportDestination : Entity
{
    new public static string GetEntityTypeName() { return "info_teleport_destination"; }
    public TeleportDestination(Object obj) : base(obj) { }
}

public class ChangeLevel : Entity
{
    new public static string GetEntityTypeName() { return "trigger_changelevel"; }
    public string LevelName 
    { 
        get 
        { 
            string levelName = Object.Properties.GetValue("level");
            if(levelName.EndsWith(".tmx")) return levelName;
            return levelName + ".tmx";
        } 
    }

    public ChangeLevel(Object obj) : base(obj) { }

    public override void Touch(Player p)
    {
        TiledDemoGame.LoadWorld(Path.Combine("maps", LevelName));
    }
}

public class NPC : WorldEntity
{
    new public static string GetEntityTypeName() { return "info_npc"; }
    public Texture2D Image { get; set; }
    public MessageBoxSeries MessageBoxes { get; set; }
    public Rectangle InteractRect { get { return new Rectangle((int)WorldX - (World.Current.TileWidth / 2), (int)WorldY - (World.Current.TileHeight / 2),
                                                               Width + World.Current.TileWidth, Height + World.Current.TileHeight); } }
    //TODO: spritesheets/animations
    //TODO: behavior enum (stationary, random movement, predefined path)
    //TODO: opacity

    public NPC(Object obj) : base(obj) { }

    public override void Use(Player p)
    {
        p.ActiveMessageBoxes = MessageBoxes;
    }

    public void Draw(SpriteBatch sb)
    {
        sb.Draw(Image, ScreenPosition.Round(), Color.White);
        if (World.Current.Debug)
        {
            Util.DrawRectangle(sb, World.Current.ScreenToWorldRectangle(InteractRect), Color.Lerp(Color.LimeGreen, Color.Transparent, 0.55f));
        }
    }
}

public static class EntityExtensions
{
    public static Entity GetByName(this List<Entity> entities, string name)
    {
        return (from Entity e in entities where e.Object.Name == name select e).FirstOrDefault();
    }

    public static Entity GetByType(this List<Entity> entities, string type)
    {
        return (from Entity e in entities where e.GetType() == Entity.GetEntityType(type) select e).FirstOrDefault();
    }
}