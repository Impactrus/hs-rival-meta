using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
        var collType = asm.GetType("Hearthstone_Deck_Tracker.Hearthstone.Collection");
        Console.WriteLine("Collection properties:");
        foreach (var p in collType.GetProperties())
        {
            Console.WriteLine("  Prop: " + p.Name + " (" + p.PropertyType.FullName + ")");
        }
        foreach (var f in collType.GetFields())
        {
            Console.WriteLine("  Field: " + f.Name + " (" + f.FieldType.FullName + ")");
        }

        var hsNetOAuth = asm.GetType("Hearthstone_Deck_Tracker.HsReplay.HSReplayNetOAuth");
        Console.WriteLine("\nHSReplayNetOAuth methods:");
        foreach (var m in hsNetOAuth.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            if (m.Name.Contains("Collection"))
                Console.WriteLine("  Method: " + m.Name);
        }
    }
}
