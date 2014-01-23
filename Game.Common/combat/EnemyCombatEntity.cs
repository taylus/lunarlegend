using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CombatRatings = System.Collections.Generic.Dictionary<DamageType, CombatRating>;

public class EnemyCombatEntity : CombatEntity
{
    private Sprite sprite;
    public Point CenterOffset { get; set; } 
    public bool HasSpriteEffects { get { return sprite.UpdateCallback != null; } }

    //used to differentiate if there's multiple of the same named enemy; e.g. Slime A, Slime B
    public char? ID { get; set; }

    public Color Tint
    {
        get
        {
            return sprite.Tint;
        }
        set
        {
            sprite.Tint = value;
        }
    }

    //an enemy's name plus its unique identifier, to differentiate multiples of the same enemy type
    public override string FullName 
    { 
        get 
        {
            if (ID == null) return Name;
            return string.Format("{0} {1}", Name, ID);
        } 
    }

    //TODO: loading a map will load all monsters it contains from a persistent store
    //monsters will appear on the map like the player or NPCS, and can be avoided

    public EnemyCombatEntity(string name, uint hp, CombatRatings cr, string imgFile, float scale = 1.0f, Point? centerOffset = null, char? id = null) :
        base(name, hp, 0, cr)
    {
        ID = id;
        sprite = new Sprite(imgFile, scale);
        if (centerOffset.HasValue) CenterOffset = centerOffset.Value;
    }

    public CombatAction DecideAction(List<EnemyCombatEntity> allies, List<PlayerCombatEntity> enemies)
    {
        //TODO: implement scriptable AI via Javascript (Jint)

        //basic attack a random enemy
        return new CombatAction(this, enemies.Where(e => e.IsAlive).OrderBy(e => Guid.NewGuid()).First());
    }

    public void Draw(SpriteBatch sb)
    {
        //sb.Draw(Image, new Vector2(X, Y), null, Color.White, 0, Vector2.Zero, Scale, SpriteEffects.None, 0);
        sprite.Draw(sb);
    }

    public void CenterOn(int x, int y)
    {
        sprite.CenterOn(x + CenterOffset.X, y + CenterOffset.Y);
    }

    public void CenterOn(Point p)
    {
        sprite.CenterOn(p);
    }

    public override void Update(GameTime currentGameTime)
    {
        sprite.Update(currentGameTime);
    }

    public void StartBlink(int ms = 300)
    {
        sprite.SetBlink(Color.White, Color.DarkGray, TimeSpan.FromMilliseconds(ms));
    }

    public void StopBlink()
    {
        sprite.StopBlink();
    }
}

//TODO: instead of hardcoding, load enemies and party configurations from a persistent store (SQLite)
public static class Enemies
{
    public static List<EnemyCombatEntity> LoadPartyByID(int id)
    {
        List<EnemyCombatEntity> enemyParty = new List<EnemyCombatEntity>();
        switch(id)
        {
            case 1:
                //4 weak lime slimes
                for (int i = 0; i < 4; i++)
                {
                    enemyParty.Add(new EnemyCombatEntity("Lime Slime", 6, null, "battle/slime.png", 3.0f, null, Util.GetLetterByNumber(i)));
                }
                break;
            case 2:
                //3 powerful lime slimes to test losing a battle
                for (int i = 0; i < 3; i++)
                {
                    Dictionary<DamageType, CombatRating> stats = new Dictionary<DamageType, CombatRating>();
                    stats.Add(DamageType.Physical, new CombatRating(25, 5));
                    enemyParty.Add(new EnemyCombatEntity("Lime Slime", 12, stats, "battle/slime.png", 3.0f, null, Util.GetLetterByNumber(i)));
                }
                break;
            default:
                //missingno
                enemyParty.Add(new EnemyCombatEntity("Equine Esquire", 25, null, "battle/horsemask_esquire.png", 1.0f, new Point(0, 40)));
                break;
        }
        return enemyParty;
    }
}