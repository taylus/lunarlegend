using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class TiledMapViewer : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private SpriteFont font;
    private MouseState currentMouseState;
    private MouseState previousMouseState;

    private const float ZOOM_FACTOR = 1.1f;

    private Map map;
    private float mapScale;
    private Vector2 mapDrawPosition;

    public TiledMapViewer()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 1024;
        graphics.PreferredBackBufferHeight = 768;
        graphics.ApplyChanges();
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = "TMX Loader";
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
    }

    protected void Window_ClientSizeChanged(object sender, EventArgs e)
    {
        ResetMapViewSettings(false);
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        font = Content.Load<SpriteFont>("font");
        //map = LoadMap("maps/desert/desert.tmx");
        //map = LoadMap("maps/gid_example/gids.tmx");
        map = LoadMap("maps/test/testmap.tmx");
        //map = LoadMap("maps/walls/walls_test.tmx");

        ResetMapViewSettings();
    }

    protected override void UnloadContent()
    {
        //TODO: unload any non-ContentManager content here
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        currentMouseState = Mouse.GetState();

        //exit on esc
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            this.Exit();

        //left mouse button
        if (previousMouseState.LeftButton == ButtonState.Pressed && 
            currentMouseState.LeftButton == ButtonState.Pressed &&
            previousMouseState.Position() != currentMouseState.Position())
        {
            //dragging
            Vector2 delta = previousMouseState.Position() - currentMouseState.Position();
            mapDrawPosition -= delta;
        }

        //middle mouse button
        if (currentMouseState.ScrollWheelValue > previousMouseState.ScrollWheelValue)
        {
            //scrolled up, zoom in
            mapScale *= ZOOM_FACTOR;
        }
        else if(currentMouseState.ScrollWheelValue < previousMouseState.ScrollWheelValue)
        {
            //scrolled down, zoom out
            mapScale /= ZOOM_FACTOR;
        }
        if (currentMouseState.MiddleButton == ButtonState.Pressed)
        {
            //reset view settings on middle button click
            ResetMapViewSettings();
        }

        previousMouseState = currentMouseState;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        //draw the map to a temporary surface
        //apply effects to that surface, instead of having the map do it
        RenderTarget2D mapSurf = new RenderTarget2D(GraphicsDevice, map.WidthPx, map.HeightPx);
        GraphicsDevice.SetRenderTarget(mapSurf);
        GraphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin();
        map.Draw(spriteBatch);
        spriteBatch.End();

        //reset to screen, and clear it
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.DimGray);

        //draw main content to screen
        spriteBatch.Begin();
        spriteBatch.Draw(mapSurf, mapDrawPosition, null, Color.White, 0.0f, mapSurf.Bounds.Center.ToVector2(), mapScale, SpriteEffects.None, 0);
        DrawDebugMapInfo();
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawDebugMapInfo()
    {
        int stringPadding = 2;
        List<string> debugStrings = new List<string>();
        debugStrings.Add("Map: " + map.MapFileName);
        debugStrings.Add("Layers: " + map.Layers.Count);
        debugStrings.Add("Tilesets: " + map.TileSets.Count);
        debugStrings.Add("Properties: " + map.Properties.Count);

        //background rectangle
        //string longestString = debugStrings.OrderByDescending(s => s.Length).First();
        //int width = (int)font.MeasureString(longestString).X + (stringPadding * 4);
        //int height = font.LineSpacing * debugStrings.Count + stringPadding;
        //Rectangle bgRect = new Rectangle(0, 0, width, height);
        //Util.DrawRectangle(spriteBatch, bgRect, Color.DimGray);

        for (int i = 0; i < debugStrings.Count; i++)
        {
            spriteBatch.DrawString(font, debugStrings[i], new Vector2(stringPadding, font.LineSpacing * i), Color.White);
        }
    }

    private void ResetMapViewSettings(bool resetZoom = true)
    {
        mapDrawPosition = GraphicsDevice.Viewport.Bounds.Center.ToVector2();
        if (resetZoom) mapScale = 1.0f;
    }

    private Map LoadMap(string tmxFile)
    {
        return new Map(Path.Combine(Content.RootDirectory, tmxFile), GraphicsDevice, font);
    }
}
