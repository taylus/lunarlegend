﻿using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//a colored rectangle with a border
public class Box : UIElement
{
    //TODO: gradient backgrounds?
    public int BorderWidth { get; set; }
    public int Margin { get; set; }
    public int Padding { get; set; }
    public float Opacity { get; set; }
    public Color BorderColor { get; set; }
    public Color BackgroundColor { get; set; }

    private const int DEFAULT_MARGIN = 8;
    private const int DEFAULT_PADDING = 8;
    private const int DEFAULT_BORDER_WIDTH = 2;
    private static readonly Color DEFAULT_BORDER_COLOR = Color.LightSteelBlue;
    private const float DEFAULT_OPACITY = 0.65f;

    public Box(int x, int y, int w, int h)
    {
        Margin = DEFAULT_MARGIN;
        Padding = DEFAULT_PADDING;
        BorderWidth = DEFAULT_BORDER_WIDTH;
        BorderColor = DEFAULT_BORDER_COLOR;
        Opacity = DEFAULT_OPACITY;
        BackgroundColor = Color.Lerp(Color.Transparent, Color.DarkBlue, MathHelper.Clamp(Opacity, 0, 1));

        X = x;
        Y = y;
        Width = w;
        Height = h;
    }

    public override void Draw(SpriteBatch sb)
    {
        if (!Visible) return;
        Util.DrawRectangle(sb, Rectangle, BackgroundColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X, Y), new Vector2(X + Width, Y), BorderColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X + Width, Y), new Vector2(X + Width, Y + Height), BorderColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X + Width, Y + Height), new Vector2(X, Y + Height), BorderColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X, Y + Height), new Vector2(X, Y), BorderColor);
    }
}