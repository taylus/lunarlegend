using System;

//a stub program to demo the combat system
public static class Program
{
    public static void Main(string[] args)
    {
        using (BattleDemo game = new BattleDemo())
        {
            game.Run();
        }
    }
}
