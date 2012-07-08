using System;
using System.Linq;
using System.Collections.Generic;
using Property = System.Collections.Generic.KeyValuePair<string, string>;
using Microsoft.Xna.Framework;

public class ObjectGroup
{
    public string Name { get; protected set; }
    public Color Color { get; protected set; }
    public float Opacity { get; protected set; }
    public bool Visible { get; protected set; }
    public List<Object> Objects { get; protected set; }

    public ObjectGroup(Tiled.objectgroup objGroup)
    {
        Name = objGroup.name;
        //TODO: color
        //TODO: opacity
        //TODO: visible

        Objects = (from Tiled.@object obj in objGroup.@object select new Object(obj)).ToList();
    }

    //TODO: should probably store objects in a Dictionary of name => object,
    //but there's nothing stopping you from duplicating names in Tiled
}

public class Object
{
    public string Name { get; protected set; }
    public string Type { get; protected set; }
    public int X { get; protected set; }
    public int Y { get; protected set; }
    public Vector2 Position { get { return new Vector2((int)X, (int)Y); } }
    public int Width { get; protected set; }
    public int Height { get; protected set; }
    public uint GID { get; protected set; }
    public bool Visible { get; protected set; }
    public List<Property> Properties { get; protected set; }
    public Rectangle Rectangle { get { return new Rectangle(X, Y, Width, Height); } }

    public static readonly Color DEFAULT_COLOR = Color.Lerp(Color.Transparent, Color.LimeGreen, 0.35f);

    public Object(Tiled.@object obj)
    {
        Name = obj.name;
        Type = obj.type;
        X = int.Parse(obj.x);
        Y = int.Parse(obj.y);
        Width = obj.width != null? int.Parse(obj.width) : 0;
        Height = obj.height != null? int.Parse(obj.height) : 0;
        //TODO: GID
        //TODO: visible

        Properties = Map.LoadProperties(obj.properties);
    }
}
