using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

//represents a turn-based battle
//handles adding/removing CombatEntities from the fight
//detects victory or defeat conditions for transition to appropriate game state
public abstract class CombatSystem
{
    public List<CombatEntity> PlayerParty;
    public List<CombatEntity> EnemyParty;

    public void Fight()
    {
        //while both sides are still standing:
        //all players select actions via UI
        //all enemies select actions via AI
        //sort actions by source's speed and execute in that order

        List<CombatAction> actions = new List<CombatAction>();
        while (!(PlayerVictory() || PlayerDefeat()))
        {
            foreach (CombatEntity player in PlayerParty)
            {
                actions.Add(player.DecideAction(PlayerParty, EnemyParty));
            }

            foreach (CombatEntity enemy in EnemyParty)
            {
                actions.Add(enemy.DecideAction(EnemyParty, PlayerParty));
            }

            actions = actions.OrderBy(a => a.Source.Speed).ToList();
            foreach(CombatAction action in actions)
            {
                action.Perform();
                if (PlayerVictory() || PlayerDefeat()) break;
            }
        }
    }

    public bool PlayerVictory()
    {
        return EnemyParty.Count(e => e.Alive) == 0;
    }

    public bool PlayerDefeat()
    {
        return PlayerParty.Count(p => p.Alive) == 0;
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