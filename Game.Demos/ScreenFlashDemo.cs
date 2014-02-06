using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class ScreenFlashDemo : BaseGame
{
    private Texture2D background = null;

    public ScreenFlashDemo()
    {
        Window.Title = "ScreenFlash Demo";
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        background = LoadTexture(@"demo\dogetestscreen");
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        EffectsManager.Update(gameTime);

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();
        if (KeyPressedThisFrame(Keys.Space))
        {
            EffectsManager.ScreenFlash(Color.Red);
        }
        if (curMouse.MiddleButton == ButtonState.Pressed)
        {
            EffectsManager.ScreenShake(15.0f, TimeSpan.FromSeconds(0.6), TimeSpan.FromMilliseconds(50));
        }
        if (curMouse.LeftButton == ButtonState.Pressed)
        {
            EffectsManager.PutSprite(new AnimatedSprite(@"demo\explosion", 64, 64, 3.0f, Color.White), curMouse.Position().ToPoint());
        }

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        spriteBatch.Begin(0, null, null, null, null, null, EffectsManager.TranslateShake());
        spriteBatch.Draw(background, GameWindow, Color.White);
        EffectsManager.Draw(spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }
}
