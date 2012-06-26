using System;

public static class Program
{
    public static void Main(string[] args)
    {
        using (TiledDemoGame game = new TiledDemoGame())
        {
            game.Run();
        }
    }
}

