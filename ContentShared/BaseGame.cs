﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
}