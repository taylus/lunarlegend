using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class SpriteDemo : BaseGame
{
    private Sprite carlton = null;
    private Sprite awesome = null;
    private Sprite shibe = null;
    private Sprite background = null;
    private AnimatedSprite explosion = null;
    private AnimatedSprite clonesplosion = null;
    private int spins = 0;
    private bool shook = false;

    public SpriteDemo()
    {
        Window.Title = "Sprite Demo";
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        carlton = new Sprite(@"demo\carlton", 0.75f);
        carlton.MoveTo(-50, -20);
        carlton.Rotation = MathHelper.ToRadians(45);
        //carlton.Blink(Color.White, Color.Purple, TimeSpan.FromMilliseconds(300));
        //carlton.BlinkFor(TimeSpan.FromSeconds(2), Color.White, Color.Purple, TimeSpan.FromMilliseconds(300));
        //carlton.ShakeFor(TimeSpan.FromMilliseconds(100), 5, TimeSpan.FromMilliseconds(50));

        awesome = new Sprite(@"demo\awesome", 0.35f);
        awesome.UpdateCallback = FadeAndRotateSprite;
        awesome.MoveTo(0, GameHeight - (int)awesome.ScaledHeight);

        shibe = new Sprite(@"demo\shibe", 250, 250);
        shibe.Pulse(Color.Blue, TimeSpan.FromMilliseconds(30), 0.05f, 0.6f);
        shibe.MoveTo(570, 360);

        background = new Sprite(@"demo\shibefull");
        background.UpdateInterval = TimeSpan.FromMilliseconds(100);
        background.UpdateCallback = ShakeSprite;
        background.DestinationRectangle = new Rectangle(-20, -20, GameWidth + 40, GameHeight + 40);

        explosion = new AnimatedSprite(@"demo\explosion", 64, 64, 3.0f, Color.Orange);
        explosion.Animation = explosion.CreateDefaultAnimation(TimeSpan.FromMilliseconds(30));
        explosion.CenterOn(675, 90);

        clonesplosion = explosion.Clone();
        clonesplosion.Scale *= 1.2f;
        clonesplosion.Tint *= 0.25f;
        clonesplosion.CenterOn(explosion.Rectangle.Center);
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();

        if (KeyPressedThisFrame(Buttons.CONFIRM))
        {
            //carlton.ShakeFor(TimeSpan.FromMilliseconds(150), 5, TimeSpan.FromMilliseconds(50));
            carlton.FadeOut(TimeSpan.FromSeconds(0.5));
        }

        background.Update(gameTime);
        carlton.Update(gameTime);
        awesome.Update(gameTime);
        shibe.Update(gameTime);
        explosion.Update(gameTime);
        clonesplosion.Update(gameTime);

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGray);

        spriteBatch.Begin();
        background.Draw(spriteBatch);
        carlton.Draw(spriteBatch);
        awesome.Draw(spriteBatch);
        clonesplosion.Draw(spriteBatch);
        explosion.Draw(spriteBatch);
        shibe.Draw(spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private void FlashSpriteRandomColor(Sprite s)
    {
        byte[] random = Guid.NewGuid().ToByteArray();
        s.Tint = new Color(random[0], random[1], random[2], random[3]);
    }

    private void FadeAndRotateSprite(Sprite s)
    {
        if (s.Tint.A > 32 && spins < 2)
        {
            //fade out
            s.Tint = new Color(s.Tint.R - 1, s.Tint.G - 1, s.Tint.B - 1, s.Tint.A - 1);
        }
        else
        {
            //and then rotate twice
            if (spins < 2)
            {
                s.Rotation += MathHelper.ToRadians(4.0f);
                if (s.Rotation >= MathHelper.TwoPi)
                {
                    s.Rotation %= MathHelper.TwoPi;
                    spins++;
                }
            }
            else
            {
                //then fade back in
                s.Tint = new Color(s.Tint.R + 1, s.Tint.G + 1, s.Tint.B + 1, s.Tint.A + 1);

                //and start over again once the fade in finishes
                if (s.Tint.A == 255) spins = 0;
            }
        }
    }

    private void ShakeSprite(Sprite s)
    {
        if (shook)
            s.Rotation = 0;
        else
            s.Rotation = MathHelper.ToRadians(-1.0f);

        shook = !shook;
    }
}
