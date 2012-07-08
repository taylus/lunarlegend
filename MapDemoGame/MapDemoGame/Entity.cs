using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

//represents a basic entity that can be triggered by some means (player touch, worldspawn, etc)
//this is essentially how Tiled Objects are incorporated into the game world
public abstract class Entity
{
    public Object Object { get; protected set; }
    public bool Active { get; set; }
    public Vector2 Position { get { return Object.Position; } }

    //ties Tiled Objects to their corresponding Entity classes
    //called during worldspawn to initialize entities of the proper type
    public static string GetEntityTypeName() { return null; }

    //called when/if this entity is touched by the player
    public virtual void Touch(Player p) {}

    //called when/if this entity is triggered by something
    public virtual void Fire() {}

    //returns the inheriting entity type that represents the given Tiled Object type
    public static Type GetEntityType(string type)
    {
        return (from Type t in Assembly.GetExecutingAssembly().GetTypes() 
                where t != typeof(Entity) &&
                    typeof(Entity).IsAssignableFrom(t) &&
                    (string)t.GetMethod("GetEntityTypeName").Invoke(null, null) == type 
                select t).FirstOrDefault();
    }

    public Entity(Object obj)
    {
        Object = obj;
        Active = true;
    }
}

public class PlayerSpawn : Entity
{
    new public static string GetEntityTypeName() { return "info_player_start"; }
    public PlayerSpawn(Object obj) : base(obj) { }
}

public class TeleportEntrance : Entity
{
    public TeleportDestination Destination { get; set; }

    new public static string GetEntityTypeName() { return "info_teleport"; }

    public override void Touch(Player p)
    {
        if (Destination != null)
        {
            p.TeleportTo(Destination.Object.Position);
        }
    }

    public TeleportEntrance(Object obj) : base(obj) {}
}

public class TeleportDestination : Entity
{
    new public static string GetEntityTypeName() { return "info_teleport_destination"; }
    public TeleportDestination(Object obj) : base(obj) { }
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