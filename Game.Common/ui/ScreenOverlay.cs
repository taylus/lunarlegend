using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class ScreenOverlay : UIElement
{
    public Color Color { get; set; }
    public float Opacity { get; set; }

    public ScreenOverlay(Color color, float opacity)
    {
        Color = color;
        Opacity = opacity;
    }

    public override void Draw(SpriteBatch sb)
    {
        Color color = Color.Lerp(Color.Transparent, Color, MathHelper.Clamp(Opacity, 0, 1.0f));
        Util.DrawRectangle(sb, sb.GraphicsDevice.Viewport.Bounds, color);
    }
}

//a screen flash is just an overlay with an opacity that changes over time
public class ScreenFlash : ScreenOverlay
{
    private float opacityStep;
    private TimeSpan updateInterval;
    private TimeSpan untilNextUpdate;

    public ScreenFlash(Color color, float opacityStart, float opacityStep, TimeSpan interval) : base(color, opacityStart)
    {
        this.opacityStep = opacityStep;
        updateInterval = interval;
    }

    public override void Update(GameTime currentGameTime)
    {
        if (Opacity <= 0) return;
        untilNextUpdate -= currentGameTime.ElapsedGameTime;
        if (untilNextUpdate.TotalMilliseconds <= 0)
        {
            Opacity -= opacityStep;
            untilNextUpdate = updateInterval;
        }
    }
}