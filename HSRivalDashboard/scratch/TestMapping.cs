using System;
using System.Collections.Generic;
using HearthDb;
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
                Console.WriteLine("Total raw mirror cards: " + fullColl.Cards.Count);
                int mappedCount = 0;
                int ownedCount = 0;
                var collectionMap = new Dictionary<int, int>();

                foreach (var card in fullColl.Cards)
                {
                    if (card == null || string.IsNullOrEmpty(card.Id)) continue;

                    int mappedDbfId = 0;
                    int dbfId = 0;
                    if (Cards.CardIdToDbfId.TryGetValue(card.Id, out mappedDbfId))
                    {
                        dbfId = mappedDbfId;
                    }
                    else
                    {
                        HearthDb.Card hearthDbCard;
                        if (Cards.All.TryGetValue(card.Id, out hearthDbCard) && hearthDbCard != null)
                        {
                            dbfId = hearthDbCard.DbfId;
                        }
                    }

                    if (dbfId > 0)
                    {
                        mappedCount++;
                        int qty = Math.Max(card.Count, card.PremiumType > 0 ? 1 : 0);
                        if (qty > 0)
                        {
                            ownedCount++;
                            if (collectionMap.ContainsKey(dbfId))
                                collectionMap[dbfId] = Math.Max(collectionMap[dbfId], qty);
                            else
                                collectionMap[dbfId] = qty;
                        }
                    }
                }

                Console.WriteLine("Mapped DBF cards count: " + mappedCount);
                Console.WriteLine("Owned DBF cards count: " + ownedCount);
                Console.WriteLine("Unique DBF IDs in collectionMap: " + collectionMap.Count);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
