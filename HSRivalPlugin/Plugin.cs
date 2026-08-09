using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using HearthDb;
using HearthMirror;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums.Hearthstone;
using Hearthstone_Deck_Tracker.Plugins;
using Newtonsoft.Json;
using CoreAPI = Hearthstone_Deck_Tracker.API.Core;

namespace HSRivalPlugin
{
    public class Plugin : IPlugin
    {
        public static PluginConfig Config { get; private set; }

        public string Name => "HS Rival Meta Sync";
        public string Description => "Automatyczna synchronizacja kolekcji kart oraz historii meczów z portalem HS Rival Meta.";
        public string Author => "HS Rival Team";
        public string ButtonText => "Settings";
        public Version Version => new Version(1, 0, 0);

        private MenuItem _menuItem;
        public MenuItem MenuItem
        {
            get
            {
                if (_menuItem == null)
                {
                    _menuItem = new MenuItem { Header = "⚔️ HS Rival Meta Settings" };
                    _menuItem.Click += (s, e) => OnButtonPress();
                }
                return _menuItem;
            }
        }

        public void OnLoad()
        {
            Config = PluginConfig.Load();

            // Subscribe to HDT events
            GameEvents.OnGameEnd.Add(OnGameEnd);
            GameEvents.OnInMenu.Add(OnInMenu);
            GameEvents.OnModeChanged.Add(OnModeChanged);

            // Trigger collection sync when plugin loads
            Task.Run(async () =>
            {
                await Task.Delay(3000); // Give HearthMirror a moment to initialize
                await SyncCollectionAsync();
            });
        }

        public void OnUnload()
        {
            // Unsubscribe from events
        }

        public void OnButtonPress()
        {
            var window = new SettingsWindow(Config);
            window.ShowDialog();
        }

        public void OnUpdate()
        {
        }

        private static void OnInMenu()
        {
            if (Config != null && Config.AutoSyncCollection)
            {
                Task.Run(async () => await SyncCollectionAsync());
            }
        }

        private static void OnModeChanged(Mode mode)
        {
            if (Config != null && Config.AutoSyncCollection)
            {
                Task.Run(async () => await SyncCollectionAsync());
            }
        }

        private static void OnGameEnd()
        {
            if (Config == null || !Config.AutoSyncMatches) return;

            try
            {
                var gameStats = CoreAPI.Game.CurrentGameStats;
                string playerClass = CoreAPI.Game.Player?.CurrentClass ?? CoreAPI.Game.Player?.OriginalClass ?? gameStats?.PlayerHero ?? "Unknown";
                string opponentClass = CoreAPI.Game.Opponent?.CurrentClass ?? CoreAPI.Game.Opponent?.OriginalClass ?? gameStats?.OpponentHero ?? "Unknown";
                string result = gameStats?.Result.ToString() ?? "Unknown";
                string format = gameStats?.Format?.ToString() ?? "Standard";
                string deckName = gameStats?.DeckName ?? DeckList.Instance.ActiveDeck?.Name ?? "";

                Task.Run(async () =>
                {
                    await SendMatchResultAsync(playerClass, opponentClass, result, format, deckName);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HS Rival Plugin Error] OnGameEnd: {ex.Message}");
            }
        }

        public static async Task<string> SyncCollectionAsync()
        {
            if (Config == null) Config = PluginConfig.Load();

            try
            {
                // Retrieve collection via HearthMirror Client
                var mirrorCollection = Reflection.Client.GetCollection();
                if (mirrorCollection == null || mirrorCollection.Count == 0)
                {
                    return "Wykryto brak kolekcji w pamięci (otwórz zakładkę Kolekcja w Hearthstone).";
                }

                var collectionMap = new Dictionary<int, int>();
                foreach (var card in mirrorCollection)
                {
                    if (card == null || string.IsNullOrEmpty(card.Id)) continue;

                    // Convert card ID (e.g. EX1_012) to dbfId
                    int dbfId = 0;
                    if (Cards.CardIdToDbfId.TryGetValue(card.Id, out int mappedDbfId))
                    {
                        dbfId = mappedDbfId;
                    }
                    else if (Cards.All.TryGetValue(card.Id, out var hearthDbCard))
                    {
                        dbfId = hearthDbCard.DbfId;
                    }

                    if (dbfId > 0)
                    {
                        int qty = Math.Max(card.Count, card.PremiumType > 0 ? 1 : 0);
                        if (qty > 0)
                        {
                            if (collectionMap.ContainsKey(dbfId))
                                collectionMap[dbfId] = Math.Max(collectionMap[dbfId], qty);
                            else
                                collectionMap[dbfId] = qty;
                        }
                    }
                }

                // Add free Core Set cards automatically
                foreach (var hearthDbCard in Cards.Collectible.Values)
                {
                    if (hearthDbCard.Rarity == HearthDb.Enums.Rarity.FREE || (hearthDbCard.Id != null && hearthDbCard.Id.StartsWith("CORE_")))
                    {
                        int maxQty = hearthDbCard.Rarity == HearthDb.Enums.Rarity.LEGENDARY ? 1 : 2;
                        if (!collectionMap.ContainsKey(hearthDbCard.DbfId))
                            collectionMap[hearthDbCard.DbfId] = maxQty;
                    }
                }

                // Send to server
                using (var client = new HttpClient())
                {
                    var payload = new { collection = collectionMap, isFullSync = true };
                    string json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    string serverUrl = string.IsNullOrWhiteSpace(Config.ServerUrl)
                        ? "https://hs-rival-meta.onrender.com"
                        : Config.ServerUrl.TrimEnd('/');

                    if (!string.IsNullOrWhiteSpace(Config.UserToken))
                    {
                        client.DefaultRequestHeaders.Add("X-User-Token", Config.UserToken);
                    }

                    var response = await client.PostAsync($"{serverUrl}/api/collection", content);
                    if (response.IsSuccessStatusCode)
                    {
                        return $"✓ Zsynchronizowano {collectionMap.Count} kart z portalem HS Rival Meta!";
                    }
                    else
                    {
                        return $"Błąd serwera (HTTP {(int)response.StatusCode})";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Błąd skanowania pamięci: {ex.Message}";
            }
        }

        private static async Task SendMatchResultAsync(string playerClass, string opponentClass, string result, string format, string deckName)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var payload = new
                    {
                        player_class = playerClass,
                        opponent_class = opponentClass,
                        result = result,
                        format = format,
                        deck_name = deckName,
                        date = DateTime.UtcNow.ToString("o")
                    };

                    string json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    string serverUrl = string.IsNullOrWhiteSpace(Config.ServerUrl)
                        ? "https://hs-rival-meta.onrender.com"
                        : Config.ServerUrl.TrimEnd('/');

                    if (!string.IsNullOrWhiteSpace(Config.UserToken))
                    {
                        client.DefaultRequestHeaders.Add("X-User-Token", Config.UserToken);
                    }

                    await client.PostAsync($"{serverUrl}/api/matches", content);
                }
            }
            catch { }
        }
    }
}
