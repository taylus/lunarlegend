using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//represents anything that can participate in a battle and its statistics
public abstract class CombatEntity
{
    public Measure Health;
    public Measure Resource;
    public bool Alive { get { return Health.Current > 0; } }
    public string Name { get; set; }
    public int Speed { get; set; }

    public Dictionary<DamageType, CombatRating> CombatRatings;

    public CombatEntity() : this(100, 20)
    {

    }

    public abstract CombatAction DecideAction(List<CombatEntity> allies, List<CombatEntity> enemies);

    public CombatEntity(uint hp, uint resource)
    {
        Health.Current = Health.Maximum = hp;
        Resource.Current = Resource.Maximum = resource;

        foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
        {
            if (type == DamageType.None) continue;
            CombatRatings.Add(type, new CombatRating());
        }
    }

    public void Attack(CombatEntity target)
    {
        target.TakeDamage(CombatRatings[DamageType.Physical].Attack, DamageType.Physical);
    }

    public void Attack(CombatEntity target, Technique tech)
    {
        if (tech != null && tech.ResourceCost <= Resource.Current)
        {
            tech.ActUpon(target);
            Resource.Current -= tech.ResourceCost;
        }
    }

    public void TakeDamage(uint damage, DamageType type)
    {
        damage -= CombatRatings[type].Defense;
        if (damage > 0) Health.Current -= damage;
    }

    public void Heal(uint health)
    {
        Health.Current += health;
    }
}

public class Enemy : CombatEntity
{
    public override CombatAction DecideAction(List<CombatEntity> allies, List<CombatEntity> enemies)
    {
        //AI to select what to do...
        //return new CombatAction(this, enemies.SelectRandom());
        throw new NotImplementedException();
    }
}

public class Player : CombatEntity
{
    public override CombatAction DecideAction(List<CombatEntity> allies, List<CombatEntity> enemies)
    {
        //select action via menus...
        throw new NotImplementedException();
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
}

public enum DamageType
{
    None,
    Physical,
    Earth,
    Fire,
    Wind,
    Water,
    Holy,
    Shadow
}

//TODO: different player classes have different types of resources, with different behavior
//      Energy = melee/warrior types: start at zero, gain more by using normal attack
//      Mana = caster/healer types: start at full, healers can regen it with skills, casters can't
//public enum ResourceType
//{
//    None,
//    Energy,
//    Mana
//}