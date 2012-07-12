using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class MessageBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Rectangle Bounds { get { return new Rectangle(X, Y, Width, Height); } }

    public int Padding { get; set; }
    public Color Color { get; set; }
}

public class MessageBoxSeries
{
    public List<MessageBox> MessageBoxes { get; set; }
}