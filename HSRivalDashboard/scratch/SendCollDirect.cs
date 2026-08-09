using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using HearthDb;
using HearthMirror;
using Newtonsoft.Json;

class Program
{
    static void Main()
    {
        try
        {
            var fullColl = Reflection.Client.GetFullCollection();
            if (fullColl != null && fullColl.Cards != null)
            {
                var collectionMap = new Dictionary<int, int>();
                int userDust = fullColl.Dust;

                foreach (var card in fullColl.Cards)
                {
                    if (card == null || string.IsNullOrEmpty(card.Id)) continue;

                    int mappedDbfId = 0;
                    int dbfId = 0;
                    if (Cards.CardIdToDbfId.TryGetValue(card.Id, out mappedDbfId))
                    {
                        dbfId = mappedDbfId;
                    }

                    if (dbfId > 0 && card.Count > 0)
                    {
                        if (collectionMap.ContainsKey(dbfId))
                            collectionMap[dbfId] += card.Count;
                        else
                            collectionMap[dbfId] = card.Count;
                    }
                }

                Console.WriteLine("Sending " + collectionMap.Count + " cards, dust: " + userDust);

                using (var client = new HttpClient())
                {
                    var payload = new { collection = collectionMap, dust = userDust, isFullSync = true };
                    string json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = client.PostAsync("https://hs-rival-meta.onrender.com/api/collection", content).Result;
                    string respStr = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine("Server Response: HTTP " + (int)response.StatusCode + " - " + respStr);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
