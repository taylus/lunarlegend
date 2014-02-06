using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class EffectsManager
{
    public static List<UIElement> Effects { get; private set; }

    private static float screenShakeIntensity; //upper bound for calculating random vector used to offset the game's main SpriteBatch
    private static float screenShakeDecrease = 1; //how much to decrease the screen shake intensity by each update
    private static TimeSpan untilNextShakeDecrease = TimeSpan.MaxValue; //how long until the next screen shake update
    private static TimeSpan shakeDecreaseInterval = TimeSpan.MaxValue; //how long between screen shake updates

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

    public static void ScreenShake(float intensity, TimeSpan duration, float intensityDecrease = 1)
    {
        //TODO: fix timing
        //don't specify intensity decrease? calculate it based on duration and current FPS (?)

        //this approach decreases the screen shake intensity linearly
        //a somewhat more realistic-looking approach would slow the decrease interval as the intensity approaches zero
        screenShakeIntensity = intensity;
        screenShakeDecrease = intensityDecrease;
        untilNextShakeDecrease = shakeDecreaseInterval = TimeSpan.FromTicks(duration.Ticks / (int)(intensity * screenShakeDecrease));
    }

    public static Matrix TranslateShake()
    {
        if (screenShakeIntensity <= 0) return Matrix.Identity;
        Vector3 randomShake = Shake(screenShakeIntensity);
        return Matrix.CreateTranslation(randomShake);
    }

    private static Vector3 Shake(float intensity)
    {
        float x = (Util.Random() * intensity) - (intensity / 2);
        float y = (Util.Random() * intensity) - (intensity / 2);
        return new Vector3(x, y, 0);
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
        //decrease screen shake intensity every time shakeDecreaseInterval elapses
        untilNextShakeDecrease -= currentGameTime.ElapsedGameTime;
        if (untilNextShakeDecrease.TotalMilliseconds <= 0)
        {
            screenShakeIntensity -= screenShakeDecrease;
            if (screenShakeIntensity <= 0) screenShakeIntensity = 0;
            untilNextShakeDecrease = shakeDecreaseInterval;
        }

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