using System;

public static class Program
{
    public static void Main(string[] args)
    {
        using (WorldDemo game = new WorldDemo())
        {
            game.Run();
        }
    }
}

