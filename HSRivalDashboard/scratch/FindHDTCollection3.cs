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
                var prop = helpersType.GetProperty("Hearthstone", BindingFlags.Public | BindingFlags.Static);
                var hsCollHelper = prop.GetValue(null, null);
                Console.WriteLine("Hearthstone CollectionHelper Instance: " + (hsCollHelper != null ? hsCollHelper.GetType().FullName : "null"));

                if (hsCollHelper != null)
                {
                    var t = hsCollHelper.GetType();
                    foreach (var p in t.GetProperties())
                    {
                        Console.WriteLine("  Prop: " + p.Name + " = " + p.GetValue(hsCollHelper, null));
                    }
                    foreach (var m in t.GetMethods())
                    {
                        if (m.Name.Contains("Get") || m.Name.Contains("Collection"))
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
