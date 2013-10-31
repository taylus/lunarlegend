using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CombatRatings = System.Collections.Generic.Dictionary<DamageType, CombatRating>;

public class PlayerCombatEntity : CombatEntity
{
    public ResourceType ResourceType { get; private set; }
    public int Width { get { return statusBox.Width; } }
    public int Height { get { return statusBox.Height; } }

    private Box statusBox;

    private const int DEFAULT_STATUSBOX_WIDTH = 150;
    private const int DEFAULT_STATUSBOX_HEIGHT = 100;

    //vertical offset (in pixels) to push the current player's status box up when it's their turn
    private const int CURRENT_PLAYER_OFFSET = 20;

    //preserve the player box's y coordinate for temporary position changes (when they're the current player)
    private int originalY;
    public bool IsCurrent { get; set; }

    private static readonly Color ALIVE_COLOR = Box.DEFAULT_BACKGROUND_COLOR;
    private static readonly Color DEAD_COLOR = Color.Lerp(Color.Transparent, Color.DarkRed, MathHelper.Clamp(Box.DEFAULT_OPACITY, 0, 1));

    public PlayerCombatEntity(string name, uint hp, uint resource, CombatRatings cr = null) :
        base(name, hp, resource, cr)
    {
        statusBox = new Box(X, Y, DEFAULT_STATUSBOX_WIDTH, DEFAULT_STATUSBOX_HEIGHT);
        originalY = statusBox.Y;
        ResourceType = ResourceType.Mana;
    }

    //TODO: bake all this into a PlayerStatusBox or something?
    public void Draw(SpriteBatch sb, bool grayedOut = false, bool currentPlayer = false)
    {
        statusBox.BackgroundColor = IsDead ? DEAD_COLOR : ALIVE_COLOR;
        statusBox.Y = IsCurrent ? originalY - CURRENT_PLAYER_OFFSET : originalY;
        statusBox.Draw(sb);

        //draw name at the top, centered horizontally
        int nameWidth = (int)BaseGame.Font.MeasureString(Name).X;
        Vector2 namePosition = new Vector2(statusBox.Rectangle.Center.X - (nameWidth / 2), statusBox.Y + statusBox.Padding);
        sb.DrawString(BaseGame.Font, Name, namePosition, Color.White);

        //draw a line under the name
        int nameLineYPos = (int)namePosition.Y + BaseGame.Font.LineSpacing + statusBox.Padding;
        Util.DrawLine(sb, statusBox.BorderWidth, new Vector2(statusBox.X, nameLineYPos), new Vector2(statusBox.X + statusBox.Width, nameLineYPos), statusBox.BorderColor);

        //TODO: color HP green if maxed, red if critical (<= 25%)
        //draw current HP vs maximum
        Vector2 healthLabelPosition = new Vector2(statusBox.X + statusBox.Padding, (int)(nameLineYPos + statusBox.Padding));
        sb.DrawString(BaseGame.Font, String.Format("Health:    {0}/{1}", Health.Current, Health.Maximum), healthLabelPosition, Color.White);

        //draw current resource vs maximum
        if (ResourceType != ResourceType.None && Resource.Maximum > 0)
        {
            Vector2 resourceLabelPosition = new Vector2(statusBox.X + statusBox.Padding, (int)(healthLabelPosition.Y + BaseGame.Font.LineSpacing * 1.5f));
            string resourceLabel = ResourceType == ResourceType.Mana ? "Mana: " : "Energy: ";
            sb.DrawString(BaseGame.Font, String.Format("{0}    {1}/{2}", resourceLabel, Resource.Current, Resource.Maximum), resourceLabelPosition, Color.White);
        }
    }

    public void CenterOn(int x, int y)
    {
        statusBox.CenterOn(x, y);
        originalY = statusBox.Y;
    }

    public void CenterOn(Point p)
    {
        statusBox.CenterOn(p);
        originalY = statusBox.Y;
    }

    public void MoveTo(int x, int y)
    {
        statusBox.MoveTo(x, y);
        originalY = statusBox.Y;
    }

    public void MoveTo(Point p)
    {
        statusBox.MoveTo(p);
        originalY = statusBox.Y;
    }
}

//TODO: friendly but AI controlled allies?