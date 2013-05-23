using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//represents any special attack or spell (offensive, defensive, or support)
//different types of spells and abilities are subclasses
//variants/ranks of those spells and abilities are just differently-configured instances of those subclasses
public abstract class Technique
{
    public uint ResourceCost { get; private set; }
    public uint Power { get; private set; }
    public string Name { get; private set; }

    public abstract void ActUpon(CombatEntity target);
}

public class DamageTechnique : Technique
{
    public DamageType Type { get; private set; }

    public override void ActUpon(CombatEntity target)
    {
        target.TakeDamage(Power, Type);
    }
}

public class HealTechnique : Technique
{
    public override void ActUpon(CombatEntity target)
    {
        target.Heal(Power);
    }
}

public class SupportTechnique : Technique
{
    public int TurnDuration { get; private set; }

    public override void ActUpon(CombatEntity target)
    {
        //TODO: act differently based on type and effect:
        //which stat, and whether it's a buff or debuff
        throw new NotImplementedException();
    }
}