using System;
using System.Reflection;

namespace Test
{
    class Program
    {
        static void Main()
        {
            try
            {
                var ass = Assembly.LoadFrom(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");

                var helpersType = ass.GetType("Hearthstone_Deck_Tracker.Hearthstone.CollectionHelpers");
                Console.WriteLine("Helpers Type: " + (helpersType != null ? helpersType.FullName : "null"));
                if (helpersType != null)
                {
                    foreach (var m in helpersType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        Console.WriteLine("  Helper Method: " + m.Name);
                    }
                    foreach (var p in helpersType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        Console.WriteLine("  Helper Prop: " + p.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
