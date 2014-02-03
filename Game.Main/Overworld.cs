using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

public class Overworld : BaseGame
{
    //these are static because it only make sense to have one
    private static Player player;
    private static MessageBox dialogue;
    private static CombatSystem combatSystem;
    private static Enemy engagedEnemy;

    //render the world and player to a temp surface for scaling
    private Texture2D gameSurf;
    private static float gameScale;
    private const float DEFAULT_GAME_SCALE = 1.0f;
    private const string GAME_TITLE = "Vidya Gaem";

    public Overworld()
    {
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = GAME_TITLE;
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        gameSurf = new RenderTarget2D(GraphicsDevice, GameWidth, GameHeight);

        combatSystem = new CombatSystem(CreateSamplePlayerParty());
        combatSystem.OnVictory = CombatVictory;
        combatSystem.OnDefeat = CombatDefeat;

        //LoadWorld("maps/test/testmap.tmx");
        LoadWorld("maps/test_scroll/test_scroll.tmx");
        //LoadWorld("maps/layer_test/layers.tmx");
        //LoadWorld("maps/pond/pond.tmx");
        //LoadWorld("maps/doors/doors.tmx");
        //LoadWorld("maps/castle/castle.tmx");
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

        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();

        //toggle debug mode
        if (KeyPressedThisFrame(Buttons.DEBUG))
        {
            World.Current.Debug = !World.Current.Debug;
            Window.Title = !World.Current.Debug ? GAME_TITLE : string.Format("{0} - FPS: {1}", GAME_TITLE, Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds));
        }

        //combat system input handling
        if (combatSystem.IsActive)
        {
            combatSystem.Update(gameTime);
            if (KeyPressedThisFrame(Buttons.CONFIRM))
                combatSystem.ConfirmKeyPressed();
            if (KeyPressedThisFrame(Buttons.CANCEL))
                combatSystem.CancelKeyPressed();
            if (KeyPressedThisFrame(Buttons.MOVE_LEFT))
                combatSystem.LeftKeyPressed();
            if (KeyPressedThisFrame(Buttons.MOVE_RIGHT))
                combatSystem.RightKeyPressed();
            if (KeyPressedThisFrame(Buttons.MOVE_UP))
                combatSystem.UpKeyPressed();
            if (KeyPressedThisFrame(Buttons.MOVE_DOWN))
                combatSystem.DownKeyPressed();
        }
        //overworld input handling
        else
        {
            //debug mode wall editor - add/remove walls via mouse click
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
            if (KeyPressedThisFrame(Buttons.CONFIRM))
            {
                if (dialogue != null)
                {
                    //scroll down in the current MessageBox if it has more lines to display
                    if (dialogue.HasMoreLinesToDisplay)
                    {
                        dialogue.AdvanceLines();
                    }
                    //if it doesn't, jump to the target of its selected choice
                    else if (dialogue.Choices.Count > 0)
                    {
                        dialogue = dialogue.SelectedChoice.Next;
                        dialogue.ResetLines();
                    }
                    //if it has no choices, go to its preferred next MessageBox
                    else if (dialogue.Next != null)
                    {
                        dialogue = dialogue.Next;
                        dialogue.ResetLines();
                    }
                    else
                    {
                        dialogue = null;
                    }
                }
                else
                {
                    //TODO: spatially index entities so we only check those nearby
                    foreach (WorldEntity wEnt in World.Current.Entities.OfType<WorldEntity>())
                    {
                        if (wEnt.InteractRect.Intersects(player.WorldRect))
                        {
                            wEnt.Use(player);
                            if (wEnt.GetType() == typeof(NPC))
                            {
                                NPC npc = (NPC)wEnt;
                                if (npc.Dialogue != null) dialogue = npc.Dialogue;
                            }
                        }
                    }
                }
            }

            if (dialogue != null)
            {
                if (KeyPressedThisFrame(Buttons.MOVE_LEFT) || KeyPressedThisFrame(Buttons.MOVE_UP))
                    dialogue.SelectPreviousChoice();
                if (KeyPressedThisFrame(Buttons.MOVE_RIGHT) || KeyPressedThisFrame(Buttons.MOVE_DOWN))
                    dialogue.SelectNextChoice();
            }

            //player movement and entity activation
            if (dialogue == null)
            {
                player.Move(curKeyboard);
                player.Update(gameTime);
            }

            //reactivate any inactive buttons that the player is no longer standing on
            foreach (Button btn in World.Current.Entities.OfType<Button>().Where(b => !b.Active))
            {
                if (!btn.WorldRect.Intersects(player.WorldRect))
                    btn.Active = true;
            }
        }

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        RenderTempWorldSurface();
        GraphicsDevice.Clear(Color.DimGray);
        spriteBatch.Begin();
        spriteBatch.Draw(gameSurf, Vector2.Zero, null, Color.White, 0.0f, Vector2.Zero, gameScale, SpriteEffects.None, 0);
        if (combatSystem.IsActive)
        {
            combatSystem.Draw(spriteBatch);
        }
        if (World.Current.Debug) DrawDebugInfo();
        if (dialogue != null)
        {
            dialogue.Draw(spriteBatch);
        }
        spriteBatch.End();
        base.Draw(gameTime);
    }

    //render the world and all game objects to the temporary surface for scaling
    private void RenderTempWorldSurface()
    {
        //draw the game to the temp surface at normal scale
        GraphicsDevice.SetRenderTarget((RenderTarget2D)gameSurf);
        GraphicsDevice.Clear(Color.Transparent);

        //SamplerState.PointClamp provides "nearest neighbor" scaling, as opposed to linear (which is blurry for small images)
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
        World.Current.DrawBelowPlayer(spriteBatch);
        if (!combatSystem.IsActive)
        {
            //don't draw entities in combat, only the world
            foreach (WorldEntity wEnt in World.Current.Entities.OfType<WorldEntity>())
            {
                wEnt.Draw(spriteBatch);
            }
            player.Draw(spriteBatch);
        }
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

    //create the MessageBox whose style, position, etc will be used by all MessageBoxes loaded for this game
    public static MessageBox CreateMessageBoxTemplate()
    {
        int w = 300;
        int h = 4;
        int x = (GameWidth / 2) - (w / 2);
        int y = 100;
        return new MessageBox(x, y, w, h, Font);
    }

    public static void BeginCombat(Enemy enemyEntity, List<EnemyCombatEntity> enemyParty)
    {
        engagedEnemy = enemyEntity;
        combatSystem.Engage(enemyParty, "battle/cave_bg.jpg");
        //combatSystem.Engage(Enemies.LoadPartyByID(3));
    }

    public void CombatVictory()
    {
        engagedEnemy.Delete();
    }

    public void CombatDefeat()
    {
        Environment.Exit(0);
    }

    private List<PlayerCombatEntity> CreateSamplePlayerParty()
    {
        List<PlayerCombatEntity> playerParty = new List<PlayerCombatEntity>();
        playerParty.Add(new PlayerCombatEntity("Brandon", 50, 10));
        playerParty.Add(new PlayerCombatEntity("Spencer", 50, 10));
        return playerParty;
    }
}
