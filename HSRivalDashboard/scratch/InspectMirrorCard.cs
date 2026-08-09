using System;
using System.Reflection;
using HearthMirror;

class Program
{
    static void Main()
    {
        try
        {
            var fullColl = Reflection.Client.GetFullCollection();
            if (fullColl != null && fullColl.Cards != null && fullColl.Cards.Count > 0)
            {
                var firstCard = fullColl.Cards[0];
                Console.WriteLine("First card type: " + firstCard.GetType().FullName);
                foreach (var prop in firstCard.GetType().GetProperties())
                {
                    Console.WriteLine(" Property: " + prop.Name + " = " + prop.GetValue(firstCard, null));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
