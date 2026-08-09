using System;
using System.Reflection;
using Hearthstone_Deck_Tracker.Plugins;

class Program
{
    static void Main()
    {
        try
        {
            var file = @"C:\Users\szymo\AppData\Roaming\HearthstoneDeckTracker\Plugins\HSRivalPlugin.dll";
            var asm = Assembly.LoadFrom(file);
            foreach (var type in asm.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    Console.WriteLine("Found Plugin: " + type.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
