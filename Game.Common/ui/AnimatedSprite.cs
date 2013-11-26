using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//an animated sprite has a sprite sheet for its image, and keeps track of the source rect "frame" it's drawing from
//an animation is just a series of 2D points in the sprite sheet indicating where and how quickly to move this rect
public class AnimatedSprite : Sprite
{
    private Point curFrameCoords;
    private Rectangle sourceRect
    {
        get
        {
            return new Rectangle(curFrameCoords.X * Width, curFrameCoords.Y * Height, Width, Height);
        }
    }
    public Animation Animation { get; set; }

    public AnimatedSprite(string imgFile, int frameWidth, int frameHeight, float scale = 1.0f)
        : this(imgFile, frameWidth, frameHeight, scale, Color.White)
    {

    }

    public AnimatedSprite(string imgFile, int frameWidth, int frameHeight, float scale, Color tint)
        : base(imgFile, scale, tint)
    {
        Width = frameWidth;
        Height = frameHeight;
    }

    protected AnimatedSprite(AnimatedSprite other) : base(other)
    {
        Animation = other.Animation.Clone();
    }

    public new AnimatedSprite Clone()
    {
        return new AnimatedSprite(this);
    }

    public override void Draw(SpriteBatch sb)
    {
        sb.Draw(Image, new Vector2(X + ScaledWidth / 2, Y + ScaledHeight / 2), sourceRect, Tint, Rotation, new Vector2(Width / 2, Height / 2), Scale, SpriteEffects.None, 0);
    }

    public override void Update(GameTime currentGameTime)
    {
        base.Update(currentGameTime);

        if (Animation == null || Animation.IsOver()) return;
        Animation.Update(currentGameTime);
        curFrameCoords = Animation.CurrentFrame;
    }

    //helper method that creates a default animation for this sprite sheet
    //that displays all frames from left to right, top to bottom, and loops
    public Animation CreateDefaultAnimation(TimeSpan frameLength)
    {
        List<Point> frames = new List<Point>();
        for (int y = 0; y < Image.Height; y += Height)
        {
            for (int x = 0; x < Image.Width; x += Width)
            {
                frames.Add(new Point(x / Width, y / Height));
            }
        }
        return new Animation(frames, frameLength);
    }
}

public class Animation
{
    private int currentFrameIndex;
    private TimeSpan untilNextFrame;
    public List<Point> Frames;
    public Point CurrentFrame { get { return Frames[currentFrameIndex]; } }
    public TimeSpan FrameLength;
    public bool Loop;

    public Animation(List<Point> frames, TimeSpan frameLength, bool loop = true)
    {
        Frames = frames;
        FrameLength = frameLength;
        untilNextFrame = frameLength;
        Loop = loop;
        currentFrameIndex = 0;
    }

    protected Animation(List<Point> frames, int curFrameIndex, TimeSpan frameLength, bool loop, TimeSpan untilNextFrame)
    {
        Frames = frames;
        currentFrameIndex = curFrameIndex;
        FrameLength = frameLength;
        Loop = loop;
        this.untilNextFrame = untilNextFrame;
    }

    public Animation Clone()
    {
        return new Animation(new List<Point>(Frames), currentFrameIndex, FrameLength, Loop, untilNextFrame);
    }

    public void Update(GameTime currentGameTime)
    {
        untilNextFrame -= currentGameTime.ElapsedGameTime;
        if (untilNextFrame.TotalMilliseconds <= 0)
        {
            Advance();
            untilNextFrame = FrameLength;
        }
    }

    public bool IsOver()
    {
        if (Loop) return false;
        return currentFrameIndex >= Frames.Count;
    }

    public void Advance()
    {
        if (currentFrameIndex < Frames.Count)
            currentFrameIndex++;
        if (Loop && currentFrameIndex >= Frames.Count)
            currentFrameIndex = 0;
    }
}