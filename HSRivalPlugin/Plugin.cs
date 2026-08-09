using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using HearthDb;
using HearthMirror;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone;
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

        private static TcpListener _tcpListener;
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

            // Subscribe to HDT game events
            GameEvents.OnGameEnd.Add(OnGameEnd);
            GameEvents.OnInMenu.Add(OnInMenu);
            GameEvents.OnModeChanged.Add(OnModeChanged);

            // Subscribe to HDT's internal collection changed event
            try
            {
                if (CollectionHelpers.Hearthstone != null)
                {
                    CollectionHelpers.Hearthstone.OnCollectionChanged += () =>
                    {
                        Task.Run(async () => await SyncCollectionAsync());
                    };
                }
            }
            catch { }

            // Start local HTTP server using TcpListener (bypasses Windows HttpListener admin restrictions)
            StartLocalHttpServer();

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
                    await Task.Delay(8000); // Sync check every 8 seconds
                }
            });
        }

        public void OnUnload()
        {
            _isRunning = false;
            try
            {
                if (_tcpListener != null)
                {
                    _tcpListener.Stop();
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

        private static void StartLocalHttpServer()
        {
            try
            {
                _tcpListener = new TcpListener(IPAddress.Loopback, 48854);
                _tcpListener.Start();

                Task.Run(async () =>
                {
                    while (_isRunning && _tcpListener != null)
                    {
                        try
                        {
                            var client = await _tcpListener.AcceptTcpClientAsync();
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using (client)
                                    using (var stream = client.GetStream())
                                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                                    {
                                        string requestLine = await reader.ReadLineAsync();
                                        if (string.IsNullOrEmpty(requestLine)) return;

                                        int contentLength = 0;
                                        string line;
                                        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                                        {
                                            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                                            {
                                                int.TryParse(line.Substring(15).Trim(), out contentLength);
                                            }
                                        }

                                        string body = "";
                                        if (contentLength > 0)
                                        {
                                            char[] buffer = new char[contentLength];
                                            int readTotal = 0;
                                            while (readTotal < contentLength)
                                            {
                                                int read = await reader.ReadAsync(buffer, readTotal, contentLength - readTotal);
                                                if (read <= 0) break;
                                                readTotal += read;
                                            }
                                            body = new string(buffer, 0, readTotal);
                                        }

                                        string[] parts = requestLine.Split(' ');
                                        string method = parts.Length > 0 ? parts[0] : "GET";
                                        string path = parts.Length > 1 ? parts[1] : "/";

                                        string responseBody = "";
                                        int statusCode = 200;

                                        if (method == "OPTIONS")
                                        {
                                            responseBody = "";
                                        }
                                        else if (path.StartsWith("/ping"))
                                        {
                                            responseBody = JsonConvert.SerializeObject(new
                                            {
                                                status = "ok",
                                                hasToken = !string.IsNullOrWhiteSpace(Config?.UserToken),
                                                userToken = Config?.UserToken ?? ""
                                            });
                                        }
                                        else if (path.StartsWith("/token"))
                                        {
                                            string newToken = null;
                                            if (!string.IsNullOrEmpty(body))
                                            {
                                                try
                                                {
                                                    var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                                                    if (payload != null && payload.ContainsKey("token"))
                                                        newToken = payload["token"];
                                                }
                                                catch { }
                                            }
                                            if (string.IsNullOrEmpty(newToken) && path.Contains("token="))
                                            {
                                                int idx = path.IndexOf("token=");
                                                newToken = path.Substring(idx + 6);
                                            }

                                            if (!string.IsNullOrWhiteSpace(newToken))
                                            {
                                                Config.UserToken = newToken.Trim();
                                                Config.Save();
                                                Task.Run(async () => await SyncCollectionAsync());
                                            }

                                            responseBody = JsonConvert.SerializeObject(new
                                            {
                                                success = true,
                                                userToken = Config?.UserToken ?? ""
                                            });
                                        }
                                        else
                                        {
                                            statusCode = 404;
                                            responseBody = "{\"error\":\"Not Found\"}";
                                        }

                                        byte[] bodyBytes = Encoding.UTF8.GetBytes(responseBody);
                                        string header = $"HTTP/1.1 {statusCode} OK\r\n" +
                                                        "Access-Control-Allow-Origin: *\r\n" +
                                                        "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                                                        "Access-Control-Allow-Headers: Content-Type\r\n" +
                                                        "Content-Type: application/json; charset=utf-8\r\n" +
                                                        $"Content-Length: {bodyBytes.Length}\r\n" +
                                                        "Connection: close\r\n\r\n";

                                        byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                                        await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                                        if (bodyBytes.Length > 0)
                                        {
                                            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                                        }
                                        await stream.FlushAsync();
                                    }
                                }
                                catch { }
                            });
                        }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HS Rival Plugin] TcpListener Error: {ex.Message}");
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
                var collectionMap = new Dictionary<int, int>();
                int userDust = 0;

                // 1. Primary Method: HDT's official internal CollectionHelpers.Hearthstone
                try
                {
                    if (CollectionHelpers.Hearthstone != null)
                    {
                        var task = CollectionHelpers.Hearthstone.GetCollection();
                        if (task != null)
                        {
                            var hdtColl = await task;
                            if (hdtColl != null)
                            {
                                if (hdtColl.Dust > 0) userDust = hdtColl.Dust;
                                if (hdtColl.Cards != null && hdtColl.Cards.Count > 0)
                                {
                                    foreach (var kvp in hdtColl.Cards)
                                    {
                                        int dbfId = kvp.Key;
                                        int[] counts = kvp.Value;
                                        if (dbfId > 0 && counts != null && counts.Length > 0)
                                        {
                                            int normalCount = counts.Length > 0 ? counts[0] : 0;
                                            int goldenCount = counts.Length > 1 ? counts[1] : 0;
                                            int totalQty = Math.Max(normalCount, 0) + Math.Max(goldenCount, 0);
                                            if (totalQty > 0)
                                            {
                                                collectionMap[dbfId] = totalQty;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[HSRivalPlugin] CollectionHelpers error: " + ex.Message);
                }

                // 2. Fallback Method: Direct HearthMirror RAM reading
                if (collectionMap.Count == 0)
                {
                    List<HearthMirror.Objects.Card> mirrorCards = null;
                    try
                    {
                        var fullColl = Reflection.Client.GetFullCollection();
                        if (fullColl != null)
                        {
                            mirrorCards = fullColl.Cards;
                            if (fullColl.Dust > 0) userDust = fullColl.Dust;
                        }
                    }
                    catch { }

                    if (mirrorCards == null || mirrorCards.Count == 0)
                    {
                        try { mirrorCards = Reflection.Client.GetCollection(); } catch { }
                    }

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
                }

                if (collectionMap.Count == 0)
                {
                    return "Wykryto brak kolekcji. Otwórz 'Moja Kolekcja' w Hearthstone.";
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
                        return $"✓ Zsynchronizowano {collectionMap.Count} kart i {userDust} pyłu!";
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
