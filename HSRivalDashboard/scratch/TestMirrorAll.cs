using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var mirrorAsm = Assembly.LoadFile(@"C:\Users\szymo\AppData\Local\HearthstoneDeckTracker\app-1.55.3\HearthMirror.dll");
            var reflectionType = mirrorAsm.GetType("HearthMirror.Reflection");
            var clientType = reflectionType != null ? reflectionType.GetNestedType("Client") : null;
            
            if (clientType != null)
            {
                Console.WriteLine("Testing HearthMirror methods...");

                var getFullColl = clientType.GetMethod("GetFullCollection");
                if (getFullColl != null)
                {
                    var full = getFullColl.Invoke(null, null);
                    Console.WriteLine("GetFullCollection() != null: " + (full != null));
                    if (full != null)
                    {
                        var cardsProp = full.GetType().GetProperty("Cards");
                        var cards = cardsProp != null ? cardsProp.GetValue(full, null) as System.Collections.IList : null;
                        Console.WriteLine("GetFullCollection.Cards count: " + (cards != null ? cards.Count : 0));
                    }
                }

                var getColl = clientType.GetMethod("GetCollection");
                if (getColl != null)
                {
                    var coll = getColl.Invoke(null, null) as System.Collections.IList;
                    Console.WriteLine("GetCollection() count: " + (coll != null ? coll.Count : 0));
                }

                var getDecks = clientType.GetMethod("GetDecks");
                if (getDecks != null)
                {
                    var decks = getDecks.Invoke(null, null) as System.Collections.IList;
                    Console.WriteLine("GetDecks() count: " + (decks != null ? decks.Count : 0));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
