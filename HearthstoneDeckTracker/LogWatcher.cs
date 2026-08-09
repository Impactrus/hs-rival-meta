using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace HearthstoneDeckTracker
{
    public class CardTransitionEventArgs : EventArgs
    {
        public string CardId { get; set; }
        public string Name { get; set; }
        public string EntityId { get; set; }
        public string FromZone { get; set; }
        public string ToZone { get; set; }
        public bool IsFriendly { get; set; }
    }

    public class DeckDetectedEventArgs : EventArgs
    {
        // The raw deckstring (AAECAaIH...) detected from Decks.log
        public string Deckstring { get; set; }
        public string DeckName { get; set; }
    }

    public class LogWatcher
    {
        private readonly string logConfigPath;
        private string hsLogPath;
        private string baseHearthstonePath;
        private CancellationTokenSource cts;
        private long lastFileSize;
        private long zoneLastFileSize;
        private string currentZoneLogPath;
        private int localPlayerId = -1;
        private CardDatabase cardDb;
        private readonly Dictionary<string, string> entityToCardId = new Dictionary<string, string>();
        private readonly Dictionary<string, string> entityToName = new Dictionary<string, string>();
        // Dedup: track current zone for each entity ID to avoid processing duplicate log lines
        private readonly Dictionary<string, string> entityZones = new Dictionary<string, string>();

        public event EventHandler OnGameStart;
        public event EventHandler OnGameEnd;
        public event EventHandler<CardTransitionEventArgs> OnCardTransition;
        public event EventHandler<string> OnStatusMessage;
        public event EventHandler<DeckDetectedEventArgs> OnDeckDetected;
        public event EventHandler<Dictionary<int, int>> OnCollectionDetected;

        private long decksLastFileSize = 0;
        private string currentDecksLogPath = null;
        private long collectionLastFileSize = 0;

        public bool IsRunning => cts != null;

        public LogWatcher(CardDatabase database)
        {
            cardDb = database;
            logConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Blizzard\Hearthstone\log.config"
            );
        }

        public string SetupLogConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(logConfigPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string configContent = "[Power]\nLogLevel=1\nFilePrinting=true\nConsolePrinting=false\nScreenPrinting=false\n\n[Zone]\nLogLevel=1\nFilePrinting=true\nConsolePrinting=false\nScreenPrinting=false\n\n[Decks]\nLogLevel=1\nFilePrinting=true\nConsolePrinting=false\nScreenPrinting=false\n\n[CollectionManager]\nLogLevel=1\nFilePrinting=true\nConsolePrinting=false\nScreenPrinting=false\n\n[Net]\nLogLevel=1\nFilePrinting=true\nConsolePrinting=false\nScreenPrinting=false\n";
                // Only write if it doesn't exist or is different to preserve other settings
                if (!File.Exists(logConfigPath) || File.ReadAllText(logConfigPath) != configContent)
                {
                    File.WriteAllText(logConfigPath, configContent);
                    OnStatusMessage?.Invoke(this, "Zaktualizowano plik log.config. Zrestartuj Hearthstone jeśli był włączony!");
                }
                else
                {
                    OnStatusMessage?.Invoke(this, "Plik log.config jest prawidłowo skonfigurowany.");
                }
            }
            catch (Exception ex)
            {
                OnStatusMessage?.Invoke(this, $"Błąd podczas konfiguracji log.config: {ex.Message}");
            }
            return logConfigPath;
        }

        public string LocateHearthstonePath()
        {
            // 0. Active process takes highest priority!
            try
            {
                var hsProc = System.Diagnostics.Process.GetProcessesByName("Hearthstone").FirstOrDefault();
                if (hsProc != null)
                {
                    string procDir = Path.GetDirectoryName(hsProc.MainModule?.FileName);
                    if (!string.IsNullOrEmpty(procDir) && Directory.Exists(procDir))
                        return procDir;
                }
            }
            catch { }

            // 1. Try registry
            string registryPath = GetPathFromRegistry();
            if (!string.IsNullOrEmpty(registryPath) && Directory.Exists(registryPath))
            {
                return registryPath;
            }

            // 2. Try Battle.net directory search
            try
            {
                string bnetDir = @"C:\Program Files (x86)\Battle.net";
                if (Directory.Exists(bnetDir))
                {
                    var hsDirs = Directory.GetDirectories(bnetDir, "Hearthstone", SearchOption.AllDirectories);
                    if (hsDirs.Length > 0) return hsDirs[0];
                }
            }
            catch { }

            // 3. Try common paths
            string[] commonPaths = {
                @"C:\Program Files (x86)\Hearthstone",
                @"C:\Program Files\Hearthstone",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Hearthstone")
            };

            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private string GetPathFromRegistry()
        {
            try
            {
                // Try 64-bit and 32-bit registry paths
                string[] keys = {
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Hearthstone",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Hearthstone"
                };

                foreach (var keyName in keys)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyName))
                    {
                        if (key != null)
                        {
                            object val = key.GetValue("InstallLocation");
                            if (val != null) return val.ToString();
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }
            return null;
        }

        private string FindLatestLogPath(string hearthstonePath)
        {
            try
            {
                string logsDir = Path.Combine(hearthstonePath, "Logs");
                if (!Directory.Exists(logsDir)) return null;

                var dirs = Directory.GetDirectories(logsDir, "Hearthstone_*");
                if (dirs.Length == 0) return null;

                // Sort descending so the alphabetically last directory (latest date/time) is first
                var latestDir = System.Linq.Enumerable.FirstOrDefault(
                    System.Linq.Enumerable.OrderByDescending(dirs, d => Path.GetFileName(d))
                );

                if (latestDir != null)
                {
                    return Path.Combine(latestDir, "Power.log");
                }
            }
            catch (Exception ex)
            {
                OnStatusMessage?.Invoke(this, $"Błąd skanowania folderu logów: {ex.Message}");
            }
            return null;
        }

        public void Start(string hearthstonePath)
        {
            if (cts != null) return; // Already running

            baseHearthstonePath = hearthstonePath;
            cts = new CancellationTokenSource();
            
            OnStatusMessage?.Invoke(this, $"Uruchomiono moduł śledzenia. Katalog gry: {baseHearthstonePath}");

            Task.Run(() => WatchLoop(cts.Token));
            Task.Run(() => WatchDecksLoop(cts.Token));
            Task.Run(() => WatchZoneLoop(cts.Token));
            Task.Run(() => WatchCollectionLoop(cts.Token));
        }

        public void Stop()
        {
            if (cts == null) return;
            cts.Cancel();
            cts.Dispose();
            cts = null;
            OnStatusMessage?.Invoke(this, "Zatrzymano monitorowanie log\u00f3w.");
        }

        private async Task WatchZoneLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string latestDir = FindLatestLogDir(baseHearthstonePath);
                    if (!string.IsNullOrEmpty(latestDir))
                    {
                        string zonePath = Path.Combine(latestDir, "Zone.log");

                        if (zonePath != currentZoneLogPath)
                        {
                            currentZoneLogPath = zonePath;
                            zoneLastFileSize = 0;
                            OnStatusMessage?.Invoke(this, $"Monitoruj\u0119 Zone.log: {zonePath}");
                        }

                        if (File.Exists(currentZoneLogPath))
                        {
                            var fi = new FileInfo(currentZoneLogPath);
                            long currentSize = fi.Length;

                            if (currentSize < zoneLastFileSize)
                                zoneLastFileSize = 0;

                            if (currentSize > zoneLastFileSize)
                            {
                                using var fs = new FileStream(currentZoneLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                fs.Seek(zoneLastFileSize, SeekOrigin.Begin);
                                using var sr = new StreamReader(fs, Encoding.UTF8);
                                string line;
                                while ((line = await sr.ReadLineAsync()) != null)
                                    ParseZoneLine(line);
                                zoneLastFileSize = currentSize;
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { }

                await Task.Delay(150, token).ContinueWith(_ => { });
            }
        }

        private void ParseZoneLine(string line)
        {
            // Only process AddServerZoneChanges — TempApplyZoneChange is a duplicate of the same event
            if (!line.Contains("AddServerZoneChanges")) return;

            if (!line.Contains("dstZoneTag=") || !line.Contains("srcZoneTag=")) return;

            // Extract srcZoneTag
            int srcIdx = line.IndexOf("srcZoneTag=");
            int dstIdx = line.IndexOf("dstZoneTag=");
            if (srcIdx < 0 || dstIdx < 0) return;

            string srcZone = ExtractToken(line, srcIdx + 11);
            string dstZone = ExtractToken(line, dstIdx + 11);

            // Skip lines where nothing interesting happens (INVALID means initial position, not a move)
            if (srcZone == "INVALID" && dstZone == "INVALID") return;
            if (srcZone == dstZone) return;

            // Get cardId from inner powerTask entity: cardId=JAIL_225
            // Pattern: entity=[id=X cardId=YYYY name=...]
            int ptEntityIdx = line.IndexOf("entity=[id=");
            string cardId = null;
            if (ptEntityIdx >= 0)
            {
                int end = FindMatchingClosingBracket(line, ptEntityIdx);
                int bracketStart = line.IndexOf('[', ptEntityIdx);
                if (end > bracketStart && bracketStart >= 0)
                {
                    string block = line.Substring(bracketStart + 1, end - bracketStart - 1);
                    var ptProps = ParseProperties(block);
                    cardId = ptProps.GetValueOrDefault("cardId");
                }
            }

            // Get player from outer entity block (last entity= in line which has player=)
            // Pattern: entity=[entityName=... id=X zone=Y ... player=Z]
            int outerEntityIdx = line.LastIndexOf("entity=[entityName=");
            int playerId = -1;
            string outerCardId = null;
            string entityId = null;
            if (outerEntityIdx >= 0)
            {
                int end = FindMatchingClosingBracket(line, outerEntityIdx);
                int bracketStart = line.IndexOf('[', outerEntityIdx);
                if (end > bracketStart && bracketStart >= 0)
                {
                    string block = line.Substring(bracketStart + 1, end - bracketStart - 1);
                    var outerProps = ParseProperties(block);
                    int.TryParse(outerProps.GetValueOrDefault("player"), out playerId);
                    outerCardId = outerProps.GetValueOrDefault("cardId");
                    entityId = outerProps.GetValueOrDefault("id");
                }
            }


            // Prefer cardId from powerTask entity, fallback to outer entity
            if (string.IsNullOrEmpty(cardId)) cardId = outerCardId;

            // Try dictionary as last resort
            if (string.IsNullOrEmpty(cardId) && !string.IsNullOrEmpty(entityId))
                cardId = entityToCardId.GetValueOrDefault(entityId);

            if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(entityId)) return;
            if (cardId.StartsWith("HERO_") || cardId.Contains("_H_") || playerId <= 0) return;

            // Update mapping
            entityToCardId[entityId] = cardId;
            string name = entityToName.GetValueOrDefault(entityId);
            if (string.IsNullOrEmpty(name))
            {
                var ci = cardDb?.GetCardById(cardId);
                if (ci != null)
                {
                    name = ci.Name;
                    entityToName[entityId] = name;
                }
            }

            // Detect local player: local player draws use SHOW_ENTITY (srcZoneTag=INVALID in Zone.log at start),
            // but during game the srcZone will be DECK. Both players appear, so we need to distinguish.
            // Heuristic: the local player is the one whose DECK cards have srcZoneTag=INVALID/dstZoneTag=HAND
            // at game start (initial cards dealt). Actually simplest: the player whose card has known cardId
            // in SHOW_ENTITY zone changes is localPlayer. During gameplay both may have DECK→HAND but
            // local player's card IS revealed (has cardId) while opponent's is NOT.
            // So: if player=X and cardId is known and srcZone==DECK or dstZone==HAND → likely local player
            if (localPlayerId == -1 && !cardId.StartsWith("HERO_"))
            {
                // In Zone.log, local player's draws always show cardId (revealed)
                // Opponent's draws show empty cardId in inner entity
                // Check if the inner powerTask entity (first entity= block) has a real cardId
                if (!string.IsNullOrEmpty(cardId) && (dstZone == "HAND" || srcZone == "DECK"))
                {
                    // We can't know for sure from a single line, but set tentatively
                    // and refine on conflict
                    localPlayerId = playerId;
                    OnStatusMessage?.Invoke(this, $"[Zone] Wykryto lokalnego gracza: player={localPlayerId}");
                }
            }

            bool isFriendly = (playerId == localPlayerId) || (localPlayerId == -1 && playerId == 1);

            // ── Deduplication: track current zone per entity ──
            if (!string.IsNullOrEmpty(entityId))
            {
                string currentZone = entityZones.GetValueOrDefault(entityId);
                if (currentZone == dstZone)
                {
                    // Already processed this zone transition for this entity, skip duplicate line
                    return;
                }
                entityZones[entityId] = dstZone;
            }

            // Skip pure deck/setaside initialization lines (INVALID→DECK with empty cardId, INVALID→SETASIDE, etc.)
            if (srcZone == "INVALID" && dstZone != "HAND" && dstZone != "GRAVEYARD" && !(dstZone == "DECK" && !string.IsNullOrEmpty(cardId))) return;



            OnStatusMessage?.Invoke(this, $"[Zone] {name ?? cardId} {srcZone}→{dstZone} gracz={playerId} friendly={isFriendly}");

            OnCardTransition?.Invoke(this, new CardTransitionEventArgs
            {
                CardId = cardId,
                Name = name ?? cardId,
                EntityId = entityId,
                FromZone = srcZone,
                ToZone = dstZone,
                IsFriendly = isFriendly
            });
        }

        private static string ExtractToken(string line, int start)
        {
            int end = start;
            while (end < line.Length && line[end] != ' ' && line[end] != '\t' && line[end] != '\r' && line[end] != '\n')
                end++;
            return line.Substring(start, end - start);
        }

        private async Task WatchDecksLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string latestDir = FindLatestLogDir(baseHearthstonePath);
                    if (!string.IsNullOrEmpty(latestDir))
                    {
                        string decksPath = Path.Combine(latestDir, "Decks.log");

                        if (decksPath != currentDecksLogPath)
                        {
                            currentDecksLogPath = decksPath;
                            decksLastFileSize = 0;
                            OnStatusMessage?.Invoke(this, $"Monitoruję Decks.log: {decksPath}");
                        }

                        if (File.Exists(currentDecksLogPath))
                        {
                            var fi = new FileInfo(currentDecksLogPath);
                            long currentSize = fi.Length;

                            if (currentSize > decksLastFileSize)
                            {
                                using var fs = new FileStream(currentDecksLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                fs.Seek(decksLastFileSize, SeekOrigin.Begin);
                                using var sr = new StreamReader(fs, Encoding.UTF8);
                                string chunk = await sr.ReadToEndAsync();
                                decksLastFileSize = currentSize;

                                ParseDecksChunk(chunk);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { }

                await Task.Delay(500, token).ContinueWith(_ => { });
            }
        }

        private string currentDeckChunk = "";
        private void ParseDecksChunk(string chunk)
        {
            currentDeckChunk += chunk;
            var lines = currentDeckChunk.Split('\n');

            // We look for the pattern:
            //   I HH:mm:ss.fffffff Finding Game With Deck:
            //   I HH:mm:ss.fffffff ### DeckName
            //   I HH:mm:ss.fffffff # Deck ID: XXXXX
            //   I HH:mm:ss.fffffff AAECAaIH...   <-- this is the deckstring

            bool findingGame = false;
            string pendingDeckName = null;
            string foundDeckstring = null;
            string foundDeckName = null;

            for (int i = 0; i < lines.Length; i++)
            {
                // Strip log prefix: "I HH:mm:ss.fffffff "
                string raw = lines[i].Trim();
                string line = StripLogPrefix(raw);

                if (line == "Finding Game With Deck:")
                {
                    findingGame = true;
                    pendingDeckName = null;
                    foundDeckstring = null;
                    continue;
                }

                if (findingGame)
                {
                    if (line.StartsWith("### "))
                    {
                        pendingDeckName = line.Substring(4).Trim();
                    }
                    else if (line.StartsWith("# "))
                    {
                        // Skip comment lines like "# Deck ID:"
                    }
                    else if (line.Length > 20 && !line.StartsWith("#") && !line.Contains(" "))
                    {
                        // Deckstrings are long base64 strings with no spaces
                        foundDeckstring = line;
                        foundDeckName = pendingDeckName;
                        findingGame = false;
                    }
                    else if (line.Length == 0)
                    {
                        // empty line — end of block
                        findingGame = false;
                    }
                }
            }

            if (!string.IsNullOrEmpty(foundDeckstring))
            {
                OnStatusMessage?.Invoke(this, $"[Decks.log] Wykryto talię '{foundDeckName}' — ładuję automatycznie.");
                OnDeckDetected?.Invoke(this, new DeckDetectedEventArgs { Deckstring = foundDeckstring, DeckName = foundDeckName });
                currentDeckChunk = ""; // reset
            }
        }

        private string StripLogPrefix(string line)
        {
            // Lines look like: "I 22:45:47.8107354 ..."
            // Strip first two space-delimited tokens
            if (line.Length < 3) return line;
            int first = line.IndexOf(' ');
            if (first < 0) return line;
            int second = line.IndexOf(' ', first + 1);
            if (second < 0) return line;
            return line.Substring(second + 1).Trim();
        }

        private string FindLatestLogDir(string hearthstonePath)
        {
            try
            {
                string logsDir = Path.Combine(hearthstonePath, "Logs");
                if (!Directory.Exists(logsDir)) return null;
                var dirs = Directory.GetDirectories(logsDir, "Hearthstone_*");
                if (dirs.Length == 0) return null;
                return System.Linq.Enumerable.FirstOrDefault(
                    System.Linq.Enumerable.OrderByDescending(dirs, d => Path.GetFileName(d)));
            }
            catch { return null; }
        }

        private async Task WatchLoop(CancellationToken token)
        {
            string currentActiveLogPath = null;
            lastFileSize = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    string latestLogPath = FindLatestLogPath(baseHearthstonePath);

                    if (string.IsNullOrEmpty(latestLogPath))
                    {
                        OnStatusMessage?.Invoke(this, "Oczekiwanie na utworzenie aktywnego folderu logów (Hearthstone_*) przez grę...");
                        await Task.Delay(2000, token);
                        continue;
                    }

                    if (latestLogPath != currentActiveLogPath)
                    {
                        currentActiveLogPath = latestLogPath;
                        lastFileSize = 0;
                        _showEntityBuffer = null;

                        if (File.Exists(currentActiveLogPath))
                        {
                            var fi2 = new FileInfo(currentActiveLogPath);
                            lastFileSize = fi2.Length;
                        }

                        OnStatusMessage?.Invoke(this, $"Wykryto aktywną sesję gry! Czytanie pliku: {currentActiveLogPath} (Rozmiar początkowy: {lastFileSize} B)");
                    }

                    if (File.Exists(currentActiveLogPath))
                    {
                        var fi = new FileInfo(currentActiveLogPath);
                        long currentSize = fi.Length;

                        if (currentSize < lastFileSize)
                        {
                            OnStatusMessage?.Invoke(this, "Plik logów został skrócony lub zresetowany.");
                            lastFileSize = 0;
                            _showEntityBuffer = null;
                        }

                        if (currentSize > lastFileSize)
                        {
                            using (var fs = new FileStream(currentActiveLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                fs.Seek(lastFileSize, SeekOrigin.Begin);
                                using (var reader = new StreamReader(fs, Encoding.UTF8))
                                {
                                    string line;
                                    while ((line = await reader.ReadLineAsync()) != null)
                                    {
                                        ParseLine(line);
                                    }
                                }
                            }
                            lastFileSize = currentSize;
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore transient lock/file-sharing errors
                }

                await Task.Delay(150, token);
            }
        }

        // State for multi-line SHOW_ENTITY block tracking
        private class ShowEntityBlock
        {
            public string EntityId;
            public string CardId;
            public string FromZone;
            public int PlayerId;
            public bool ZoneHandSeen;
            public bool ZoneDeckSeen;
        }
        private ShowEntityBlock _showEntityBuffer;

        private void ParseLine(string line)
        {
            // ── Game start ──────────────────────────────────────────────
            if (line.Contains("CREATE_GAME"))
            {
                localPlayerId = -1;
                entityToCardId.Clear();
                entityToName.Clear();
                entityZones.Clear();
                _showEntityBuffer = null;
                OnStatusMessage?.Invoke(this, "[Gra] Rozpoznano rozpocz\u0119cie meczu (CREATE_GAME).");
                OnGameStart?.Invoke(this, EventArgs.Empty);
                return;
            }

            // ── Game end ─────────────────────────────────────────────────
            if (line.Contains("tag=STATE") && line.Contains("value=COMPLETE") && line.Contains("GameEntity"))
            {
                OnStatusMessage?.Invoke(this, "[Gra] Mecz zako\u0144czony (STATE=COMPLETE).");
                OnGameEnd?.Invoke(this, EventArgs.Empty);
                _showEntityBuffer = null;
                return;
            }

            // ── SHOW_ENTITY block start ───────────────────────────────────
            // Format: "... SHOW_ENTITY - Updating Entity=[... id=14 zone=DECK ... player=1] CardID=CORE_RLK_567"
            if (line.Contains("SHOW_ENTITY - Updating Entity="))
            {
                // Flush any previous unfinished block first
                FlushShowEntityBlock();

                int cardIdIdx = line.IndexOf("CardID=");
                if (cardIdIdx < 0) return;
                string cardId = line.Substring(cardIdIdx + 7).Trim();
                if (string.IsNullOrEmpty(cardId)) return;

                string entityId = null;
                string fromZone = null;
                int playerId = -1;

                int entityIdx = line.IndexOf("Entity=");
                int bracketStart = line.IndexOf('[', entityIdx);
                if (bracketStart >= 0 && bracketStart < cardIdIdx)
                {
                    int bracketEnd = line.IndexOf(']', bracketStart);
                    if (bracketEnd > bracketStart)
                    {
                        string eb = line.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
                        var props = ParseProperties(eb);
                        entityId = props.GetValueOrDefault("id");
                        fromZone = props.GetValueOrDefault("zone");
                        string ps = props.GetValueOrDefault("player");
                        int.TryParse(ps, out playerId);
                    }
                }
                else
                {
                    // Simple format without brackets — no player/zone info in this line
                    int spaceIdx = line.IndexOf(' ', entityIdx + 7);
                    entityId = spaceIdx > 0
                        ? line.Substring(entityIdx + 7, spaceIdx - (entityIdx + 7)).Trim()
                        : line.Substring(entityIdx + 7).Trim();
                }

                // Update entity→cardId mapping
                if (!string.IsNullOrEmpty(entityId) && !string.IsNullOrEmpty(cardId))
                {
                    entityToCardId[entityId] = cardId;
                    var ci = cardDb?.GetCardById(cardId);
                    if (ci != null) entityToName[entityId] = ci.Name;

                    // Detect local player from first revealed deck/hand card
                    if (localPlayerId == -1 && playerId > 0
                        && !cardId.StartsWith("HERO_") && !cardId.EndsWith("e")
                        && (fromZone == "DECK" || fromZone == "HAND"))
                    {
                        localPlayerId = playerId;
                        OnStatusMessage?.Invoke(this, $"[Ustawienie] Wykryto ID lokalnego gracza: {localPlayerId}");
                    }

                    // Start tracking block for draw detection
                    // Only track player=1 (local) cards starting from DECK zone
                    bool isLocal = (playerId == localPlayerId) || (localPlayerId == -1 && playerId == 1);
                    if (fromZone == "DECK" && isLocal)
                    {
                        _showEntityBuffer = new ShowEntityBlock
                        {
                            EntityId = entityId,
                            CardId = cardId,
                            FromZone = fromZone,
                            PlayerId = playerId
                        };
                    }
                }
                return;
            }

            // ── Inside SHOW_ENTITY block — look for tag=ZONE value=HAND/DECK ──
            if (_showEntityBuffer != null)
            {
                // tag lines inside block look like: "...     tag=ZONE value=HAND"
                if (line.Contains("tag=ZONE ") || line.Contains("tag=ZONE\t"))
                {
                    int valueIdx = line.IndexOf("value=");
                    if (valueIdx >= 0)
                    {
                        string zoneVal = line.Substring(valueIdx + 6).Trim();
                        if (zoneVal == "HAND") _showEntityBuffer.ZoneHandSeen = true;
                        if (zoneVal == "DECK") _showEntityBuffer.ZoneDeckSeen = true;
                    }
                    return;
                }

                // Any line starting a new top-level entry flushes the buffer
                if (line.Contains("BLOCK_START") || line.Contains("BLOCK_END") ||
                    line.Contains("TAG_CHANGE") || line.Contains("FULL_ENTITY") ||
                    line.Contains("SHOW_ENTITY") || line.Contains("CREATE_GAME"))
                {
                    FlushShowEntityBlock();
                    // Fall through to process this line normally
                }
            }

            // ── TAG_CHANGE — zone transitions ────────────────────────────
            // Handles: HAND→PLAY (played), HAND→DECK / PLAY→DECK (shuffle back), opponent reveals
            if (line.Contains("TAG_CHANGE") && line.Contains("tag=ZONE") && line.Contains("value=") && line.Contains("Entity=["))
            {
                int entityStart = line.IndexOf("Entity=[");
                int entityEnd = line.IndexOf(']', entityStart);
                if (entityStart < 0 || entityEnd <= entityStart) return;

                string entityBlock = line.Substring(entityStart + 8, entityEnd - entityStart - 8);
                var props = ParseProperties(entityBlock);

                string entityId = props.GetValueOrDefault("id");
                string fromZone = props.GetValueOrDefault("zone");
                string playerStr = props.GetValueOrDefault("player");
                string cardId = props.GetValueOrDefault("cardId");

                // Must be tag=ZONE (not tag=ZONE_POSITION etc.)
                int tagIdx = line.IndexOf("tag=ZONE", entityEnd);
                if (tagIdx < 0) return;
                string afterTag = line.Substring(tagIdx + 8).TrimStart();
                if (!afterTag.StartsWith("value=") && !afterTag.StartsWith(" value=")) return;

                int valueIdx = line.IndexOf("value=", tagIdx);
                if (valueIdx < 0) return;
                string toZone = line.Substring(valueIdx + 6).Trim();

                // Resolve cardId from dictionary
                if (string.IsNullOrEmpty(cardId) && !string.IsNullOrEmpty(entityId))
                    cardId = entityToCardId.GetValueOrDefault(entityId);

                if (string.IsNullOrEmpty(entityId) || string.IsNullOrEmpty(cardId)) return;
                if (cardId.StartsWith("HERO_") || cardId.Contains("_H_")) return;

                entityToCardId[entityId] = cardId;

                string name = entityToName.GetValueOrDefault(entityId);
                if (string.IsNullOrEmpty(name))
                {
                    var ci = cardDb?.GetCardById(cardId);
                    if (ci != null) name = ci.Name;
                }

                if (!int.TryParse(playerStr, out int playerId)) return;

                if (localPlayerId == -1 && !cardId.StartsWith("HERO_") &&
                    (fromZone == "HAND" || toZone == "HAND"))
                {
                    localPlayerId = playerId;
                    OnStatusMessage?.Invoke(this, $"[Ustawienie] Wykryto gracza z TAG_CHANGE: {localPlayerId}");
                }
            }
        }

        private void FlushShowEntityBlock()
        {
            _showEntityBuffer = null;
        }



        private static Dictionary<string, string> ParseProperties(string block)
        {
            var dict = new Dictionary<string, string>();
            int index = 0;
            while (index < block.Length)
            {
                int eqIdx = block.IndexOf('=', index);
                if (eqIdx < 0) break;
                
                int keyStart = block.LastIndexOf(' ', eqIdx);
                if (keyStart < index) keyStart = index;
                else keyStart += 1;
                
                string key = block.Substring(keyStart, eqIdx - keyStart).Trim();
                
                int nextEqIdx = block.IndexOf('=', eqIdx + 1);
                int valEnd;
                if (nextEqIdx >= 0)
                {
                    valEnd = block.LastIndexOf(' ', nextEqIdx);
                    if (valEnd < eqIdx) valEnd = block.Length;
                }
                else
                {
                    valEnd = block.Length;
                }
                
                string value = block.Substring(eqIdx + 1, valEnd - (eqIdx + 1)).Trim();
                dict[key] = value;
                index = valEnd;
            }
            return dict;
        }

        private static int FindMatchingClosingBracket(string text, int startPos)
        {
            int bracketIdx = text.IndexOf('[', startPos);
            if (bracketIdx < 0) return -1;

            int depth = 0;
            for (int i = bracketIdx; i < text.Length; i++)
            {
                if (text[i] == '[') depth++;
                else if (text[i] == ']')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }
        private string FindCollectionLogPath()
        {
            string hsPath = LocateHearthstonePath();
            if (string.IsNullOrEmpty(hsPath)) hsPath = baseHearthstonePath;
            if (string.IsNullOrEmpty(hsPath)) return null;

            try
            {
                string logsDir = Path.Combine(hsPath, "Logs");
                if (!Directory.Exists(logsDir)) return null;
                var dirs = Directory.GetDirectories(logsDir, "Hearthstone_*");
                if (dirs.Length == 0) return null;
                var latestDir = System.Linq.Enumerable.FirstOrDefault(
                    System.Linq.Enumerable.OrderByDescending(dirs, d => Path.GetFileName(d))
                );
                if (latestDir == null) return null;
                string collPath = Path.Combine(latestDir, "CollectionManager.log");
                if (File.Exists(collPath)) return collPath;
                // Fallback to Decks.log if CollectionManager.log doesn't exist yet
                string decksPath = Path.Combine(latestDir, "Decks.log");
                return File.Exists(decksPath) ? decksPath : null;
            }
            catch { return null; }
        }

        private async Task WatchCollectionLoop(CancellationToken token)
        {
            OnStatusMessage?.Invoke(this, "Monitorowanie kolekcji kart (CollectionManager.log) uruchomione.");
            string currentPath = null;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    string path = FindCollectionLogPath();
                    if (path != currentPath)
                    {
                        currentPath = path;
                        collectionLastFileSize = 0;
                    }

                    if (currentPath != null && File.Exists(currentPath))
                    {
                        long size = new FileInfo(currentPath).Length;
                        if (size > collectionLastFileSize)
                        {
                            string newContent;
                            using (var fs = new FileStream(currentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var sr = new StreamReader(fs, Encoding.UTF8))
                            {
                                if (collectionLastFileSize > 0)
                                    fs.Seek(collectionLastFileSize, SeekOrigin.Begin);
                                newContent = await sr.ReadToEndAsync();
                            }
                            collectionLastFileSize = size;

                            // Parse lines like:
                            // [CollectionManager] - AddOrUpdateCard(dbfId=1234, count=2, ...)
                            // or: [CollectionManager] OnLoaded: dbfId=1234 count=2
                            var collection = new Dictionary<int, int>();
                            var patterns = new[]
                            {
                                // Pattern 1: AddOrUpdateCard dbfId=1234, count=2
                                new System.Text.RegularExpressions.Regex(
                                    @"AddOrUpdateCard.*?dbfId=(\d+).*?count=(\d+)",
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                                // Pattern 2: dbfId=1234 count=2 (any format)
                                new System.Text.RegularExpressions.Regex(
                                    @"dbfId=(\d+)[,\s]+count=(\d+)",
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                                // Pattern 3: id=1234 count=2
                                new System.Text.RegularExpressions.Regex(
                                    @"\bid=(\d+).*?count=(\d+)",
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                            };

                            foreach (var pattern in patterns)
                            {
                                var matches = pattern.Matches(newContent);
                                foreach (System.Text.RegularExpressions.Match m in matches)
                                {
                                    if (int.TryParse(m.Groups[1].Value, out int dbfId) &&
                                        int.TryParse(m.Groups[2].Value, out int count))
                                    {
                                        if (collection.TryGetValue(dbfId, out int existing))
                                            collection[dbfId] = Math.Max(existing, count);
                                        else
                                            collection[dbfId] = count;
                                    }
                                }
                            }

                            // Also scan Decks.log in the same log directory for deckstrings
                            try
                            {
                                string latestDir = Path.GetDirectoryName(currentPath);
                                string decksLog = Path.Combine(latestDir, "Decks.log");
                                if (File.Exists(decksLog))
                                {
                                    string decksContent = File.ReadAllText(decksLog);
                                    var deckMatches = System.Text.RegularExpressions.Regex.Matches(decksContent, @"\b(AAE[A-Za-z0-9+/=]+)");
                                    foreach (System.Text.RegularExpressions.Match dm in deckMatches)
                                    {
                                        try
                                        {
                                            var parsed = DeckstringParser.Parse(dm.Value);
                                            foreach (var kvp in parsed.CardCounts)
                                            {
                                                if (collection.TryGetValue(kvp.Key, out int existing))
                                                    collection[kvp.Key] = Math.Max(existing, kvp.Value);
                                                else
                                                    collection[kvp.Key] = kvp.Value;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }

                            if (collection.Count > 0)
                            {
                                OnStatusMessage?.Invoke(this, $"CollectionManager.log: wykryto {collection.Count} kart z kolekcji! Wysyłanie...");
                                OnCollectionDetected?.Invoke(this, collection);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnStatusMessage?.Invoke(this, $"Błąd monitorowania kolekcji: {ex.Message}");
                }

                await Task.Delay(1500, token).ContinueWith(_ => { });
            }
        }
    }
}
