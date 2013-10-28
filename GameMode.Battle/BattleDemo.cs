using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class BattleDemo : BaseGame
{
    private CombatSystem combatSystem;
    public const int BOX_SCREEN_MARGIN = 8;

    public BattleDemo()
    {
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = "Vidya Gaem";
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        combatSystem = new CombatSystem(CreateSamplePlayerParty());
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();

        combatSystem.Update(gameTime);
        if (KeyPressedThisFrame(Buttons.DEBUG))
        {
            combatSystem.Engage("img/cave_bg.jpg", CreateSampleEnemyParty());
        }
        if (KeyPressedThisFrame(Buttons.CONFIRM))
        {
            combatSystem.ConfirmKeyPressed();
        }
        if (KeyPressedThisFrame(Buttons.CANCEL))
        {
            combatSystem.CancelKeyPressed();
        }
        if (KeyPressedThisFrame(Buttons.MOVE_LEFT))
        {
            combatSystem.LeftKeyPressed();
        }
        if (KeyPressedThisFrame(Buttons.MOVE_RIGHT))
        {
            combatSystem.RightKeyPressed();
        }
        if (KeyPressedThisFrame(Buttons.MOVE_UP))
        {
            combatSystem.UpKeyPressed();
        }
        if (KeyPressedThisFrame(Buttons.MOVE_DOWN))
        {
            combatSystem.DownKeyPressed();
        }

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGray);

        spriteBatch.Begin();
        combatSystem.Draw(spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private List<PlayerCombatEntity> CreateSamplePlayerParty()
    {
        List<PlayerCombatEntity> playerParty = new List<PlayerCombatEntity>();
        playerParty.Add(new PlayerCombatEntity("Brandon", 50, 10));
        playerParty.Add(new PlayerCombatEntity("Spencer", 50, 10));
        //playerParty.Add(new PlayerCombatEntity("Justin", 10, 5));
        //playerParty.Add(new PlayerCombatEntity("Vicks", 5, 0));
        //playerParty.Add(new PlayerCombatEntity("Wedge", 5, 0));
        return playerParty;
    }

    private List<EnemyCombatEntity> CreateSampleEnemyParty()
    {
        List<EnemyCombatEntity> enemyParty = new List<EnemyCombatEntity>();
        //enemyParty.Add(new EnemyCombatEntity("Equine Esquire", 25, null, "horsemask_esquire.png", new Point(0, 40)));
        for (int i = 0; i < 3; i++)
        {
            Dictionary<DamageType, CombatRating> stats = new Dictionary<DamageType, CombatRating>();
            stats.Add(DamageType.Physical, new CombatRating(25, 5));
            enemyParty.Add(new EnemyCombatEntity("Lime Slime", 12, stats, "img/slime.png", 3.0f, null, GetLetterByNumber(i)));
        }
        return enemyParty;
    }

    //gets the letter in the alphabet at the given position (e.g. 1 = A, 2 = B, ...)
    //used for assigning unique names to monsters when there's multiple copies of the same type of monster
    //unique names aren't required, it just helps the player and makes targetting not feel as weird
    //wraps around after 26, but if you have this many enemies at once then I don't want to play your turn-based game
    private char GetLetterByNumber(int i)
    {
        return (char)Enumerable.Range('A', 'Z' - 'A' + 1).ElementAt(i % 26);
    }

    //create the MessageBox whose style, position, etc will be used by all MessageBoxes loaded for this game
    public static MessageBox CreateMessageBoxTemplate()
    {
        MenuBox mainMenu = CreateMainMenuBoxTemplate();
        int w = GameWidth - mainMenu.Width - (2 * BOX_SCREEN_MARGIN);
        int h = 4;
        int x = mainMenu.X + mainMenu.Width - mainMenu.BorderWidth;
        int y = mainMenu.Y;
        return new MessageBox(x, y, w, h, Font);
    }

    public static MenuBox CreateMainMenuBoxTemplate()
    {
        int w = 75;
        int h = 4;
        int cols = 1;
        int x = BOX_SCREEN_MARGIN;
        int y = BOX_SCREEN_MARGIN;
        return new MenuBox(x, y, w, h, cols, Font);
    }

    public static PowerMeter CreatePowerMeter()
    {
        int w = 350;
        int h = 40;
        int x = 225;
        int y = 420;
        PowerMeter pm = new PowerMeter(x, y, w, h);
        pm.Profiles.Add(new PowerMeterProfile("XX======XX", 6.0f));
        //pm.Profiles.Add(new PowerMeterProfile("===-XX-===", 6.0f));
        pm.IsActive = pm.Visible = false;
        return pm;
    }
}
