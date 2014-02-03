using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class PowerMeterDemo : BaseGame
{
    private MessageBox msg;
    private PowerMeter meter;

    //TODO: pressing up and down cycles through different meter layouts,
    //      pressing left and right increases or decreases cursor speed

    public PowerMeterDemo()
    {

    }

    protected override void LoadContent()
    {
        base.LoadContent();
        msg = CreateMessageBox();
        msg.Visible = false;
        meter = CreatePowerMeter();
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();

        meter.Update();

        if (KeyPressedThisFrame(Buttons.CONFIRM))
        {
            if (meter.IsActive)
            {
                PowerMeterResult result = meter.ConfirmCursor();
                if (result == PowerMeterResult.MISS)
                {
                    msg.Visible = true;
                    msg.Text += "\nYou missed...";
                    meter.IsActive = false;
                    msg.Text += "\nTotal damage modifier: " + meter.DamageModifier;
                }
                else
                {
                    if (result == PowerMeterResult.HIT)
                    {
                        msg.Visible = true;
                        msg.Text += "\nNormal damage.";
                    }
                    else
                    {
                        msg.Visible = true;
                        msg.Text += "\nCritical strike!";
                    }

                    if (!meter.Advance())
                    {
                        meter.IsActive = false;
                        msg.Text += "\nTotal damage modifier: " + meter.DamageModifier;
                    }
                }
            }
            else
            {
                meter.Reset();
                meter.IsActive = true;
                msg.Text = "";
                msg.Visible = false;
            }
        }

        prevKeyboard = curKeyboard;
        prevMouse = curMouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.DarkGray);

        spriteBatch.Begin();
        msg.Draw(spriteBatch);
        meter.Draw(spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    public static MessageBox CreateMessageBox()
    {
        int w = 780;
        int h = 4;
        int x = (GameWidth / 2) - (w / 2);
        int y = 10;
        MessageBox mb = new MessageBox(x, y, w, h, Font);
        mb.BackgroundColor = Color.Black;
        mb.BorderColor = Color.White;
        return mb;
    }

    public static PowerMeter CreatePowerMeter()
    {
        int w = 350;
        int h = 40;
        int x = (GameWidth / 2) - (w / 2);
        int y = (GameHeight / 2) - (h / 2);
        PowerMeter pm = new PowerMeter(x, y, w, h);
        pm.Patterns.Add(new PowerMeterPattern("====XX====", 5.0f));
        pm.Patterns.Add(new PowerMeterPattern("===-XX-===", 6.0f));
        pm.Patterns.Add(new PowerMeterPattern("==--XX--==", 7.0f));
        return pm;
    }
}
