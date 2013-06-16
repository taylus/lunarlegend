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
        combatSystem.Engage("classy_bg.jpg", CreateSampleEnemyParty());
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();

        combatSystem.Update();
        if (KeyPressedThisFrame(Buttons.CONFIRM))
            combatSystem.Messages.AdvanceLines();
        if (KeyPressedThisFrame(Buttons.MOVE_LEFT) || KeyPressedThisFrame(Buttons.MOVE_UP))
            combatSystem.Messages.SelectPreviousChoice();
        if (KeyPressedThisFrame(Buttons.MOVE_RIGHT) || KeyPressedThisFrame(Buttons.MOVE_DOWN))
            combatSystem.Messages.SelectNextChoice();

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        //GraphicsDevice.Clear(Color.DarkGray);

        spriteBatch.Begin();
        combatSystem.Draw(spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private List<PlayerCombatEntity> CreateSamplePlayerParty()
    {
        List<PlayerCombatEntity> playerParty = new List<PlayerCombatEntity>();
        playerParty.Add(new PlayerCombatEntity("Brandon", 50, 10, null));
        playerParty.Add(new PlayerCombatEntity("Spencer", 50, 10, null));
        return playerParty;
    }

    private List<EnemyCombatEntity> CreateSampleEnemyParty()
    {
        List<EnemyCombatEntity> enemyParty = new List<EnemyCombatEntity>();
        enemyParty.Add(new EnemyCombatEntity("Equine Esquire", 25, null, "horsemask_esquire.png", new Point(0, 40)));
        return enemyParty;
    }

    //create the MessageBox whose style, position, etc will be used by all MessageBoxes loaded for this game
    public static MessageBox CreateMessageBoxTemplate()
    {
        int w = 780;
        int h = 1;
        int x = (GameWidth / 2) - (w / 2);
        int y = 10;
        return new MessageBox(x, y, w, h, Font);
    }
}
