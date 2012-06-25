using System;

public static class Program
{
    public static void Main(string[] args)
    {
        using (TiledXNA game = new TiledXNA())
        {
            game.Run();
        }
    }
}

