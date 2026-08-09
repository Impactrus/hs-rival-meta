using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

        private static HttpListener _localListener;
        private static bool _isRunning = false;

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
            _isRunning = true;

            // Subscribe to HDT events
            GameEvents.OnGameEnd.Add(OnGameEnd);
            GameEvents.OnInMenu.Add(OnInMenu);
            GameEvents.OnModeChanged.Add(OnModeChanged);

            // Start local HTTP listener for zero-click web app pairing
            StartLocalHttpListener();

            // Start background loop for auto-syncing collection whenever available
            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        if (Config != null && Config.AutoSyncCollection)
                        {
                            await SyncCollectionAsync();
                        }
                    }
                    catch { }
                    await Task.Delay(10000); // Check/sync every 10 seconds
                }
            });
        }

        public void OnUnload()
        {
            _isRunning = false;
            try
            {
                if (_localListener != null && _localListener.IsListening)
                {
                    _localListener.Stop();
                    _localListener.Close();
                }
            }
            catch { }
        }

        public void OnButtonPress()
        {
            var window = new SettingsWindow(Config);
            window.ShowDialog();
        }

        public void OnUpdate()
        {
        }

        private static void StartLocalHttpListener()
        {
            try
            {
                _localListener = new HttpListener();
                _localListener.Prefixes.Add("http://127.0.0.1:48854/");
                _localListener.Start();

                Task.Run(async () =>
                {
                    while (_localListener != null && _localListener.IsListening)
                    {
                        try
                        {
                            var ctx = await _localListener.GetContextAsync();
                            var req = ctx.Request;
                            var res = ctx.Response;

                            // Add CORS headers so web app can communicate with HDT plugin locally
                            res.Headers.Add("Access-Control-Allow-Origin", "*");
                            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                            if (req.HttpMethod == "OPTIONS")
                            {
                                res.StatusCode = 200;
                                res.Close();
                                continue;
                            }

                            if (req.Url.AbsolutePath == "/ping")
                            {
                                string responseString = JsonConvert.SerializeObject(new
                                {
                                    status = "ok",
                                    hasToken = !string.IsNullOrWhiteSpace(Config?.UserToken),
                                    userToken = Config?.UserToken ?? ""
                                });
                                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                                res.ContentType = "application/json";
                                res.ContentLength64 = buffer.Length;
                                await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                            else if (req.Url.AbsolutePath == "/token" && (req.HttpMethod == "POST" || req.HttpMethod == "GET"))
                            {
                                string newToken = req.QueryString["token"];
                                if (string.IsNullOrWhiteSpace(newToken) && req.HasEntityBody)
                                {
                                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                                    {
                                        string body = await reader.ReadToEndAsync();
                                        try
                                        {
                                            var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                                            if (payload != null && payload.ContainsKey("token"))
                                                newToken = payload["token"];
                                        }
                                        catch { }
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(newToken))
                                {
                                    Config.UserToken = newToken.Trim();
                                    Config.Save();
                                    Task.Run(async () => await SyncCollectionAsync());
                                }

                                string responseString = JsonConvert.SerializeObject(new
                                {
                                    success = true,
                                    userToken = Config.UserToken
                                });
                                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                                res.ContentType = "application/json";
                                res.ContentLength64 = buffer.Length;
                                await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                            else
                            {
                                res.StatusCode = 404;
                            }

                            res.Close();
                        }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HS Rival Plugin] Failed to start local listener: {ex.Message}");
            }
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
                List<HearthMirror.Objects.Card> mirrorCards = null;
                int userDust = 0;

                // 1. Try FullCollection via Reflection.Client
                try
                {
                    var fullColl = Reflection.Client.GetFullCollection();
                    if (fullColl != null)
                    {
                        mirrorCards = fullColl.Cards;
                        userDust = fullColl.Dust;
                    }
                }
                catch { }

                // 2. Try GetCollection via Reflection.Client fallback
                if (mirrorCards == null || mirrorCards.Count == 0)
                {
                    try
                    {
                        mirrorCards = Reflection.Client.GetCollection();
                    }
                    catch { }
                }

                var collectionMap = new Dictionary<int, int>();

                // Parse mirror cards if available
                if (mirrorCards != null && mirrorCards.Count > 0)
                {
                    foreach (var card in mirrorCards)
                    {
                        if (card == null || string.IsNullOrEmpty(card.Id)) continue;

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
                }

                if (collectionMap.Count == 0)
                {
                    return "Wykryto brak kolekcji w pamięci. Wejdź do zakładki 'Moja Kolekcja' w Hearthstone.";
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
                    var payload = new { collection = collectionMap, dust = userDust, isFullSync = true };
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
                return $"Błąd synchronizacji: {ex.Message}";
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
