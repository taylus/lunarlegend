using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class TiledDemoGame : Game
{
    //these are static to allow for static methods to load resources/etc
    private static GraphicsDevice graphicsDevice;
    private static ContentManager contentManager;
    private static SpriteBatch spriteBatch;

    //these are static because it only make sense to have one
    private static Player player;

    private GraphicsDeviceManager graphics;
    private KeyboardState prevKeyboard;
    private KeyboardState curKeyboard;
    private MouseState prevMouse;
    private MouseState curMouse;

    //render the world and player to a temp surface for scaling
    private Texture2D gameSurf;
    private static float gameScale;
    private const float DEFAULT_GAME_SCALE = 1.0f;
    private const string GAME_TITLE = "Demo Game";

    public const int MSGBOX_WIDTH = 300;
    public const int MSGBOX_HEIGHT = 80;
    public static int GameWidth { get { return graphicsDevice.Viewport.Width; } }
    public static int GameHeight { get { return graphicsDevice.Viewport.Height; } }
    public static Rectangle GameWindow { get { return graphicsDevice.Viewport.Bounds; } }
    public static SpriteFont Font { get; protected set; }  //TODO: make some kind of font manager for different fonts, sizes, and settings

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
        contentManager = Content;
        graphicsDevice = GraphicsDevice;
        spriteBatch = new SpriteBatch(GraphicsDevice);
        gameSurf = new RenderTarget2D(GraphicsDevice, GameWidth, GameHeight);
        Font = Content.Load<SpriteFont>("font");

        //LoadWorld("maps/test/testmap.tmx");
        //LoadWorld("maps/test_scroll/test_scroll.tmx");
        //LoadWorld("maps/layer_test/layers.tmx");
        //LoadWorld("maps/pond/pond.tmx");
        LoadWorld("maps/doors/doors.tmx");

        //DEBUG: space out messageboxes so we can draw them all at once
        //for (int i = 0; i < activeMessageBoxes.Count; i++)
        //{
        //    activeMessageBoxes[i].X += (i * (activeMessageBoxes.TemplateMessageBox.Width + activeMessageBoxes.TemplateMessageBox.Padding));
        //}
    }

    public static void LoadWorld(string tmxMapFile)
    {
        bool preserveDebug = World.Current == null? false : World.Current.Debug;
        Map map = new Map(Path.Combine(contentManager.RootDirectory, tmxMapFile), graphicsDevice, Font);
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

        Rectangle scaledViewWindow = graphicsDevice.Viewport.Bounds.Scale(1 / gameScale);
        World.Load(map, scaledViewWindow, false);
        player = new Player(GetPlayerSpawnPosition(), (int)World.Current.TileWidth, (int)World.Current.TileHeight);
        //player.Speed /= gameScale;
        World.Current.CenterViewOnPlayer(player);
        World.Current.Debug = preserveDebug;
    }

    private static Vector2 GetPlayerSpawnPosition()
    {
        PlayerSpawn spawnPoint = World.Current.Entities.OfType<PlayerSpawn>().FirstOrDefault();
        if (spawnPoint != null) return spawnPoint.WorldPosition;

        //default to map's center
        //TODO: log a warning about the missing spawnpoint
        return new Vector2(World.Current.WidthPx / 2, World.Current.HeightPx / 2);
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game window isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        if (curKeyboard.IsKeyDown(Buttons.QUIT))
            this.Exit();

        //toggle debug mode
        if (!prevKeyboard.IsKeyDown(Buttons.DEBUG) && curKeyboard.IsKeyDown(Buttons.DEBUG))
        {
            World.Current.Debug = !World.Current.Debug;
            Window.Title = !World.Current.Debug ? GAME_TITLE : string.Format("{0} - FPS: {1}", GAME_TITLE, Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds));
        }

        //debug mode wall editor
        if (World.Current.Debug)
        {
            Point mouseTileCoords = World.Current.Map.GetTileAt(World.Current.ScreenToWorldCoordinates(curMouse.Position() / gameScale));

            if (curMouse.LeftButton == ButtonState.Pressed && !World.Current.CollisionLayer.ContainsTileAt(mouseTileCoords))
            {
                World.Current.CollisionLayer.Tiles[mouseTileCoords.X, mouseTileCoords.Y] = World.Current.Map.GetWallTile();
            }
            else if (curMouse.RightButton == ButtonState.Pressed && World.Current.CollisionLayer.ContainsTileAt(mouseTileCoords))
            {
                World.Current.CollisionLayer.Tiles[mouseTileCoords.X, mouseTileCoords.Y] = new Tile();
            }
        }

        //confirm/use button
        if (!prevKeyboard.IsKeyDown(Buttons.USE) && curKeyboard.IsKeyDown(Buttons.USE))
        {
            if (player.ActiveMessageBoxes != null)
            {
                //activeMessageBoxes.Advance();
                player.ActiveMessageBoxes = null;
            }
            else
            {
                //TODO: spatially index entities so we only check those nearby
                foreach (WorldEntity wEnt in World.Current.Entities.OfType<WorldEntity>())
                {
                    if (wEnt.InteractRect.Intersects(player.WorldRect))
                    {
                        wEnt.Use(player);
                    }
                }
            }
        }

        //player movement and entity activation
        if (player.ActiveMessageBoxes == null)
        {
            player.Move(curKeyboard);
            player.TouchEntities();
        }

        //reactivate any inactive buttons that the player is no longer standing on
        foreach(Button btn in World.Current.Entities.OfType<Button>().Where(b => !b.Active))
        {
            if(!btn.WorldRect.Intersects(player.WorldRect)) 
                btn.Active = true;
        }

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
        if (World.Current.Debug) DrawDebugInfo();
        if (player.ActiveMessageBoxes != null)
        {
            foreach (MessageBox msgBox in player.ActiveMessageBoxes)
            {
                msgBox.Draw(spriteBatch);
            }
        }
        spriteBatch.End();
        base.Draw(gameTime);
    }

    //render the world and all game objects to the temporary surface for scaling
    private void RenderTempSurface()
    {
        //draw the game to the temp surface at normal scale
        GraphicsDevice.SetRenderTarget((RenderTarget2D)gameSurf);
        GraphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin();
        World.Current.DrawBelowPlayer(spriteBatch);
        foreach (WorldEntity wEnt in World.Current.Entities.OfType<WorldEntity>())
        {
            wEnt.Draw(spriteBatch);
        }
        player.Draw(spriteBatch);
        World.Current.DrawAbovePlayer(spriteBatch);
        spriteBatch.End();

        //reset drawing to the screen
        GraphicsDevice.SetRenderTarget(null);
    }

    private void DrawDebugInfo()
    {
        int stringPadding = 2;
        List<string> debugStrings = new List<string>();
        debugStrings.Add(string.Format("View: ({0}, {1})", World.Current.ViewWindow.X, World.Current.ViewWindow.Y));
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
            spriteBatch.DrawString(Font, debugStrings[i], new Vector2(stringPadding, Font.LineSpacing * i), Color.White);
        }
    }

    public static Texture2D LoadTexture(string imgFile, bool external)
    {
        if (!external) return contentManager.Load<Texture2D>(imgFile);

        using (FileStream fstream = new FileStream(imgFile, FileMode.Open))
        {
            return Texture2D.FromStream(graphicsDevice, fstream);
        }
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
