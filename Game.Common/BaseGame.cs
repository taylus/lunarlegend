using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// This class serves as a layer on top of XNA's Game class, providing some common, useful features.
/// This prevents copy-paste and allows specific game classes to focus only one what makes them different.
/// </summary>
public class BaseGame : Game
{
    //these are static to allow for static methods to load resources/etc
    protected static GraphicsDevice graphicsDevice;
    protected static ContentManager contentManager;
    protected static SpriteBatch spriteBatch;

    public static Rectangle GameWindow { get { return graphicsDevice.Viewport.Bounds; } }
    public static int GameWidth { get { return GameWindow.Width; } }
    public static int GameHeight { get { return GameWindow.Height; } }

    protected GraphicsDeviceManager graphics;

    //compare previous state to current state to detect input that happened this frame
    protected KeyboardState prevKeyboard;
    protected KeyboardState curKeyboard;
    protected MouseState prevMouse;
    protected MouseState curMouse;

    //TODO: make some kind of font manager for different fonts, sizes, and settings
    public static SpriteFont Font { get; protected set; }

    public BaseGame()
    {
        IsMouseVisible = true;
        Window.Title = "Vidya Gaem";
        Content.RootDirectory = "Content";

        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 800;
        graphics.PreferredBackBufferHeight = 600;
        graphics.ApplyChanges();
    }

    protected override void LoadContent()
    {
        contentManager = Content;
        graphicsDevice = GraphicsDevice;
        spriteBatch = new SpriteBatch(GraphicsDevice);
        Font = Content.Load<SpriteFont>("font");
    }

    public bool KeyPressedThisFrame(Keys key)
    {
        return !prevKeyboard.IsKeyDown(key) && curKeyboard.IsKeyDown(key);
    }

    public static Texture2D LoadTexture(string imgFile, bool external)
    {
        //note: loading a texture through the content pipeline will use premultiplied
        //alpha blending (by default), but loading one from a file stream will not
        //http://blogs.msdn.com/b/shawnhar/archive/2010/04/08/premultiplied-alpha-in-xna-game-studio-4-0.aspx
        //http://blogs.msdn.com/b/shawnhar/archive/2009/11/06/premultiplied-alpha.aspx

        if (!external) return contentManager.Load<Texture2D>(imgFile);

        //if the path is relative, then root it in the content project
        if (!Path.IsPathRooted(imgFile))
        {
            imgFile = Path.Combine(contentManager.RootDirectory, imgFile);
        }

        using (FileStream fstream = new FileStream(imgFile, FileMode.Open))
        {
            return Texture2D.FromStream(graphicsDevice, fstream);
        }
    }

    public static RenderTarget2D CreateRenderTarget(int w, int h)
    {
        return new RenderTarget2D(graphicsDevice, w, h);
    }

    public static SoundEffect LoadSoundEffect(string sfxFile, bool external)
    {
        if (!external) return contentManager.Load<SoundEffect>(sfxFile);

        using (FileStream fstream = new FileStream(sfxFile, FileMode.Open))
        {
            return SoundEffect.FromStream(fstream);
        }
    }

    public static Song LoadSong(string songFile, bool external, string songName)
    {
        if (!external) return contentManager.Load<Song>(songFile);
        return Song.FromUri(songName, new Uri(songFile));
    }
}
