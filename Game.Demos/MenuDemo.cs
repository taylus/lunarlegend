using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class MenuDemo : BaseGame
{
    private MessageBox msg;

    private MenuBox activeMenu;
    private MenuBox monthMenu;
    private MenuBox confirmMenu;

    public MenuDemo()
    {

    }

    protected override void LoadContent()
    {
        base.LoadContent();
        msg = CreateMessageBox();
        activeMenu = monthMenu = CreateMonthMenu();
        confirmMenu = CreateConfirmMenu();
        confirmMenu.Visible = false;
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();

        //if (KeyPressedThisFrame(Buttons.CONFIRM) && msg != null)
        //{
        //    if (msg.HasMoreLinesToDisplay)
        //    {
        //        msg.AdvanceLines();
        //    }
        //    else if (msg.Next != null)
        //    {
        //        msg = msg.Next;
        //    }
        //}

        if (activeMenu != null)
        {
            if (KeyPressedThisFrame(Buttons.MOVE_LEFT))
            {
                activeMenu.SelectLeftChoice();
            }
            if (KeyPressedThisFrame(Buttons.MOVE_UP))
            {
                activeMenu.SelectAboveChoice();
            }
            if (KeyPressedThisFrame(Buttons.MOVE_RIGHT))
            {
                activeMenu.SelectRightChoice();
            }
            if (KeyPressedThisFrame(Buttons.MOVE_DOWN))
            {
                activeMenu.SelectBelowChoice();
            }
            if (KeyPressedThisFrame(Buttons.CONFIRM))
            {
                if (activeMenu == monthMenu)
                {
                    msg = new MessageBox(msg, string.Format("{0}, huh?", monthMenu.SelectedText));
                    confirmMenu.Visible = true;
                    activeMenu = confirmMenu;
                }
                else if (activeMenu == confirmMenu)
                {
                    if (confirmMenu.SelectedText == "Yes")
                    {
                        msg = new MessageBox(msg, "That's my favorite, too.");
                        monthMenu.Visible = false;
                        confirmMenu.Visible = false;
                        activeMenu = null;
                    }
                    else
                    {
                        confirmMenu.ResetSelection();
                        confirmMenu.Visible = false;
                        activeMenu = monthMenu;
                        msg = CreateMessageBox();
                    }
                }
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
        monthMenu.Draw(spriteBatch);
        confirmMenu.Draw(spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    public static MessageBox CreateMessageBox()
    {
        int w = 780;
        int h = 1;
        int x = (GameWidth / 2) - (w / 2);
        int y = 10;
        return new MessageBox(x, y, w, h, Font, "Select your favorite month:");
    }

    public static MenuBox CreateMonthMenu()
    {
        int w = 400;
        int h = 3;
        int cols = 4;
        int x = 10;
        int y = 50;
        return new MenuBox(x, y, w, h, cols, Font, GetMonthNames());
    }

    public static MenuBox CreateConfirmMenu()
    {
        int w = 100;
        int h = 1;
        int cols = 2;
        int x = 415;
        int y = 50;
        return new MenuBox(x, y, w, h, cols, Font, "Yes", "No");
    }

    public static string[] GetMonthNames()
    {
        //too cool to just hard code an array
        return Enumerable.Range(1, 12).Select(i => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)).ToArray();
    }
}
