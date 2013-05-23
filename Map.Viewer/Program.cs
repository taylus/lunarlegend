using System;

public static class Program
{
    public static void Main(string[] args)
    {
        using (TiledMapViewer game = new TiledMapViewer())
        {
            game.Run();
        }
    }
}

