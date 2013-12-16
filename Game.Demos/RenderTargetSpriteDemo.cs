using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class RenderTargetSpriteDemo : BaseGame
{
    private Sprite background = null;
    private Dictionary<MessageBox, RenderTargetSprite> msgBoxes = null;
    private bool shook = false;

    public RenderTargetSpriteDemo()
    {
        Window.Title = "RenderTargetSprite Demo";
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        background = new Sprite("demo/shibe.jpg");
        background.DestinationRectangle = new Rectangle(-20, -20, GameWidth + 40, GameHeight + 40);

        msgBoxes = CreateMessageBoxes();
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();

        msgBoxes.Select(kvp => kvp.Value).ToList().ForEach(v => v.Update(gameTime));

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        msgBoxes.ToList().ForEach(kvp => kvp.Key.DrawTo(kvp.Value, spriteBatch, GraphicsDevice));

        GraphicsDevice.Clear(Color.DarkGray);

        spriteBatch.Begin();
        background.Draw(spriteBatch);
        msgBoxes.Select(kvp => kvp.Value).ToList().ForEach(v => v.Draw(spriteBatch));
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private Dictionary<MessageBox, RenderTargetSprite> CreateMessageBoxes()
    {
        Dictionary<MessageBox, RenderTargetSprite> msgBoxes = new Dictionary<MessageBox, RenderTargetSprite>();

        MessageBox msgBox0 = new MessageBox(0, 0, 150, 1, BaseGame.Font, "sprite effects wow");
        RenderTargetSprite msgBoxSprite0 = new RenderTargetSprite(msgBox0);
        msgBoxSprite0.CenterOn(380, 30);
        msgBoxSprite0.Tint = Color.Orange;
        msgBoxes.Add(msgBox0, msgBoxSprite0);

        MessageBox msgBox1 = new MessageBox(0, 0, 100, 1, BaseGame.Font, "such pulse");
        RenderTargetSprite msgBoxSprite1 = new RenderTargetSprite(msgBox1);
        msgBoxSprite1.CenterOn(100, 160);
        msgBoxSprite1.SetPulse(Color.Blue, TimeSpan.FromMilliseconds(50), 0.04f, 0.5f);
        msgBoxes.Add(msgBox1, msgBoxSprite1);

        MessageBox msgBox2 = new MessageBox(0, 0, 100, 1, BaseGame.Font, "such rotate");
        RenderTargetSprite msgBoxSprite2 = new RenderTargetSprite(msgBox2);
        msgBoxSprite2.CenterOn(740, 560);
        msgBoxSprite2.Rotation = MathHelper.ToRadians(-25);
        msgBoxes.Add(msgBox2, msgBoxSprite2);

        MessageBox msgBox3 = new MessageBox(0, 0, 80, 1, BaseGame.Font, "so blink");
        RenderTargetSprite msgBoxSprite3 = new RenderTargetSprite(msgBox3);
        msgBoxSprite3.CenterOn(740, 70);
        msgBoxSprite3.SetBlink(Color.Gray, Color.White, TimeSpan.FromMilliseconds(300));
        msgBoxes.Add(msgBox3, msgBoxSprite3);

        MessageBox msgBox4 = new MessageBox(0, 0, 85, 1, BaseGame.Font, "so shake");
        RenderTargetSprite msgBoxSprite4 = new RenderTargetSprite(msgBox4);
        msgBoxSprite4.CenterOn(100, 500);
        msgBoxSprite4.UpdateInterval = TimeSpan.FromMilliseconds(70);
        msgBoxSprite4.UpdateCallback = ShakeSprite;
        msgBoxes.Add(msgBox4, msgBoxSprite4);

        MessageBox msgBox5 = new MessageBox(0, 0, 110, 1, BaseGame.Font, "very fade");
        RenderTargetSprite msgBoxSprite5 = new RenderTargetSprite(msgBox5);
        msgBoxSprite5.CenterOn(680, 320);
        msgBoxSprite5.SetPulse(Color.Transparent, TimeSpan.FromMilliseconds(60), 0.09f, 0.75f);
        msgBoxes.Add(msgBox5, msgBoxSprite5);

        MessageBox msgBox6 = new MessageBox(0, 0, 107, 1, BaseGame.Font, "upside doge");
        RenderTargetSprite msgBoxSprite6 = new RenderTargetSprite(msgBox6);
        msgBoxSprite6.CenterOn(450, 580);
        msgBoxSprite6.Rotation = MathHelper.Pi;
        msgBoxes.Add(msgBox6, msgBoxSprite6);

        return msgBoxes;
    }

    private void ShakeSprite(Sprite s)
    {
        if (shook)
            s.Rotation = 0;
        else
            s.Rotation = MathHelper.ToRadians(-3.0f);

        shook = !shook;
    }
}
