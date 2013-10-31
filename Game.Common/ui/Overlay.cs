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
}

public class ScreenOverlay : Overlay
{
    public ScreenOverlay(Color color, float opacity) : base(color, opacity)
    {

    }

    public void Draw(SpriteBatch sb)
    {
        Color color = Color.Lerp(Color.Transparent, Color, MathHelper.Clamp(Opacity, 0, 1.0f));
        Util.DrawRectangle(sb, sb.GraphicsDevice.Viewport.Bounds, color);
    }
}

public class SpriteOverlay : Overlay
{
    protected Texture2D overlay;
    public int X { get; set; }
    public int Y { get; set; }

    public SpriteOverlay(Texture2D sprite, Color color, float opacity) : base(color, opacity)
    {
        X = sprite.Bounds.X;
        Y = sprite.Bounds.Y;
        overlay = new Texture2D(sprite.GraphicsDevice, sprite.Width, sprite.Height);

        //default to white so any other color can be used when drawing
        Color overlayColor = Color.Lerp(Color.Transparent, Color.White, MathHelper.Clamp(Opacity, 0, 1.0f));

        //scan every pixel in the image, creating a solid-color mask
        Color[] spritePixels = GetPixels(sprite);
        Color[] overlayPixels = new Color[spritePixels.Length];
        for (int y = 0; y < sprite.Height; y++)
        {
            for(int x = 0; x < sprite.Width; x++)
            {
                int pixelArrayIndex = x + (y * sprite.Width);
                Color c = spritePixels[pixelArrayIndex];
                if (c.A != 0)
                    overlayPixels[pixelArrayIndex] = overlayColor;
                else
                    overlayPixels[pixelArrayIndex] = Color.Transparent;
            }
        }
        overlay.SetData(overlayPixels);
    }

    public virtual void Draw(SpriteBatch sb, int x, int y, float scale)
    {
        sb.Draw(overlay, new Vector2(x, y), null, Color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    private Color[] GetPixels(Texture2D sprite)
    {
        Color[] c = new Color[sprite.Width * sprite.Height];
        sprite.GetData(c);
        return c;
    }
}

public class BlinkingSpriteOverlay : SpriteOverlay
{
    public TimeSpan BlinkInterval { get; set; }
    public bool BlinkEnabled { get; set; }
    private TimeSpan untilNextBlink;
    private Color currentColor;

    public BlinkingSpriteOverlay(Texture2D sprite, Color color, float opacity, TimeSpan interval) : base(sprite, color, opacity)
    {
        currentColor = Color.Transparent;
        BlinkInterval = interval;
        untilNextBlink = interval;
        BlinkEnabled = true;
    }

    public void Update(GameTime currentGameTime)
    {
        if (!BlinkEnabled) return;

        untilNextBlink -= currentGameTime.ElapsedGameTime;
        if (untilNextBlink.TotalMilliseconds <= 0)
        {
            Blink();
        }
    }

    public override void Draw(SpriteBatch sb, int x, int y, float scale)
    {
        if (BlinkEnabled)
        {
            sb.Draw(overlay, new Vector2(x, y), null, currentColor, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
        else
        {
            //if the blink is disabled, always display the non-transparent color (e.g. stay "off")
            sb.Draw(overlay, new Vector2(x, y), null, Color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }

    public void Reset()
    {
        untilNextBlink = BlinkInterval;
        currentColor = Color.Transparent;
    }

    private void Blink()
    {
        if (currentColor == Color.Transparent)
            currentColor = Color;
        else
            currentColor = Color.Transparent;

        untilNextBlink = BlinkInterval;
    }
}
