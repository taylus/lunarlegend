using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class ScreenOverlay
{
    public Color Color { get; set; }
    public float Opacity { get; set; }

    public ScreenOverlay(Color color, float opacity)
    {
        Color = color;
        Opacity = opacity;
    }

    public void Draw(SpriteBatch sb)
    {
        Color color = Color.Lerp(Color.Transparent, Color, MathHelper.Clamp(Opacity, 0, 1.0f));
        Util.DrawRectangle(sb, sb.GraphicsDevice.Viewport.Bounds, color);
    }
}