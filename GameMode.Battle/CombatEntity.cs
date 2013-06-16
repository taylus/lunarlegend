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
    public int Speed { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Dictionary<DamageType, CombatRating> CombatRatings { get; set; }

    public const int DEFAULT_ATTACK = 5;
    public const int DEFAULT_DEFENSE = 0;

    public abstract CombatAction DecideAction(List<CombatEntity> allies, List<CombatEntity> enemies);

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

    public void Attack(CombatEntity target)
    {
        target.TakeDamage(CombatRatings[DamageType.Physical].Attack, DamageType.Physical);
    }

    public void Attack(CombatEntity target, Technique tech)
    {
        if (tech != null && tech.ResourceCost <= Resource.Current)
        {
            tech.ActUpon(target);
            Resource.Current -= tech.ResourceCost;
        }
    }

    public void TakeDamage(uint damage, DamageType type)
    {
        damage -= CombatRatings[type].Defense;
        if (damage > 0) Health.Current -= damage;
    }

    public void Heal(uint health)
    {
        Health.Current += health;
    }

    public abstract void Draw(SpriteBatch sb);
}

public class EnemyCombatEntity : CombatEntity
{
    public Texture2D Image { get; set; }
    public Point CenterOffset { get; set; }

    //TODO: loading a map will load all monsters it contains from a persistent store (SQLLite?)

    public EnemyCombatEntity(string name, uint hp, CombatRatings cr, string imgFile, Point? centerOffset = null) : 
        base(name, hp, 0, cr)
    {
        Image = BaseGame.LoadTexture(imgFile, true);
        if (centerOffset == null)
            CenterOffset = Point.Zero;
        else
            CenterOffset = centerOffset.Value;
    }

    public override CombatAction DecideAction(List<CombatEntity> allies, List<CombatEntity> enemies)
    {
        //AI to select what to do...
        //TODO: implement scriptability via Javascript (Jint)
        //return new CombatAction(this, enemies.SelectRandom());
        throw new NotImplementedException();
    }

    public override void Draw(SpriteBatch sb)
    {
        sb.Draw(Image, new Vector2(X, Y), Color.White);
    }

    public void CenterOn(int x, int y)
    {
        if (Image == null) return;
        X = x - (Image.Width / 2) + CenterOffset.X;
        Y = y - (Image.Height / 2) + CenterOffset.Y;
    }

    public void CenterOn(Point p)
    {
        CenterOn(p.X, p.Y);
    }
}

public class PlayerCombatEntity : CombatEntity
{
    public ResourceType ResourceType { get; set; }
    public Box StatusBox { get; set; }

    private const int DEFAULT_STATUSBOX_WIDTH = 160;
    private const int DEFAULT_STATUSBOX_HEIGHT = 100;

    public PlayerCombatEntity(string name, uint hp, uint resource, CombatRatings cr) : 
        base(name, hp, resource, cr)
    {
        StatusBox = new Box(X, Y, DEFAULT_STATUSBOX_WIDTH, DEFAULT_STATUSBOX_HEIGHT);
        ResourceType = ResourceType.Mana;
    }

    public override CombatAction DecideAction(List<CombatEntity> allies, List<CombatEntity> enemies)
    {
        //select action via menus...
        throw new NotImplementedException();
    }

    //TODO: bake all this into a PlayerStatusBox or something?
    public override void Draw(SpriteBatch sb)
    {
        StatusBox.Draw(sb);

        //draw name at the top, centered horizontally
        int nameWidth = (int)BaseGame.Font.MeasureString(Name).X;
        Vector2 namePosition = new Vector2(StatusBox.Rectangle.Center.X - (nameWidth / 2), StatusBox.Y + StatusBox.Padding);
        sb.DrawString(BaseGame.Font, Name, namePosition, Color.White);

        //draw a line under the name
        int nameLineYPos = (int)namePosition.Y + BaseGame.Font.LineSpacing + StatusBox.Padding;
        Util.DrawLine(sb, StatusBox.BorderWidth, new Vector2(StatusBox.X, nameLineYPos), new Vector2(StatusBox.X + StatusBox.Width, nameLineYPos), StatusBox.BorderColor);

        //TODO: color HP green if maxed, red if critical (<= 25%)
        //draw current HP vs maximum
        Vector2 healthLabelPosition = new Vector2(StatusBox.X + StatusBox.Padding, (int)(nameLineYPos + StatusBox.Padding));
        sb.DrawString(BaseGame.Font, String.Format("Health:    {0}/{1}", Health.Current, Health.Maximum), healthLabelPosition, Color.White);

        //draw current resource vs maximum
        if (ResourceType != ResourceType.None && Resource.Maximum > 0)
        {
            Vector2 resourceLabelPosition = new Vector2(StatusBox.X + StatusBox.Padding, (int)(healthLabelPosition.Y + BaseGame.Font.LineSpacing * 1.5f));
            string resourceLabel = ResourceType == ResourceType.Mana ? "Mana: " : "Energy: ";
            sb.DrawString(BaseGame.Font, String.Format("{0}    {1}/{2}", resourceLabel, Resource.Current, Resource.Maximum), resourceLabelPosition, Color.White);
        }
    }

    //public void Update()
    //{
    //    StatusBox.Update();
    //}
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
//Energy = melee/ranger types: start at zero; build up as fight goes on
//Mana = caster/healer types: start at full; healers can regen with skills, casters need to normal attack
public enum ResourceType
{
    None,
    Energy,
    Mana
}