using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//an animated sprite has a sprite sheet for its image, and keeps track of the source rect "frame" it's drawing from
//an animation is just a series of 2D points in the sprite sheet indicating where and how quickly to move this rect
//animated sprites have a good deal of metadata that will need to be persisted (either in files, or SQLite)
//e.g. file name, frame dimensions, list of animations (named lists of 2D points), etc
public class AnimatedSprite : Sprite
{
    private Point curFrameCoords;
    private Rectangle sourceRect
    {
        get
        {
            int x = SpriteSheetOrigin.X + (curFrameCoords.X * Width) + (curFrameCoords.X * FrameMarginWidth);
            int y = SpriteSheetOrigin.Y + (curFrameCoords.Y * Height) + (curFrameCoords.Y * FrameMarginHeight);
            return new Rectangle(x, y, Width, Height);
        }
    }

    //optional current animation and named set of all animations in this sprite sheet
    public Animation Animation { get; set; }
    public List<Animation> Animations { get; private set; } 

    public Point SpriteSheetOrigin { get; set; }    //optional top left coords in the sprite sheet where the frames begin
    public int FrameMarginWidth { get; set; }       //optional horizontal distance between frames
    public int FrameMarginHeight { get; set; }      //optional vertical distance between frames

    public AnimatedSprite(string imgFile, int frameWidth, int frameHeight, float scale, Color tint)
        : base(imgFile, scale, tint)
    {
        Width = frameWidth;
        Height = frameHeight;
        Animations = new List<Animation>();
    }

    public AnimatedSprite(string imgFile, int frameWidth, int frameHeight, float scale = 1.0f)
        : this(imgFile, frameWidth, frameHeight, scale, Color.White)
    {

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
        sb.Draw(Image, new Vector2(X + (int)(ScaledWidth / 2), Y + (int)(ScaledHeight / 2)), sourceRect, Tint, Rotation, new Vector2(Width / 2, Height / 2), Scale, SpriteEffects.None, 0);
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
        return new Animation("default", frameLength, true, frames.ToArray());
    }

    public void SetAnimation(string name)
    {
        if(!Animations.Any(a => a.Name == name))
            throw new ArgumentException(string.Format("Unable to set sprite animation to '{0}', animation not found", name));        

        if (Animation == null || Animation.Name != name) 
        {
            //Console.WriteLine(name);
            Animation = Animations.Where(a => a.Name == name).First();
            //Animation.Reset();
        }
    }
}

public class Animation
{
    private int currentFrameIndex;
    private TimeSpan untilNextFrame;
    public List<Point> Frames { get; set; }
    public Point CurrentFrame { get { return Frames[currentFrameIndex]; } }
    public TimeSpan FrameLength { get; set; }
    public bool Loop { get; set; }
    public string Name { get; set; }

    public Animation(string name, TimeSpan frameLength, bool loop = true, params Point[] frames)
    {
        Name = name;
        Frames = frames.ToList();
        FrameLength = frameLength;
        untilNextFrame = frameLength;
        Loop = loop;
        currentFrameIndex = 0;
    }

    protected Animation(string name, int curFrameIndex, TimeSpan frameLength, bool loop, TimeSpan untilNextFrame, params Point[] frames)
        : this(name, frameLength, loop, frames)
    {
        //so cloned sprites start at the same point in their animation cycle
        currentFrameIndex = curFrameIndex;
        this.untilNextFrame = untilNextFrame;
    }

    public Animation Clone()
    {
        return new Animation(Name, currentFrameIndex, FrameLength, Loop, untilNextFrame, new List<Point>(Frames).ToArray());
    }

    public void Update(GameTime currentGameTime)
    {
        untilNextFrame -= currentGameTime.ElapsedGameTime;
        if (untilNextFrame.TotalMilliseconds <= 0 && HasNextFrame())
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

    private bool HasNextFrame()
    {
        if (Loop) return true;
        return currentFrameIndex + 1 < Frames.Count;
    }

    public void Advance()
    {
        if (currentFrameIndex < Frames.Count)
            currentFrameIndex++;
        if (Loop && currentFrameIndex >= Frames.Count)
            currentFrameIndex = 0;
    }

    public void Reset()
    {
        currentFrameIndex = 0;
        untilNextFrame = FrameLength;
    }
}