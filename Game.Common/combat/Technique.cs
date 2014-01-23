using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//represents any special attack or spell
//different types of spells and abilities are subclasses (e.g. offensive, defensive, buff/debuff)
//variants/ranks of those spells and abilities are just differently-configured instances of those subclasses
//TODO: this would be a good candidate, along with specific monster types, to persist using SQLite
public abstract class Technique
{
    public CombatAction OwningAction { get; protected set; }
    public uint ResourceCost { get; protected set; }
    public uint Power { get; protected set; }
    public string Name { get; private set; }

    public abstract void ActUpon(CombatEntity target);
    public abstract string DescribeUsage(CombatEntity source, CombatEntity target);
}

public class DamageTechnique : Technique
{
    public DamageType Type { get; private set; }

    public DamageTechnique(uint power)
    {
        Power = power;
    }

    public override void ActUpon(CombatEntity target)
    {
        target.TakeDamage(OwningAction.CalculateDamage());
    }

    public override string DescribeUsage(CombatEntity source, CombatEntity target)
    {
        throw new NotImplementedException();
    }
}

public class HealTechnique : Technique
{
    public HealTechnique(uint power)
    {
        Power = power;
    }

    public override void ActUpon(CombatEntity target)
    {
        target.Heal(Power);
    }

    public override string DescribeUsage(CombatEntity source, CombatEntity target)
    {
        return string.Format("{0} restores {1} health!", source.Name, Power);
    }
}

public class SupportTechnique : Technique
{
    public StatusModifier Effect { get; private set; }
    //public int TurnDuration { get; private set; }

    public override void ActUpon(CombatEntity target)
    {
        target.Modifiers.Add(Effect);
    }

    public override string DescribeUsage(CombatEntity source, CombatEntity target)
    {
        throw new NotImplementedException();
    }
}