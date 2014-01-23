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
    private Sprite doge;
    private BaseMenuBox activeMenu;
    private MenuBox<string> monthMenu;
    private MenuBox<string> confirmMenu;
    private MenuBox<Fruit> fruitMenu;

    private class Fruit : Sprite
    {
        public string Name;

        public Fruit(string imgFile, string name) : base(imgFile)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public MenuDemo()
    {

    }

    protected override void LoadContent()
    {
        base.LoadContent();
        msg = CreateMessageBox();
        doge = new Sprite("demo/shibe.gif");
        doge.Rotation = MathHelper.ToRadians(20);
        doge.MoveTo(550, 395);
        doge.Visible = false;
        activeMenu = monthMenu = CreateMonthMenu();
        confirmMenu = CreateConfirmMenu();
        fruitMenu = CreateFruitMenu();
        confirmMenu.Visible = false;
        fruitMenu.Visible = false;
    }

    protected override void Update(GameTime gameTime)
    {
        //don't respond to input if the game isn't active
        if (!IsActive) return;

        curKeyboard = Keyboard.GetState();
        curMouse = Mouse.GetState();

        //exit on esc
        if (curKeyboard.IsKeyDown(Buttons.QUIT)) this.Exit();

        if (KeyPressedThisFrame(Buttons.MOVE_LEFT))
        {
            if(activeMenu != null) activeMenu.SelectLeftChoice();
        }
        if (KeyPressedThisFrame(Buttons.MOVE_UP))
        {
            if (activeMenu != null) activeMenu.SelectAboveChoice();
        }
        if (KeyPressedThisFrame(Buttons.MOVE_RIGHT))
        {
            if (activeMenu != null) activeMenu.SelectRightChoice();
        }
        if (KeyPressedThisFrame(Buttons.MOVE_DOWN))
        {
            if (activeMenu != null) activeMenu.SelectBelowChoice();
        }
        if (KeyPressedThisFrame(Buttons.CONFIRM))
        {
            if (activeMenu == monthMenu)
            {
                msg.Text = string.Format("{0}, huh?", monthMenu.SelectedText);
                confirmMenu.Visible = true;
                activeMenu = confirmMenu;
            }
            else if (activeMenu == confirmMenu)
            {
                if (confirmMenu.SelectedText == "Yes")
                {
                    msg.Text = "That's my favorite, too.";
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
            else if (activeMenu == fruitMenu)
            {
                if (fruitMenu.Choices.Count > 2)
                {
                    msg.Text = string.Format("Much gross, doge hate {0}.", fruitMenu.SelectedText.ToLower());
                    fruitMenu.RemoveSelection();
                }
                else
                {
                    msg.Text = string.Format("Wow, doge love {0}!", fruitMenu.SelectedText.ToLower());
                    doge.Image = BaseGame.LoadTexture("demo/shibehappy.gif", true);
                    fruitMenu.Visible = false;
                    activeMenu = null;
                }
            }
            else
            {
                if (fruitMenu.Choices.Count > 2)
                {
                    msg.Text = "Give fruit to doge?";
                    fruitMenu.Visible = true;
                    doge.Visible = true;
                    activeMenu = fruitMenu;
                }
                else
                {
                    Exit();
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
        fruitMenu.Draw(spriteBatch);
        confirmMenu.Draw(spriteBatch);
        doge.Draw(spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private static MessageBox CreateMessageBox()
    {
        int w = 780;
        int h = 1;
        int x = (GameWidth / 2) - (w / 2);
        int y = 10;
        return new MessageBox(x, y, w, h, Font, "What's your favorite month?");
    }

    private static MenuBox<string> CreateMonthMenu()
    {
        int w = 400;
        int h = 3;
        int cols = 4;
        int x = 10;
        int y = 50;
        return new MenuBox<string>(x, y, w, h, cols, Font, GetMonthNames());
    }

    private static MenuBox<Fruit> CreateFruitMenu()
    {
        int w = 450;
        int h = 1;
        int cols = 3;
        int x = 10;
        int y = 50;
        return new MenuBox<Fruit>(x, y, w, h, cols, Font, CreateFruit());
    }

    private static MenuBox<string> CreateConfirmMenu()
    {
        int w = 100;
        int h = 1;
        int cols = 2;
        int x = 415;
        int y = 50;
        return new MenuBox<string>(x, y, w, h, cols, Font, "Yes", "No");
    }

    private static string[] GetMonthNames()
    {
        //too cool to just hard code an array
        return Enumerable.Range(1, 12).Select(i => CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)).ToArray();
    }

    private static Fruit[] CreateFruit()
    {
        return new Fruit[] 
        {
            new Fruit("demo/apple.png", "Apple"),
            new Fruit("demo/watermelon.png", "Watermelon"),
            new Fruit("demo/grape.png", "Grapes")
        };
    }
}
