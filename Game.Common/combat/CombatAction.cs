using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

//represents a player or enemy's action for one turn
public class CombatAction
{
    public CombatEntity Source;
    public CombatEntity Target;
    public Technique Technique;
    public SoundEffect Sound;
    public Color? ScreenFlash;

    public CombatAction(CombatEntity source, CombatEntity target)
    {
        Source = source;
        Target = target;
    }

    public uint Execute()
    {
        if (Technique == null)
        {
            return Target.TakeDamage(CalculateDamage());
        }
        else
        {
            if (Technique.ResourceCost <= Source.Resource.Current)
            {
                Source.Resource.Current -= Technique.ResourceCost;
                return Technique.ActUpon(Target);
            }
            else
            {
                //UI should not allow this
                throw new Exception("Attempted to use technique without enough resources!");
            }
        }
    }

    public uint CalculateDamage()
    {
        //determine damage type
        DamageType type = DamageType.Physical;
        if (Technique != null && Technique.GetType() == typeof(DamageType))
        {
            type = ((DamageTechnique)Technique).Type;
        }

        //determine base damage before crit and defenses
        //a technique's damage is calculated to be (Power + Attack for that school) * Crit
        uint baseDamage = Source.CombatRatings[type].Attack;
        if (Technique != null && Technique.GetType() == typeof(DamageType))
        {
            baseDamage += Technique.Power;
        }

        //apply crit and defenses to determine final damage
        float damage = baseDamage * Source.CriticalDamageModifier;
        uint defense = Target.CombatRatings[type].Defense;

        //always do at least 1 damage
        if (defense >= damage) return 1;
        return (uint)(damage - defense);
    }
}