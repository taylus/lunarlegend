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
        //dialogue = BuildMessageBoxSeriesManually();
        dialogue = BuildMessageBoxSeriesFromGraph("dlg/sample_dialogue.graphml");
    }

    private MessageBoxSeries BuildMessageBoxSeriesFromGraph(string graphMLFile)
    {
        MessageBoxSeries mbs = new MessageBoxSeries(GraphicsDevice.Viewport.Bounds, 10, GameWidth - 600, 5, Font);
        mbs.MessageBoxes.AddRange(mbs.LoadFromGraphFile(Path.Combine(contentManager.RootDirectory, graphMLFile)));

        return mbs;
    }

    //build a sample MessageBoxSeries with choices
    private MessageBoxSeries BuildMessageBoxSeriesManually()
    {
        MessageBoxSeries mbs = new MessageBoxSeries(GraphicsDevice.Viewport.Bounds, 10, GameWidth - 600, 5, Font);
        MessageBox beginning = mbs.Add("I'm going to ask you a question...\nbut you should know that this message box is pretty small.\n It won't be able to hold all of the text that I'm trying to fit inside it.");
        MessageBox choiceBox = mbs.Add("This box is also a bit cramped, but there's not much we can do about that. I suppose I'll ask you that question now. Where do you see this conversation headed?");
        MessageBox end = mbs.Add("This concludes your message box experience. Be kind, please rewind.");

        choiceBox.AddChoice("Backwards", beginning);
        choiceBox.AddChoice("Forwards", end);
        choiceBox.AddChoice("I don't know", end);
        choiceBox.AddChoice("An abrupt end", end);
        choiceBox.AddChoice("No seriously, stop", end);
        choiceBox.AddChoice("This only fits because the box height scales. Width doesn't, though...", end);

        end.AddChoice("Okay", beginning);
        end.AddChoice("No", end);

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
        if (KeyPressedThisFrame(Buttons.MOVE_LEFT) || KeyPressedThisFrame(Buttons.MOVE_UP))
            dialogue.Active.SelectPreviousChoice();
        if (KeyPressedThisFrame(Buttons.MOVE_RIGHT) || KeyPressedThisFrame(Buttons.MOVE_DOWN))
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
