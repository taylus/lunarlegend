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
    //private Texture2D enemy;

    public BattleDemo()
    {
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
        Window.Title = "Battle Demo";
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        //dialogue = new MessageBoxSeries(GraphicsDevice.Viewport.Bounds, 10, GameWidth - 20, 80, Font, "MessageBox #1");
        //dialogue.MessageBoxes[0].Choices.Add(new MessageBoxChoice("Forward", null));
        //dialogue.MessageBoxes.AddRange(dialogue.WrapText("Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."));
        dialogue = BuildMessageBoxSeries();
    }

    //build a sample MessageBoxSeries with choices
    //TODO: make a generic GraphML file loader so all the dialogue in the game doesn't have to be built like this
    private MessageBoxSeries BuildMessageBoxSeries()
    {
        MessageBoxSeries mbs = new MessageBoxSeries(GraphicsDevice.Viewport.Bounds, 10, GameWidth - 20, 80, Font);
        MessageBox beginning = mbs.Add("I'm going to ask you a question...");
        MessageBox choiceBox = mbs.Add("Do you want to go backwards, or forwards?");
        MessageBox end = mbs.Add("That's all I had to say.");

        choiceBox.Choices.Add(new MessageBoxChoice("Backwards", beginning, 20, 24));
        choiceBox.Choices.Add(new MessageBoxChoice("Forwards", end, 120, 24));

        return mbs;
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Buttons.QUIT))
            this.Exit();

        if (KeyPressedThisFrame(Buttons.CONFIRM))
            dialogue.Advance();
        if (KeyPressedThisFrame(Buttons.MOVE_LEFT))
            dialogue.Active.SelectPreviousChoice();
        if (KeyPressedThisFrame(Buttons.MOVE_RIGHT))
            dialogue.Active.SelectNextChoice();

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
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
