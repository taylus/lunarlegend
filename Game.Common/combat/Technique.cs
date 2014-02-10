using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

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

    public Technique(string name, uint cost = 0, List<PowerMeterPattern> powerMeterPatterns = null)
    {
        Name = name;
        ResourceCost = cost;

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

    public virtual Color GetColor()
    {
        return Color.White;
    }
}

public class DamageTechnique : Technique
{
    public DamageType Type { get; private set; }
    public uint Power { get; protected set; }

    public DamageTechnique(string name, uint power, DamageType type, uint cost, List<PowerMeterPattern> powerMeterPatterns = null)
        : base(name, cost, powerMeterPatterns)
    {
        Power = power;
        Type = type;
    }

    //color representing this technique's DamageType
    //used for screen flashes when using this technique
    public override Color GetColor()
    {
        switch (Type)
        {
            case DamageType.Astral:
                return Color.MediumPurple;
            case DamageType.Earth:
                return Color.SaddleBrown;
            case DamageType.Fire:
                return Color.Red;
            case DamageType.Physical:
                return Color.Gray;
            case DamageType.Shadow:
                return Color.Black;
            case DamageType.Water:
                return Color.Blue;
            case DamageType.Wind:
                return Color.Yellow;
            default:
                return Color.PapayaWhip; //always wanted to use this color
        }
    }
}

public class HealTechnique : Technique
{
    public uint Power { get; protected set; }

    public HealTechnique(string name, uint power, uint cost) : base(name, cost)
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
                return new DamageTechnique(name, 10, DamageType.Fire, 2, patterns);
            case "Firestorm":
                patterns.Add(new PowerMeterPattern("==-=X=X=-==", 5.0f));
                patterns.Add(new PowerMeterPattern("=---=X=---=", 7.0f));
                return new DamageTechnique(name, 20, DamageType.Fire, 5, patterns);
            case "Hellfire":
                patterns.Add(new PowerMeterPattern("X===XX===X", 5.0f));
                patterns.Add(new PowerMeterPattern("X=--====--=X", 7.0f));
                patterns.Add(new PowerMeterPattern("X=-----==-----=X", 8.0f));
                return new DamageTechnique(name, 30, DamageType.Fire, 10, patterns);
            case "Frostbite":
                return new DamageTechnique("Frostbite", 10, DamageType.Water, 2);
            case "Spark":
                return new DamageTechnique("Spark", 10, DamageType.Wind, 2);
            case "Thunderstorm":
                return new DamageTechnique("Thunderstorm", 20, DamageType.Wind, 5);
            case "Boulder":
                return new DamageTechnique("Boulder", 10, DamageType.Earth, 2);
            case "Earthquake":
                return new DamageTechnique("Earthquake", 20, DamageType.Earth, 5);
            case "Astral Flare":
                return new DamageTechnique("Astral Flare", 50, DamageType.Astral, 10);
            case "Shadowburn":
                return new DamageTechnique("Shadowburn", 50, DamageType.Shadow, 10);
            case "Heal":
                return new HealTechnique("Heal", 20, 5);
            default:
                throw new ArgumentException("Technique \"" + name + "\" not found.");
        }
    }
}