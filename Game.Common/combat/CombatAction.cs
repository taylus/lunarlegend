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

    public CombatAction(CombatEntity source, CombatEntity target, Technique tech = null)
    {
        Source = source;
        Target = target;
        Technique = tech;
    }

    //perform this action, and return a textual representation of what occurred for display in battle
    public string Execute()
    {
        string description = null;

        if (Technique == null)
        {
            uint damage = CalculateDamage();
            description = string.Format("{0} attacks {1} for {2} damage!", Source.FullName, Target.FullName, damage);
            Target.TakeDamage(damage);
        }
        else
        {
            if (Technique.ResourceCost >= 0 && Technique.ResourceCost <= Source.Resource.Current)
            {
                Source.Resource.Current -= Technique.ResourceCost;
                uint damage = CalculateDamage();
                description = string.Format("{0} casts {1} on {2} for {3} damage!", Source.FullName, Technique.Name, Target.FullName, damage);
                Target.TakeDamage(damage);
            }
            else
            {
                //trap: UI shouldn't let us get here
                throw new Exception("UI Error: Attempted to use technique without sufficient resources.");
            }
        }

        if (Target.IsDead) description += string.Format("\n{0} is defeated!", Target.FullName);
        return description;
    }

    public uint CalculateDamage()
    {
        //determine damage type: physical unless a technique is being used
        DamageType type = DamageType.Physical;
        if (Technique != null && Technique.GetType() == typeof(DamageTechnique))
        {
            type = ((DamageTechnique)Technique).Type;
        }

        //determine base damage before crit and defenses
        //a technique's damage is calculated to be (Power + Attack for that school) * Crit
        uint baseDamage = Source.CombatRatings[type].Attack;
        if (Technique != null && Technique.GetType() == typeof(DamageTechnique))
        {
            baseDamage += ((DamageTechnique)Technique).Power;
        }

        //if a player is normal attacking, apply attack from equipped weapon
        //if (Source.GetType() == typeof(PlayerCombatEntity) && Technique == null)
        //{
        //    Weapon weapon = ((PlayerCombatEntity)Source).Weapon;
        //    if (weapon != null) baseDamage += weapon.Attack;
        //}

        //apply crit and defenses to determine final damage
        float damage = baseDamage * Source.CriticalDamageModifier;
        uint defense = Target.CombatRatings[type].Defense;

        //if target is a player, apply defense from equipped armor
        //if (Target.GetType() == typeof(PlayerCombatEntity))
        //{
        //    Armor armor = ((PlayerCombatEntity)Target).Armor;
        //    if (armor != null) defense += armor.Defense[type];
        //}

        //always do at least 1 damage
        if (defense >= damage) return 1;
        return (uint)(damage - defense);
    }
}