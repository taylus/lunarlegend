using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public delegate void SpriteUpdateCallback(Sprite s);

//a sprite is a wrapper class around Texture2D and offers various effects
public class Sprite : UIElement
{
    public Texture2D Image { get; set; }
    public new Rectangle Rectangle { get { return new Rectangle(X, Y, (int)ScaledWidth, (int)ScaledHeight); } }

    //the image is multiplied by this color when drawing
    //transparency can be achieved by changing this color's alpha
    public Color Tint { get; set; }
    public float Scale { get; set; }
    public float ScaledWidth { get { return Width * Scale; } }
    public float ScaledHeight { get { return Height * Scale; } }
    public Rectangle? DestinationRectangle { get; set; }
    public float Rotation { get; set; }
    public Point RotationCenterOffset { get; set; }

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
        Image = BaseGame.LoadTexture(imgFile, true);
        Scale = scale;
        Width = Image.Width;
        Height = Image.Height;
        Tint = tint;
    }

    public Sprite(string imgFile, float scale = 1.0f) : this(imgFile, scale, Color.White)
    {

    }

    public Sprite(string imgFile, int width, int height) : this(imgFile, 1.0f, Color.White)
    {
        DestinationRectangle = new Rectangle(0, 0, width, height);
        Width = width;
        Height = height;
    }

    protected Sprite(Sprite other)
    {
        Image = other.Image;
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

    protected Sprite()
    {

    }

    public Sprite Clone()
    {
        return new Sprite(this);
    }

    public override void Draw(SpriteBatch sb)
    {
        if (!Visible) return;

        if (DestinationRectangle.HasValue)
        {
            //draw inside the destination rectangle if there is one
            Rectangle offsetDestRect = DestinationRectangle.Value;
            offsetDestRect.Offset(offsetDestRect.Width / 2, offsetDestRect.Height / 2);
            sb.Draw(Image, offsetDestRect, null, Tint, Rotation, new Vector2(Image.Width / 2, Image.Height / 2), SpriteEffects.None, 0);
        }
        else
        {
            //otherwise, drawn centered and at the provided scale
            Vector2 origin = new Vector2((int)(Width / 2) + RotationCenterOffset.X, (int)(Height / 2) + RotationCenterOffset.Y);
            sb.Draw(Image, new Vector2(X + (int)(ScaledWidth / 2), (int)(Y + ScaledHeight / 2)), null, Tint, Rotation, origin, Scale, SpriteEffects.None, 0);
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
        //this is expensive to do CPU-side...
        Color[] pixels = new Color[Image.Width * Image.Height];
        Image.GetData(pixels);
        for (int y = 0; y < Image.Height; y++)
        {
            for (int x = 0; x < Image.Width; x++)
            {
                int pixelArrayIndex = x + (y * Image.Width);
                Color c = pixels[pixelArrayIndex];
                pixels[pixelArrayIndex] = new Color(255 - c.R, 255 - c.G, 255 - c.B, c.A);
            }
        }
        Image.SetData(pixels);
    }

    public override void MoveTo(int x, int y)
    {
        if (DestinationRectangle.HasValue)
        {
            Rectangle dr = DestinationRectangle.Value;
            DestinationRectangle = new Rectangle(x, y, dr.Width, dr.Height);
        }
        else
        {
            base.MoveTo(x, y);
        }
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
        untilNextUpdate = TimeSpan.Zero;
        //Tint = originalTint;
    }

    private static void BlinkCallback(Sprite s)
    {
        s.Tint = (s.Tint == s.blinkColor1 ? s.blinkColor2 : s.blinkColor1);
    }

    #endregion

    #region Pulse Helper Methods

    public void SetPulse(Color color, TimeSpan interval, float lerpStep = 0.05f, float lerpLimit = 0.6f)
    {
        originalTint = Tint;
        pulseColor = color;
        pulseLerp = 0;
        pulseLerpStep = lerpStep;   //how much to lerp every update
        pulseLerpLimit = MathHelper.Clamp(lerpLimit, 0, 1);     //max lerp weight towards the given color
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