using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var hdtAsm = Assembly.LoadFile(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
            var collType = hdtAsm.GetType("Hearthstone_Deck_Tracker.Hearthstone.Collection");
            Console.WriteLine("Collection Type: " + (collType != null ? collType.FullName : "null"));
            if (collType != null)
            {
                foreach (var prop in collType.GetProperties())
                {
                    Console.WriteLine(" Property: " + prop.Name + " : " + prop.PropertyType.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
