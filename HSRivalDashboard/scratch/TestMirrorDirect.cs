using System;
using HearthMirror;

class Program
{
    static void Main()
    {
        try
        {
            Console.WriteLine("Testing HearthMirror directly...");
            
            var fullColl = Reflection.Client.GetFullCollection();
            Console.WriteLine("GetFullCollection() != null: " + (fullColl != null));
            if (fullColl != null)
            {
                Console.WriteLine("GetFullCollection.Cards count: " + (fullColl.Cards != null ? fullColl.Cards.Count : 0));
                Console.WriteLine("GetFullCollection.Dust: " + fullColl.Dust);
            }

            var coll = Reflection.Client.GetCollection();
            Console.WriteLine("GetCollection() count: " + (coll != null ? coll.Count : 0));

            var decks = Reflection.Client.GetDecks();
            Console.WriteLine("GetDecks() count: " + (decks != null ? decks.Count : 0));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
