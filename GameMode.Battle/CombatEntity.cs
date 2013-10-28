using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CombatRatings = System.Collections.Generic.Dictionary<DamageType, CombatRating>;

//represents anything that can participate in a battle (player, monster)
//player combat entities will stay instantiated all the time (to be viewable from status menu outside of combat)
//enemy combat entities are instantiated upon entering combat
public abstract class CombatEntity
{
    public string Name { get; set; }
    public Measure Health;
    public Measure Resource;    //mana, energy, etc
    public bool IsAlive { get { return Health.Current > 0; } }
    public bool IsDead { get { return !IsAlive; } }
    public int Speed { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    //base combat stats, before crits, buffs, or any other damage modifiers
    public Dictionary<DamageType, CombatRating> CombatRatings { get; set; }
    public float CriticalDamageModifier = 1.0f;

    public const int DEFAULT_ATTACK = 5;
    public const int DEFAULT_DEFENSE = 0;

    public CombatEntity(string name, uint hp, uint resource, CombatRatings cr)
    {
        Name = name;
        Health.Current = Health.Maximum = hp;
        Resource.Current = Resource.Maximum = resource;
        CombatRatings = new CombatRatings();

        //default all combat ratings except those overridden by the given params
        foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
        {
            if (type == DamageType.None) continue;
            if (cr != null && cr.ContainsKey(type))
                CombatRatings.Add(type, cr[type]);
            else
                CombatRatings.Add(type, new CombatRating(DEFAULT_ATTACK, DEFAULT_DEFENSE));
        }
    }

    public uint TakeDamage(uint damage)
    {
        if (damage > Health.Current)
        {
            Health.Current = 0; //dead
        }
        else
        {
            Health.Current -= damage; //tis but a scratch!
        }

        return damage;
    }

    public uint Heal(uint health)
    {
        uint amountHealed = Math.Max(health, Health.Maximum - Health.Current);
        Health.Current += health;
        return amountHealed;
    }

    public virtual void Update(GameTime currentGameTime)
    {

    }
}

public class EnemyCombatEntity : CombatEntity
{
    //used to differentiate if there's multiple of the same named enemy; e.g. Slime A, Slime B
    public char? ID { get; set; }
    public Texture2D Image { get; set; }
    public BlinkingSpriteOverlay Overlay { get; set; }
    public bool DrawOverlay { get; set; }
    public float Scale { get; set; }
    public Point CenterOffset { get; set; }

    //an enemy's name plus its unique identifier
    public string FullName
    {
        get
        {
            if (ID == null) return Name;
            return string.Format("{0} {1}", Name, ID);
        }
    }

    //TODO: loading a map will load all monsters it contains from a persistent store (SQLite?)
    //monsters will appear on the map like the player or NPCS, and can be avoided

    public EnemyCombatEntity(string name, uint hp, CombatRatings cr, string imgFile, float scale = 1.0f, Point? centerOffset = null, char? id = null) : 
        base(name, hp, 0, cr)
    {
        ID = id;
        Image = BaseGame.LoadTexture(imgFile, true);
        Overlay = new BlinkingSpriteOverlay(Image, Color.Black, 0.5f, TimeSpan.FromSeconds(0.25)) { BlinkEnabled = false };
        Scale = scale;
        if (centerOffset == null)
            CenterOffset = Point.Zero;
        else
            CenterOffset = centerOffset.Value;
    }

    public CombatAction DecideAction(List<EnemyCombatEntity> allies, List<PlayerCombatEntity> enemies)
    {
        //TODO: implement scriptable AI via Javascript (Jint)

        //basic attack a random enemy
        return new CombatAction(this, enemies.Where(e => e.IsAlive).OrderBy(e => Guid.NewGuid()).First());
    }

    public void Draw(SpriteBatch sb)
    {
        sb.Draw(Image, new Vector2(X, Y), null, Color.White, 0, Vector2.Zero, Scale, SpriteEffects.None, 0);
        if (DrawOverlay && Overlay != null) Overlay.Draw(sb, X, Y, Scale);
    }

    public void CenterOn(int x, int y)
    {
        if (Image == null) return;
        X = x - (int)(Image.Width * Scale / 2) + CenterOffset.X;
        Y = y - (int)(Image.Height * Scale / 2) + CenterOffset.Y;
    }

    public void CenterOn(Point p)
    {
        CenterOn(p.X, p.Y);
    }

    public override void Update(GameTime currentGameTime)
    {
        if(DrawOverlay) Overlay.Update(currentGameTime);
    }
}

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

//represents a constrained current/maximum value
public struct Measure
{
    private uint current;
    public uint Current 
    {
        get { return current; }
        set
        {
            current = value;
            if (current > Maximum) current = Maximum;
        }
    }

    public uint Maximum { get; set; }
}

public struct CombatRating
{
    public uint Attack;
    public uint Defense;

    public CombatRating(uint attack, uint defense)
    {
        Attack = attack;
        Defense = defense;
    }
}

public enum DamageType
{
    None,
    Physical,
    Earth,
    Wind,
    Water,
    Fire,
    Holy,
    Shadow,
    Astral
}

//different player classes have different types of resources, with different behavior
//Energy = physical types: start at zero; build up as fight goes on
//Mana = caster/healer types: start at full; healers can regen with skills, casters need to normal attack
public enum ResourceType
{
    None,
    Energy,
    Mana
}