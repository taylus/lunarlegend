using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CombatRatings = System.Collections.Generic.Dictionary<DamageType, CombatRating>;

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

    //TODO: loading a map will load all monsters it contains from a persistent store
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
        if (DrawOverlay) Overlay.Update(currentGameTime);
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