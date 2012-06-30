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

    private World world;
    private Player player;

    public TiledDemoGame()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 800;
        graphics.PreferredBackBufferHeight = 600;
        graphics.ApplyChanges();
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = "Demo Game";
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        font = Content.Load<SpriteFont>("font");

        //map = LoadMap("maps/walls/walls_test.tmx");
        Map map = LoadMap("maps/test_scroll/test_scroll.tmx");
        world = new World(map, 1.0f, Vector2.Zero, GraphicsDevice);

        //TODO: necessary to render every frame? (animated tiles?)
        world.RenderMap(spriteBatch);

        player = new Player(world, GetPlayerSpawnPosition(), (int)world.TileWidth, (int)world.TileHeight);
        world.CenterViewOnPlayer(player);
    }

    private Vector2 GetPlayerSpawnPosition()
    {
        Object spawnPoint = world.Map.GetObject("info_player_start");
        if (spawnPoint != null) return spawnPoint.Position * world.Scale;

        //log a warning: no player spawnpoint in the map!
        return GraphicsDevice.Viewport.Bounds.Center.ToVector2();
    }

    protected override void UnloadContent()
    {
        //TODO: unload any non-ContentManager content here
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        KeyboardState keyboard = Keyboard.GetState();

        //exit on esc
        if (keyboard.IsKeyDown(Keys.Escape))
            this.Exit();

        player.Move(keyboard);

        if (keyboard.IsKeyDown(Keys.Space) && System.Diagnostics.Debugger.IsAttached)
            System.Diagnostics.Debugger.Break();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        //clear the screen
        GraphicsDevice.Clear(Color.DimGray);

        //draw game objects to the screen
        spriteBatch.Begin();
        world.Draw(spriteBatch);
        player.Draw(spriteBatch);
        DrawDebugInfo();
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawDebugInfo()
    {
        int stringPadding = 2;
        List<string> debugStrings = new List<string>();
        debugStrings.Add(string.Format("View: ({0}, {1})", (int)world.ViewX, (int)world.ViewY));
        debugStrings.Add(string.Format("World: ({0}, {1})", (int)player.WorldX, (int)player.WorldY));
        debugStrings.Add(string.Format("Screen: ({0}, {1})", (int)player.ScreenX, (int)player.ScreenY));

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

    private Vector2 GetInitialViewOffset()
    {
        return GraphicsDevice.Viewport.Bounds.Center.ToVector2() - new Vector2(world.WidthPx / 2, world.HeightPx / 2);
    }
}
