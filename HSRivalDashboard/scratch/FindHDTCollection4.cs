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

                var tryGetMethod = hsCollHelper.GetType().GetMethod("GetCollection");
                Console.WriteLine("GetCollection Method: " + tryGetMethod);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
