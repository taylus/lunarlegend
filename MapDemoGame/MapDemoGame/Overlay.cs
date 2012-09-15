using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public abstract class Overlay
{
    public Color Color { get; set; }
    public float Opacity { get; set; }

    public Overlay(Color color, float opacity)
    {
        Color = color;
        Opacity = opacity;
    }

    //TODO: move this into IDrawable? 
    //make any class that can draw itself implement that
    public abstract void Draw(SpriteBatch sb);
}

public class ScreenOverlay : Overlay
{
    public ScreenOverlay(Color color, float opacity) : base(color, opacity)
    {

    }

    public override void Draw(SpriteBatch sb)
    {
        Color color = Color.Lerp(Color.Transparent, Color, MathHelper.Clamp(Opacity, 0, 1.0f));
        Util.DrawRectangle(sb, WorldDemo.GameWindow, color);
    }
}

//TODO: inherit from IWorldEntity, make work with arbitrary images (and ignore their transparent pixels)
public class SpriteOverlay : Overlay
{
    //private Texture2D overlayImg;

    public SpriteOverlay(Texture2D sprite, Color color, float opacity) : base(color, opacity)
    {
        //TODO: scan every pixel in sprite, making overlayImg an alpha-only binary mask
    }

    public override void Draw(SpriteBatch sb)
    {
        throw new NotImplementedException();
    }
}
