﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//represents any special attack or spell
//different types of spells and abilities are subclasses (e.g. offensive, defensive, buff/debuff)
//variants/ranks of those spells and abilities are just differently-configured instances of those subclasses
//TODO: this would be a good candidate, along with specific monster types, to persist using SQLite
public abstract class Technique
{
    public CombatAction OwningAction { get; private set; }
    public uint ResourceCost { get; private set; }
    public uint Power { get; private set; }
    public string Name { get; private set; }

    public abstract uint ActUpon(CombatEntity target);
}

public class DamageTechnique : Technique
{
    public DamageType Type { get; private set; }

    public override uint ActUpon(CombatEntity target)
    {
        return target.TakeDamage(OwningAction.CalculateDamage());
    }
}

public class HealTechnique : Technique
{
    public override uint ActUpon(CombatEntity target)
    {
        return target.Heal(Power);
    }
}

public class SupportTechnique : Technique
{
    public int TurnDuration { get; private set; }

    public override uint ActUpon(CombatEntity target)
    {
        //TODO: act differently based on type and effect:
        //which stat, and whether it's a buff or debuff
        throw new NotImplementedException();
    }
}