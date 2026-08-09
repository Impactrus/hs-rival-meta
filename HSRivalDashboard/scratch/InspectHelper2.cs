using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthstoneDeckTracker.exe");
        var helperType = asm.GetType("Hearthstone_Deck_Tracker.Hearthstone.CollectionHelpers");
        var hsHelperField = helperType.GetProperty("Hearthstone").GetValue(null);
        
        Console.WriteLine("hsHelperField type: " + hsHelperField.GetType().FullName);
        foreach (var m in hsHelperField.GetType().GetMethods())
        {
            Console.WriteLine("  Method: " + m.Name);
        }
        foreach (var e in hsHelperField.GetType().GetEvents())
        {
            Console.WriteLine("  Event: " + e.Name);
        }
    }
}
