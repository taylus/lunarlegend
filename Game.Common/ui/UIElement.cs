using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//anything that can be drawn on the screen as part of the HUD
//game manages a collection of these to implement its UI
public abstract class UIElement
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Rectangle Rectangle { get { return new Rectangle(X, Y, Width, Height); } }
    public bool Visible { get; set; }

    public UIElement()
    {
        Visible = true;
    }

    public abstract void Draw(SpriteBatch sb);

    //draw this element into the given RenderSprite, allowing for sprite effects to be applied
    //this must be called outside of this SpriteBatch's begin/end
    public virtual void DrawTo(RenderTargetSprite target, SpriteBatch sb, GraphicsDevice gd)
    {
        gd.SetRenderTarget(target.RenderTarget);
        sb.Begin();
        Draw(sb);
        sb.End();
        gd.SetRenderTarget(null);
    }

    public virtual void Update(GameTime currentGameTime)
    {

    }

    public virtual void MoveTo(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void MoveTo(Point p)
    {
        MoveTo(p.X, p.Y);
    }

    public virtual void CenterOn(int x, int y)
    {
        MoveTo(x - (Width / 2), y - (Height / 2));
    }

    public void CenterOn(Point p)
    {
        CenterOn(p.X, p.Y);
    }
}