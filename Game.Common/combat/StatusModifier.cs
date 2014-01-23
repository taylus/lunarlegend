using System;

//represents special buffs and effects that various parts of the game check for
//some of these can be granted by items or equipment
public enum StatusModifier
{
    Invulnerable,   //can't take damage or die
    Freecasting,    //techniques cost no resources
    Mute,           //can't use techniques (only magic?)
    Poisoned,       //taking damage every turn
    NoCrit,         //can't critical strike ("blind" debuff)
    SuperCrit       //always critical strike
}