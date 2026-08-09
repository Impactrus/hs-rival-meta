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
                var collType = ass.GetType("Hearthstone_Deck_Tracker.Hearthstone.Collection");
                Console.WriteLine("Collection Type: " + (collType != null ? collType.FullName : "null"));
                if (collType != null)
                {
                    foreach (var p in collType.GetProperties())
                    {
                        Console.WriteLine("  Prop: " + p.Name + " Type: " + p.PropertyType);
                    }
                    foreach (var m in collType.GetMethods())
                    {
                        if (m.Name.Contains("Count") || m.Name.Contains("Card"))
                        {
                            Console.WriteLine("  Method: " + m.Name);
                        }
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
