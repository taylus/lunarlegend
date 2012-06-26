using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class TiledDemoGame : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private SpriteFont font;

    private Map map;

    public TiledDemoGame()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 1024;
        graphics.PreferredBackBufferHeight = 768;
        graphics.ApplyChanges();
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = "Demo Game";
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        font = Content.Load<SpriteFont>("font");
        //map = LoadMap("maps/desert/desert.tmx");
        //map = LoadMap("maps/gid_example/gids.tmx");
        map = LoadMap("maps/test/testmap.tmx");
        //map = LoadMap("maps/walls/walls_test.tmx");
    }

    protected override void UnloadContent()
    {
        //TODO: unload any non-ContentManager content here
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        //exit on esc
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            this.Exit();

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
        map.Draw(spriteBatch, true);
        spriteBatch.End();

        //reset to screen, and clear it
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.DimGray);

        //draw main content to screen
        spriteBatch.Begin();
        spriteBatch.Draw(mapSurf, Vector2.Zero, null, Color.White, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
        DrawDebugMapInfo();
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawDebugMapInfo()
    {
        int stringPadding = 2;
        List<string> debugStrings = new List<string>();
        debugStrings.Add("Map: " + map.Name);
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

    private Map LoadMap(string tmxFile)
    {
        return new Map(Path.Combine(Content.RootDirectory, tmxFile), GraphicsDevice);
    }
}
