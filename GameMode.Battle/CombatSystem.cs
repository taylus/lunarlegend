using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

//represents a turn-based battle between the player's party and an AI-controlled enemy party
//handles adding/removing CombatEntities from the fight (e.g. a monster summons allies)
//detects victory or defeat conditions for transition to appropriate game state
public class CombatSystem
{
    public Texture2D Background;
    public MessageBox Messages;
    public List<PlayerCombatEntity> PlayerParty = new List<PlayerCombatEntity>();
    public List<EnemyCombatEntity> EnemyParty = new List<EnemyCombatEntity>();

    public const int BOX_PADDING = 8;

    public CombatSystem(List<PlayerCombatEntity> playerParty)
    {
        PlayerParty = playerParty;
        AlignPlayers(playerParty);
    }

    public void Engage(string bgFile, List<EnemyCombatEntity> enemyParty)
    {
        Background = BaseGame.LoadTexture(bgFile, true);

        if (enemyParty == null || enemyParty.Count <= 0)
            throw new ArgumentException("Attempted to enter combat with empty enemy party.");

        EnemyParty = enemyParty;
        AlignEnemies(EnemyParty);
        //Messages = new MessageBoxSeries(BattleDemo.CreateMessageBoxTemplate(), GetEngagementText());
        Messages = new MessageBox(BattleDemo.CreateMessageBoxTemplate(), GetEngagementText());
    }

    public bool PlayerVictory()
    {
        return EnemyParty.Count(e => e.IsAlive) == 0;
    }

    public bool PlayerDefeat()
    {
        return PlayerParty.Count(p => p.IsAlive) == 0;
    }

    public bool AreFriendly(CombatEntity ce1, CombatEntity ce2)
    {
        if (PlayerParty.Contains(ce1)) return PlayerParty.Contains(ce2);
        else if (EnemyParty.Contains(ce1)) return EnemyParty.Contains(ce2);
        throw new Exception("Unknown entity \"" + ce1.Name + "\"");
    }

    public bool AreHostile(CombatEntity ce1, CombatEntity ce2)
    {
        return !AreFriendly(ce1, ce2);
    }

    public void Draw(SpriteBatch sb)
    {
        sb.Draw(Background, BaseGame.GameWindow, Color.White);

        foreach (CombatEntity enemy in EnemyParty)
        {
            enemy.Draw(sb);
        }

        foreach (CombatEntity player in PlayerParty)
        {
            player.Draw(sb);
        }

        Messages.Draw(sb);
    }

    public void Update()
    {

    }

    private string GetEngagementText()
    {
        if (EnemyParty.Count > 1)
            return string.Format("{0} and company draw near!", EnemyParty[0].Name);
        else
            return string.Format("{0} draws near!", EnemyParty[0].Name);
    }

    //TODO: generalize these two functions into one that aligns any number of CombatEntities
    //within a given rectangle, that will be determined differently for enemies and players

    private void AlignEnemies(List<EnemyCombatEntity> enemies)
    {
        //TODO: generalize for multiple enemies and screen resolutions
        foreach(EnemyCombatEntity enemy in enemies)
        {
            enemy.CenterOn(BaseGame.GameWindow.Center);
        }
    }

    private void AlignPlayers(List<PlayerCombatEntity> players)
    {
        //TODO: generalize for multiple players and screen resolutions
        //foreach (PlayerCombatEntity player in players)
        //{
        //    int x = BaseGame.GameWindow.Center.X - (player.StatusBox.Width / 2);
        //    int y = BaseGame.GameWindow.Height - (player.StatusBox.Height + player.StatusBox.Margin);
        //    player.StatusBox.MoveTo(x, y);
        //}
        players[0].StatusBox.MoveTo(220, 492);
        players[1].StatusBox.MoveTo(420, 492);
    }
}

//represents a player or enemy's action for one turn
public class CombatAction
{
    public string Description;
    public SoundEffect Sound;
    public Color? ScreenFlash;
    public CombatEntity Source;
    public CombatEntity Target;
    public Technique Technique;

    public void Perform()
    {
        if (Technique != null)
        {
            Source.Attack(Target, Technique);
        }
        else
        {
            Source.Attack(Target);
        }
    }

    public CombatAction(CombatEntity source, CombatEntity target)
    {
        Source = source;
        Target = target;

        if (Technique == null) 
            Description = string.Format("{0} attacks {1}!", source.Name, target.Name);
        else 
            Description = string.Format("{0} casts {1} on {2}!", source.Name, Technique.Name, target.Name);
    }
}