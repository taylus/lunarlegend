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

    public CombatEntity(string name, uint hp, uint resource, CombatRatings stats)
    {
        Name = name;
        Health.Current = Health.Maximum = hp;
        Resource.Current = Resource.Maximum = resource;
        CombatRatings = new CombatRatings();

        //default all combat ratings except those overridden by the given stats
        foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
        {
            if (stats != null && stats.ContainsKey(type))
                CombatRatings.Add(type, stats[type]);
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
            Health.Current -= damage;
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

//basic 4 elements plus light/dark
//holy magic used offensively qualifies as astral, but astral in general is more like generic arcane magic
public enum DamageType
{
    Physical,
    Earth,
    Wind,
    Water,
    Fire,
    Astral,
    Shadow
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