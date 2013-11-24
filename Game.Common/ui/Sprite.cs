using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public delegate void SpriteUpdateCallback(Sprite s);

//a sprite is a wrapper class around Texture2D and offers various effects
public class Sprite : UIElement
{
    public Texture2D image;
    public new Rectangle Rectangle { get { return new Rectangle(X, Y, (int)ScaledWidth, (int)ScaledHeight); } }

    //the image is multiplied by this color when drawing
    //transparency can be achieved by changing this color's alpha
    public Color Tint { get; set; }
    public float Scale { get; set; }
    public float ScaledWidth { get { return Width * Scale; } }
    public float ScaledHeight { get { return Height * Scale; } }
    public Rectangle? DestinationRectangle { get; set; }
    public float Rotation { get; set; }

    //update callback interval; set to zero to update as fast as possible
    public TimeSpan UpdateInterval { get; set; }
    private TimeSpan untilNextUpdate;
    public SpriteUpdateCallback UpdateCallback { get; set; }

    //TODO: support keeping track of multiple update intervals for multicast delegates?
    //e.g. a sprite that rotates and blinks, but the rotate callback has a shorter interval

    //variables to keep track of things for the default update callbacks
    protected Color originalTint;
    protected Color blinkColor1;
    protected Color blinkColor2;
    protected Color pulseColor;
    protected float pulseLerp;
    protected float pulseLerpStep;
    protected float pulseLerpLimit;
    protected float spin;

    public Sprite(string imgFile, float scale, Color tint)
    {
        image = BaseGame.LoadTexture(imgFile, true);
        Scale = scale;
        Width = image.Width;
        Height = image.Height;
        Tint = tint;
    }

    public Sprite(string imgFile, float scale = 1.0f) : this(imgFile, scale, Color.White)
    {

    }

    protected Sprite(Sprite other)
    {
        image = other.image;
        Scale = other.Scale;
        Width = other.Width;
        Height = other.Height;
        Tint = other.Tint;
        UpdateCallback = other.UpdateCallback;
        UpdateInterval = other.UpdateInterval;
        X = other.X;
        Y = other.Y;
        spin = other.spin;
        originalTint = other.originalTint;
        blinkColor1 = other.blinkColor1;
        blinkColor2 = other.blinkColor2;
        pulseColor = other.pulseColor;
        pulseLerp = other.pulseLerp;
        pulseLerpLimit = other.pulseLerpLimit;
        pulseLerpStep = other.pulseLerpStep;
    }

    public Sprite Clone()
    {
        return new Sprite(this);
    }

    public override void Draw(SpriteBatch sb)
    {
        if (DestinationRectangle.HasValue)
        {
            //fit in the destination rectangle if there is one
            Rectangle offsetDestRect = DestinationRectangle.Value;
            offsetDestRect.Offset(offsetDestRect.Width / 2, offsetDestRect.Height / 2);
            sb.Draw(image, offsetDestRect, null, Tint, Rotation, new Vector2(image.Width / 2, image.Height / 2), SpriteEffects.None, 0);
        }
        else
        {
            //otherwise drawn at the provided scale
            sb.Draw(image, new Vector2(X + ScaledWidth / 2, Y + ScaledHeight / 2), null, Tint, Rotation, new Vector2(Width / 2, Height / 2), Scale, SpriteEffects.None, 0);
        }
    }

    public virtual void Update(GameTime currentGameTime)
    {
        if(UpdateCallback == null) return;

        untilNextUpdate -= currentGameTime.ElapsedGameTime;
        if (untilNextUpdate.TotalMilliseconds <= 0)
        {
            UpdateCallback(this);
            untilNextUpdate = UpdateInterval;
        }
    }

    public void InvertColors()
    {
        //scan every pixel in the image and invert it
        //kinda expensive to do this CPU-side, but meh
        Color[] pixels = new Color[image.Width * image.Height];
        image.GetData(pixels);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                int pixelArrayIndex = x + (y * image.Width);
                Color c = pixels[pixelArrayIndex];
                pixels[pixelArrayIndex] = new Color(255 - c.R, 255 - c.G, 255 - c.B, c.A);
            }
        }
        image.SetData(pixels);
    }

    public override void CenterOn(int x, int y)
    {
        MoveTo(x - (int)(ScaledWidth / 2), y - (int)(ScaledHeight / 2));
    }

    #region Blink Helper Methods

    public void SetBlink(Color color1, Color color2, TimeSpan interval)
    {
        originalTint = Tint;
        blinkColor1 = color1;
        blinkColor2 = color2;
        UpdateInterval = interval;
        UpdateCallback = BlinkCallback;
    }

    public void StopBlink()
    {
        if (UpdateCallback != BlinkCallback) return;
        UpdateCallback = null;
        Tint = originalTint;
    }

    private static void BlinkCallback(Sprite s)
    {
        s.Tint = (s.Tint == s.blinkColor1 ? s.blinkColor2 : s.blinkColor1);
    }

    #endregion

    #region Pulse Helper Methods

    public void SetPulse(Color color, float lerpStep, float lerpLimit, TimeSpan interval)
    {
        originalTint = Tint;
        pulseColor = color;
        pulseLerp = 0;
        pulseLerpStep = lerpStep;
        pulseLerpLimit = MathHelper.Clamp(lerpLimit, 0, 1);
        UpdateInterval = interval;
        UpdateCallback = PulseCallback;
    }

    public void StopPulse()
    {
        if (UpdateCallback != PulseCallback) return;
        UpdateCallback = null;
        Tint = originalTint;
    }

    private static void PulseCallback(Sprite s)
    {
        s.Tint = Color.Lerp(Color.White, s.pulseColor, s.pulseLerp);
        s.pulseLerp += s.pulseLerpStep;

        if (s.pulseLerp < 0 /*Math.Abs(s.pulseLerpStep)*/ || s.pulseLerp > s.pulseLerpLimit)
        {
            s.pulseLerpStep = -s.pulseLerpStep;
        }
    }

    #endregion

    #region Rotate Helper Methods

    public void SetRotation(float rads, TimeSpan? interval = null)
    {
        spin = rads;
        UpdateInterval = interval.HasValue? interval.Value : TimeSpan.Zero;
        UpdateCallback = RotateCallback;
    }

    public void StopRotation()
    {
        if (UpdateCallback != RotateCallback) return;
        UpdateCallback = null;
    }

    private static void RotateCallback(Sprite s)
    {
        s.Rotation += s.spin;
        if (s.Rotation <= 0 || s.Rotation >= MathHelper.TwoPi)
            s.Rotation %= MathHelper.TwoPi;
    }

    #endregion
}