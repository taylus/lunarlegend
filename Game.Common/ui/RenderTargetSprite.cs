using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//a sprite that is intended to be drawn into by other drawables
//while a Sprite is a static image, and an AnimatedSprite is a sprite sheet, this is a blank slate
public class RenderTargetSprite : Sprite
{
    public RenderTarget2D RenderTarget { get { return (RenderTarget2D)Image; } }

    public RenderTargetSprite(int width, int height)
    {
        Image = BaseGame.CreateRenderTarget(width, height);
        Scale = 1.0f;
        Width = width;
        Height = height;
        Tint = Color.White;
    }

    public RenderTargetSprite(UIElement source) : this(source.Width, source.Height)
    {

    }
}