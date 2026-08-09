using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HearthstoneDeckTracker
{
    public class CardInfo
    {
        [JsonPropertyName("dbfId")]
        public int DbfId { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("cost")]
        public int Cost { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("cardClass")]
        public string CardClass { get; set; }

        [JsonPropertyName("rarity")]
        public string Rarity { get; set; }
    }

    public class CardDatabase
    {
        private static readonly HttpClient client = new HttpClient();
        
        private readonly string cacheDir;
        private readonly string tilesDir;
        
        private Dictionary<int, CardInfo> cardsByDbfId = new Dictionary<int, CardInfo>();
        private Dictionary<string, CardInfo> cardsById = new Dictionary<string, CardInfo>();

        public string CurrentLocale { get; private set; } = "enUS";

        public CardDatabase()
        {
            cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
            tilesDir = Path.Combine(cacheDir, "tiles");
            
            Directory.CreateDirectory(cacheDir);
            Directory.CreateDirectory(tilesDir);
        }

        public async Task InitializeAsync(string locale = "enUS")
        {
            CurrentLocale = locale;
            string localPath = Path.Combine(cacheDir, $"cards.{locale}.json");
            
            if (!File.Exists(localPath))
            {
                string url = $"https://api.hearthstonejson.com/v1/latest/{locale}/cards.json";
                try
                {
                    string json = await client.GetStringAsync(url);
                    await File.WriteAllTextAsync(localPath, json);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to download card database for locale {locale}: {ex.Message}", ex);
                }
            }

            try
            {
                string jsonContent = await File.ReadAllTextAsync(localPath);
                var cardList = JsonSerializer.Deserialize<List<CardInfo>>(jsonContent);
                
                cardsByDbfId.Clear();
                cardsById.Clear();

                if (cardList != null)
                {
                    foreach (var card in cardList)
                    {
                        cardsByDbfId[card.DbfId] = card;
                        cardsById[card.Id] = card;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse card database: {ex.Message}", ex);
            }
        }

        public CardInfo GetCardByDbfId(int dbfId)
        {
            return cardsByDbfId.TryGetValue(dbfId, out var card) ? card : null;
        }

        public CardInfo GetCardById(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            return cardsById.TryGetValue(cardId, out var card) ? card : null;
        }

        public IEnumerable<CardInfo> GetAllCards()
        {
            return cardsByDbfId.Values;
        }

        public async Task<string> GetTilePathAsync(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            
            string localPath = Path.Combine(tilesDir, $"{cardId}.png");
            if (File.Exists(localPath))
            {
                return localPath;
            }

            string url = $"https://art.hearthstonejson.com/v1/tiles/{cardId}.png";
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                    return localPath;
                }
            }
            catch
            {
                // Fallback to null or standard image on download failure
            }

            return null;
        }

        public async Task<string> GetCardRenderPathAsync(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;

            string rendersDir = Path.Combine(cacheDir, "renders");
            Directory.CreateDirectory(rendersDir);

            string localPath = Path.Combine(rendersDir, $"512_{cardId}_{CurrentLocale}.png");
            if (File.Exists(localPath))
            {
                return localPath;
            }

            string url = $"https://art.hearthstonejson.com/v1/render/latest/{CurrentLocale}/512x/{cardId}.png";
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                    return localPath;
                }
            }
            catch
            {
                // Fallback to enUS if plPL fails or is unavailable
                try
                {
                    string fallbackUrl = $"https://art.hearthstonejson.com/v1/render/latest/enUS/512x/{cardId}.png";
                    var response = await client.GetAsync(fallbackUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        using (var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                        return localPath;
                    }
                }
                catch { }
            }

            return null;
        }

    }
}

