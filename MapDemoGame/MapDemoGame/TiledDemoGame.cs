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
    private KeyboardState prevKeyboard;
    private KeyboardState curKeyboard;
    private MouseState prevMouse;
    private MouseState curMouse;

    private World world;
    private Player player;

    //render everything to a temp surface for scaling
    private const float GAME_SCALE = 1.0f;
    private Texture2D gameSurf;

    private const string GAME_TITLE = "Demo Game";

    public TiledDemoGame()
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 800;
        graphics.PreferredBackBufferHeight = 600;
        graphics.ApplyChanges();
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = GAME_TITLE;
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        gameSurf = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        font = Content.Load<SpriteFont>("font");

        //Map map = LoadMap("maps/walls/walls_test.tmx");
        Map map = LoadMap("maps/test_scroll/test_scroll.tmx");
        Rectangle scaledViewWindow = GraphicsDevice.Viewport.Bounds.Scale(1 / GAME_SCALE);
        world = new World(map, scaledViewWindow, false);

        player = new Player(world, GetPlayerSpawnPosition(), (int)world.TileWidth, (int)world.TileHeight);
        world.CenterViewOnPlayer(player);
    }

    private Vector2 GetPlayerSpawnPosition()
    {
        Entity spawnPoint = world.Entities.GetByType("info_player_start");
        if (spawnPoint != null) return spawnPoint.Position;

        //default to map's center
        //TODO: log a warning about the missing spawnpoint
        return new Vector2(world.WidthPx / 2, world.HeightPx / 2);
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Keys.Escape))
            this.Exit();

        if (!prevKeyboard.IsKeyDown(Keys.Space) && curKeyboard.IsKeyDown(Keys.Space))
        {
            world.Debug = !world.Debug;
        }

        player.Move(curKeyboard);
        if (world.Debug)
        {
            //world.Map.HighlightedTiles = world.Map.GetOccupyingTiles(player.WorldRect);
            Window.Title = string.Format("{0} - FPS: {1}", GAME_TITLE, Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds));
        }
        else
        {
            Window.Title = GAME_TITLE;
        }

        if (world.Debug)
        {
            Point mouseTileCoords = world.Map.GetTileAt(world.ScreenToWorldCoordinates(curMouse.Position() / GAME_SCALE));

            if (curMouse.LeftButton == ButtonState.Pressed && !world.CollisionLayer.ContainsTileAt(mouseTileCoords))
            {
                world.CollisionLayer.Tiles[mouseTileCoords.X, mouseTileCoords.Y] = world.Map.GetWallTile();
            }
            else if (curMouse.RightButton == ButtonState.Pressed && world.CollisionLayer.ContainsTileAt(mouseTileCoords))
            {
                world.CollisionLayer.Tiles[mouseTileCoords.X, mouseTileCoords.Y] = new Tile();
            }
        }

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        RenderGameToTempSurface();

        //clear the screen
        GraphicsDevice.Clear(Color.DimGray);

        //draw the scaled game surface and any additional overlays
        spriteBatch.Begin();
        spriteBatch.Draw(gameSurf, Vector2.Zero, null, Color.White, 0.0f, Vector2.Zero, GAME_SCALE, SpriteEffects.None, 0);
        if(world.Debug) DrawDebugInfo();
        spriteBatch.End();

        base.Draw(gameTime);
    }

    //render the world and all game objects to the temporary surface for scaling
    private void RenderGameToTempSurface()
    {
        //draw the game to the temp surface at normal scale
        GraphicsDevice.SetRenderTarget((RenderTarget2D)gameSurf);
        GraphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin();
        world.Draw(spriteBatch);
        player.Draw(spriteBatch);
        spriteBatch.End();

        //reset drawing to the screen
        GraphicsDevice.SetRenderTarget(null);
    }

    private void DrawDebugInfo()
    {
        int stringPadding = 2;
        List<string> debugStrings = new List<string>();
        debugStrings.Add(string.Format("View: ({0}, {1})", world.ViewWindow.X, world.ViewWindow.Y));
        debugStrings.Add(string.Format("World: ({0}, {1})", player.WorldRect.X, player.WorldRect.Y));
        debugStrings.Add(string.Format("Screen: ({0}, {1})", player.ScreenRect.X, player.ScreenRect.Y));

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
        return new Map(Path.Combine(Content.RootDirectory, tmxFile), GraphicsDevice, font, "wall layer");
    }
}
