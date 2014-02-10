using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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
        combatSystem.Engage(Enemies.LoadPartyByID(3), @"battle\cave_bg");
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();

        EffectsManager.Update(gameTime);
        combatSystem.Update(gameTime);
        if (KeyPressedThisFrame(Buttons.DEBUG))
            combatSystem.Engage(Enemies.LoadPartyByID(1), @"battle\cave_bg");
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

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin(0, null, null, null, null, null, EffectsManager.TranslateShake());
        combatSystem.Draw(spriteBatch);
        EffectsManager.Draw(spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private List<PlayerCombatEntity> CreateSamplePlayerParty()
    {
        List<PlayerCombatEntity> playerParty = new List<PlayerCombatEntity>();
        playerParty.Add(new PlayerCombatEntity("Brandon", 50, 25));
        playerParty.Add(new PlayerCombatEntity("Spencer", 50, 50));
        //playerParty.Add(new PlayerCombatEntity("Justin", 10, 5));
        //playerParty.Add(new PlayerCombatEntity("Vicks", 5, 0));
        //playerParty.Add(new PlayerCombatEntity("Wedge", 5, 0));
        return playerParty;
    }
}
