using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
        var collectionHelpers = asm.GetType("Hearthstone_Deck_Tracker.Hearthstone.CollectionHelpers");
        Console.WriteLine("CollectionHelpers properties/fields:");
        foreach (var p in collectionHelpers.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            Console.WriteLine("Prop: " + p.Name + " -> " + p.PropertyType.FullName);
        }

        var hsCollProp = collectionHelpers.GetProperty("Hearthstone");
        if (hsCollProp != null)
        {
            var hsCollObj = hsCollProp.GetValue(null);
            Console.WriteLine("hsCollObj: " + hsCollObj);
            var getCollMethod = hsCollObj.GetType().GetMethod("GetCollection");
            Console.WriteLine("getCollMethod: " + getCollMethod);
        }
    }
}
