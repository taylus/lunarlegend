using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class EffectsManager
{
    //public List<Sprite> Sprites { get; private set; }
    //public ScreenFlash ScreenFlash { get; private set; }
    public static List<UIElement> Effects { get; private set; }

    static EffectsManager()
    {
        Effects = new List<UIElement>();
    }

    public static void Register(UIElement e)
    {
        Effects.Add(e);
    }

    public static void Unregister(UIElement e)
    {
        Effects.Remove(e);
    }

    public static void ScreenFlash(Color color, float startOpacity = 0.75f, float opacityStep = 0.025f, TimeSpan stepInterval = default(TimeSpan))
    {
        if(stepInterval == default(TimeSpan))
            stepInterval = TimeSpan.FromMilliseconds(25);

        Register(new ScreenFlash(color, startOpacity, opacityStep, stepInterval));
    }

    public static void ScreenShake(uint intensity, TimeSpan duration)
    {
        throw new NotImplementedException();
    }

    public static void PutSprite(Sprite spr, Point position)
    {
        spr.CenterOn(position);
        Register(spr);
    }

    public static void PutSprite(AnimatedSprite spr, Point position)
    {
        spr.CenterOn(position);
        spr.Animation = spr.CreateDefaultAnimation(TimeSpan.FromMilliseconds(30), false);
        Register(spr);
    }

    public static void Draw(SpriteBatch sb)
    {
        foreach (UIElement e in Effects)
        {
            e.Draw(sb);
        }
    }

    public static void Update(GameTime currentGameTime)
    {
        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            UIElement e = Effects[i];
            e.Update(currentGameTime);

            //remove any screen flashes that are no longer visible
            if (e.GetType() == typeof(ScreenFlash) && ((ScreenFlash)e).Opacity <= 0)
            {
                Unregister(e);
            }

            //remove any animated sprites that have finished their animation
            if (e.GetType() == typeof(AnimatedSprite))
            {
                AnimatedSprite spr = (AnimatedSprite)e;
                if (spr.Animation != null && spr.Animation.IsOver())
                {
                    Unregister(e);
                }
            }
        }
    }
}