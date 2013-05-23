using System;

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
