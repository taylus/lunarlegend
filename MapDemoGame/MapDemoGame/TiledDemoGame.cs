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
    private MessageBox msgBox;

    //render the world and player to a temp surface for scaling
    private Texture2D gameSurf;
    private static float gameScale;
    private const float DEFAULT_GAME_SCALE = 1.0f;

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

        //LoadWorld("maps/walls/walls_test.tmx");
        LoadWorld("maps/test_scroll/test_scroll.tmx");
        //LoadWorld("maps/layer_test/layers.tmx");

        //msgBox = new MessageBox(100, 100, 200, 50, font, "this is a very long string of text that will not fit on just one line...\n\n\nin fact, it might take three or even four");
        msgBox = new MessageBox(100, 100, 200, 50, font, "line one\nline two\nline three\nline four");
    }

    private void LoadWorld(string tmxMapFile)
    {
        Map map = new Map(Path.Combine(Content.RootDirectory, tmxMapFile), GraphicsDevice, font);
        string mapScaleProperty = map.Properties.GetValue("scale");
        if (!string.IsNullOrWhiteSpace(mapScaleProperty))
        {
            gameScale = float.Parse(mapScaleProperty);
            if (gameScale == 0) gameScale = DEFAULT_GAME_SCALE;
        }
        else
        {
            gameScale = DEFAULT_GAME_SCALE;
        }

        Rectangle scaledViewWindow = GraphicsDevice.Viewport.Bounds.Scale(1 / gameScale);
        world = new World(map, scaledViewWindow, false);
        player = new Player(world, GetPlayerSpawnPosition(), (int)world.TileWidth, (int)world.TileHeight);
        //player.Speed /= gameScale;
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
        //don't respond to input if the game window isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Keys.Escape))
            this.Exit();

        //toggle debug mode
        if (!prevKeyboard.IsKeyDown(Keys.Space) && curKeyboard.IsKeyDown(Keys.Space))
        {
            world.Debug = !world.Debug;
            Window.Title = !world.Debug? GAME_TITLE : string.Format("{0} - FPS: {1}", GAME_TITLE, Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds));
        }

        //debug mode wall editor
        if (world.Debug)
        {
            Point mouseTileCoords = world.Map.GetTileAt(world.ScreenToWorldCoordinates(curMouse.Position() / gameScale));

            if (curMouse.LeftButton == ButtonState.Pressed && !world.CollisionLayer.ContainsTileAt(mouseTileCoords))
            {
                world.CollisionLayer.Tiles[mouseTileCoords.X, mouseTileCoords.Y] = world.Map.GetWallTile();
            }
            else if (curMouse.RightButton == ButtonState.Pressed && world.CollisionLayer.ContainsTileAt(mouseTileCoords))
            {
                world.CollisionLayer.Tiles[mouseTileCoords.X, mouseTileCoords.Y] = new Tile();
            }
        }

        //normal game logic
        player.Move(curKeyboard);
        TouchEntities();

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        RenderTempSurface();

        //clear the screen
        GraphicsDevice.Clear(Color.DimGray);

        //draw the scaled game surface, then any additional overlays at normal scale
        spriteBatch.Begin();
        spriteBatch.Draw(gameSurf, Vector2.Zero, null, Color.White, 0.0f, Vector2.Zero, gameScale, SpriteEffects.None, 0);
        msgBox.Draw(spriteBatch);
        if(world.Debug) DrawDebugInfo();
        spriteBatch.End();

        base.Draw(gameTime);
    }

    public void TouchEntities()
    {
        //TODO: spatially index the entities so we're not checking all of them
        foreach (Entity e in world.Entities)
        {
            if (!e.Active) continue;

            if (player.WorldRect.Intersects(e.Object.Rectangle))
            {
                //handle special entities where the game engine needs to do something special
                //TODO: this is a code smell... this function should really be in World, but it needs engine functionality to load new levels
                //      if we get a lot of situations like this, then expose engine functionality to entities somehow (service locator w/ engine callbacks?)
                if (e.GetType() == typeof(ChangeLevel))
                {
                    bool preserveDebug = world.Debug;
                    LoadWorld(Path.Combine("maps", ((ChangeLevel)e).LevelName));
                    world.Debug = preserveDebug;
                    break;
                }
                else
                {
                    e.Touch(player);
                }
            }
        }
    }

    //render the world and all game objects to the temporary surface for scaling
    private void RenderTempSurface()
    {
        //draw the game to the temp surface at normal scale
        GraphicsDevice.SetRenderTarget((RenderTarget2D)gameSurf);
        GraphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin();
        world.DrawBelowPlayer(spriteBatch);
        player.Draw(spriteBatch);
        world.DrawAbovePlayer(spriteBatch);
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
}
