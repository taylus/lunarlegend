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
    private MessageBoxSeries dialogue;

    public BattleDemo()
    {
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = "Battle Demo";
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        dialogue = new MessageBoxSeries(GraphicsDevice.Viewport.Bounds, 10, GameWidth - 20, 80, Font, "This is a combat message!");
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        //exit on esc
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            this.Exit();

        if (Keyboard.GetState().IsKeyDown(Buttons.CONFIRM))
            dialogue.Advance();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGray);

        spriteBatch.Begin();
        dialogue.Draw(spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }
}
