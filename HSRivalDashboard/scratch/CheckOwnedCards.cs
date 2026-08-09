using System;
using HearthMirror;

class Program
{
    static void Main()
    {
        try
        {
            var fullColl = Reflection.Client.GetFullCollection();
            if (fullColl != null && fullColl.Cards != null)
            {
                int totalCards = fullColl.Cards.Count;
                int countGreaterThanZero = 0;
                int premiumGreaterThanZero = 0;
                int trialGreaterThanZero = 0;
                int ownedCardsTotal = 0;

                foreach (var card in fullColl.Cards)
                {
                    if (card.Count > 0) countGreaterThanZero++;
                    if (card.PremiumType > 0) premiumGreaterThanZero++;
                    if (card.TrialCount > 0) trialGreaterThanZero++;
                    
                    int qty = Math.Max(card.Count, card.PremiumType > 0 ? 1 : 0);
                    if (qty > 0) ownedCardsTotal++;
                }

                Console.WriteLine("Total cards in RAM: " + totalCards);
                Console.WriteLine("Cards with Count > 0: " + countGreaterThanZero);
                Console.WriteLine("Cards with PremiumType > 0: " + premiumGreaterThanZero);
                Console.WriteLine("Cards with TrialCount > 0: " + trialGreaterThanZero);
                Console.WriteLine("Owned cards total (qty > 0): " + ownedCardsTotal);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
