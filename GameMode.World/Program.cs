using System;

public static class Program
{
    public static void Main(string[] args)
    {
        using (Overworld game = new Overworld())
        {
            game.Run();
        }
    }
}

