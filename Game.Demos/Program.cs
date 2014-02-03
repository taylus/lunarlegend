using System;

public static class Program
{
    public static void Main(string[] args)
    {
        ////console-based launcher maybe for final release?
        ////has some problems: XNA window doesn't start focused, but IsActive is still true...
        //BaseGame demo = null;
        //do
        //{
        //    Console.WriteLine("Select a UI demo to run:");
        //    Console.WriteLine("1: Menus");
        //    Console.WriteLine("2: Power Bar");

        //    ConsoleKeyInfo input = Console.ReadKey();
        //    switch (input.KeyChar)
        //    {
        //        case '1':
        //            demo = new MenuDemo();
        //            break;
        //        case '2':
        //            demo = new MenuDemo();
        //            break;
        //        default:
        //            Console.WriteLine("Unrecognized selection: " + input.KeyChar);
        //            break;
        //    }
        //} while (demo == null);

        //using (demo)
        //{
        //    demo.Run();
        //}

        //just hardcode the demo you want to run for now, and build this project as a 
        //Windows Application instead of a Console Application, to hide the console window
        using (BaseGame demo = new BattleDemo())
        {
            demo.Run();
        }
    }
}

