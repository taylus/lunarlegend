using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class EffectsManager
{
    public static List<UIElement> Effects { get; private set; }

    private static float shakeIntensity;
    private static Vector3 shakeVector;
    private static TimeSpan shakeDuration;
    private static TimeSpan shakeInterval;
    private static TimeSpan untilNextShake;

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

    //shake with the given intensity for the given duration, calculating a new random shake vector every interval
    public static void ScreenShake(float intensity, TimeSpan duration, TimeSpan interval)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeInterval = interval;
    }

    //generate a random vector of the given magnitude
    private static Vector3 RandomShake(float intensity)
    {
        //set x or y to zero to only shake on one axis
        //use a wider range to weigh one axis more than the other
        float x = Util.RandomRange(-1.0f, 1.0f);
        float y = Util.RandomRange(-1.0f, 1.0f);

        //if both signs of the new shake vector are the same as the last, flip one or both of them at random
        //makes it so successive shake vectors are never in the same quadrant, making the shake look better
        if (Math.Sign(x) == Math.Sign(shakeVector.X) && Math.Sign(y) == Math.Sign(shakeVector.Y))
        {
            switch (Util.RandomRange(0, 3))
            {
                case 0:
                    x = -x;
                    break;
                case 1:
                    y = -y;
                    break;
                default:
                    x = -x;
                    y = -y;
                    break;
            }
        }

        Vector3 v = new Vector3(x, y, 0);
        v.Normalize();
        return v * intensity;
    }  

    public static Matrix TranslateShake()
    {
        if (shakeDuration.TotalMilliseconds > 0)
            return Matrix.CreateTranslation(shakeVector);
        else
            return Matrix.Identity;
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
        if (shakeDuration.TotalMilliseconds > 0)
        {
            shakeDuration -= currentGameTime.ElapsedGameTime;
            if (shakeDuration.TotalMilliseconds > 0)
            {
                untilNextShake -= currentGameTime.ElapsedGameTime;
                if (untilNextShake.TotalDays <= 0)
                {
                    shakeVector = RandomShake(shakeIntensity);
                    untilNextShake = shakeInterval;
                }
            }
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