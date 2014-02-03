using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//represents any special attack or spell
//different types of spells and abilities are subclasses (e.g. offensive, defensive, buff/debuff)
//variations and ranks of those spells and abilities are just differently-configured instances of these subclasses
//TODO: this would be a good candidate, along with specific monster types, to persist using SQLite
//until that is implemented, implement a fake data access layer using static "factory" type classes
public abstract class Technique
{
    public uint ResourceCost { get; protected set; }
    public string Name { get; private set; }
    public bool MultiTarget { get; private set; }   //TODO: techniques that target all enemies
    public List<PowerMeterPattern> PowerMeterPatterns { get; protected set; }

    public Technique(string name, List<PowerMeterPattern> powerMeterPatterns = null)
    {
        Name = name;

        if (powerMeterPatterns != null)
        {
            PowerMeterPatterns = powerMeterPatterns;
        }
        else
        {
            //default to a generic unmissable pattern with crits in the center
            PowerMeterPatterns = new List<PowerMeterPattern>();
            PowerMeterPatterns.Add(new PowerMeterPattern("====XX====", 5.0f));
        }
    }

    public override string ToString()
    {
        return Name;
    }
}

public class DamageTechnique : Technique
{
    public DamageType Type { get; private set; }
    public uint Power { get; protected set; }

    public DamageTechnique(string name, uint power, DamageType type = DamageType.Physical, List<PowerMeterPattern> powerMeterPatterns = null)
        : base(name, powerMeterPatterns)
    {
        Power = power;
        Type = type;
    }
}

public class HealTechnique : Technique
{
    public uint Power { get; protected set; }

    public HealTechnique(string name, uint power) : base(name)
    {
        Power = power;
    }
}

public class SupportTechnique : Technique
{
    public StatusModifier Effect { get; private set; }
    //public int TurnDuration { get; private set; }

    public SupportTechnique(string name) : base(name)
    {

    }

    //public override void ActUpon(CombatEntity target)
    //{
    //    target.Modifiers.Add(Effect);
    //}
}

//TODO: instead of hardcoding, load technique and power meter patterns from a persistent store (SQLite)
public static class Techniques
{
    public static Technique LoadByName(string name)
    {
        List<PowerMeterPattern> patterns = new List<PowerMeterPattern>();
        switch (name)
        {
            case "Fireball":
                patterns.Add(new PowerMeterPattern("========XX", 5.0f));
                return new DamageTechnique(name, 10, DamageType.Fire, patterns);
            case "Firestorm":
                patterns.Add(new PowerMeterPattern("==-=X=X=-==", 5.0f));
                patterns.Add(new PowerMeterPattern("=---=X=---=", 7.0f));
                return new DamageTechnique(name, 20, DamageType.Fire, patterns);
            case "Hellfire":
                patterns.Add(new PowerMeterPattern("X===XX===X", 5.0f));
                patterns.Add(new PowerMeterPattern("X=--====--=X", 7.0f));
                patterns.Add(new PowerMeterPattern("X=-----==-----=X", 8.0f));
                return new DamageTechnique(name, 30, DamageType.Fire, patterns);
            default:
                throw new ArgumentException("Technique \"" + name + "\" not found.");
        }
    }
}