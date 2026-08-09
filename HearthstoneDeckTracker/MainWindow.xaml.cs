using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace HearthstoneDeckTracker
{
    public class DeckItemViewModel : DependencyObject
    {
        public static readonly DependencyProperty TilePathProperty =
            DependencyProperty.Register("TilePath", typeof(string), typeof(DeckItemViewModel), new PropertyMetadata(null));

        public int DbfId { get; set; }
        public string CardId { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public int Count { get; set; }
        public string Rarity { get; set; }

        public string CountText => Count > 1 ? $"x{Count}" : (Rarity == "LEGENDARY" ? "★" : "x1");

        public string TilePath
        {
            get => (string)GetValue(TilePathProperty);
            set => SetValue(TilePathProperty, value);
        }
    }

    public class AppConfig
    {
        public string HearthstonePath { get; set; }
        public string Locale { get; set; } = "enUS";
        public string LastDeckCode { get; set; }
        public string ApiUrl { get; set; } = "https://hs-rival-meta.onrender.com";
    }

    public partial class MainWindow : Window
    {
        private CardDatabase cardDb;
        private LogWatcher logWatcher;
        private OverlayWindow overlayWindow;
        private OpponentOverlayWindow opponentOverlayWindow;
        private WidgetWindow widgetWindow;
        private AppConfig config;
        private string configPath;

        private ObservableCollection<DeckItemViewModel> currentDeckList = new ObservableCollection<DeckItemViewModel>();

        public MainWindow()
        {
            InitializeComponent();
            configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            
            cardDb = new CardDatabase();
            logWatcher = new LogWatcher(cardDb);
            
            logWatcher.OnStatusMessage += LogWatcher_OnStatusMessage;
            logWatcher.OnGameStart += LogWatcher_OnGameStart;
            logWatcher.OnGameEnd += LogWatcher_OnGameEnd;
            logWatcher.OnCardTransition += LogWatcher_OnCardTransition;
            logWatcher.OnDeckDetected += LogWatcher_OnDeckDetected;
            logWatcher.OnCollectionDetected += LogWatcher_OnCollectionDetected;

            ItemsDeckPreview.ItemsSource = currentDeckList;

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();

            // Populate language combo
            ComboLocale.Items.Clear();
            ComboLocale.Items.Add(new ComboBoxItem { Content = "English (enUS)", Tag = "enUS" });
            ComboLocale.Items.Add(new ComboBoxItem { Content = "Polski (plPL)", Tag = "plPL" });

            foreach (ComboBoxItem item in ComboLocale.Items)
            {
                if (item.Tag.ToString() == config.Locale)
                {
                    ComboLocale.SelectedItem = item;
                    break;
                }
            }
            if (ComboLocale.SelectedItem == null) ComboLocale.SelectedIndex = 0;

            UpdateLanguageUI(config.Locale);

            // Auto-detect game path (always try)
            config.HearthstonePath = logWatcher.LocateHearthstonePath();
            if (string.IsNullOrEmpty(config.HearthstonePath) && !string.IsNullOrEmpty(TxtHsPath?.Text))
                config.HearthstonePath = TxtHsPath.Text;

            // Update new UI: game path display
            bool gameFound = !string.IsNullOrEmpty(config.HearthstonePath) && Directory.Exists(config.HearthstonePath);
            bool isPl = config.Locale == "plPL";
            Dispatcher.Invoke(() =>
            {
                if (TxtGamePathDisplay != null)
                    TxtGamePathDisplay.Text = gameFound ? config.HearthstonePath : (isPl ? "Nie znaleziono Hearthstone" : "Hearthstone not found");
                if (BorderGameDetected != null)
                {
                    BorderGameDetected.Background = gameFound
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(27, 77, 30))
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(77, 27, 27));
                    if (TxtGameDetected != null)
                    {
                        TxtGameDetected.Text = gameFound ? (isPl ? "✓ Wykryto" : "✓ Detected") : (isPl ? "✗ Brak" : "✗ Missing");
                        TxtGameDetected.Foreground = gameFound
                            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 187, 106))
                            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 115, 115));
                    }
                }
                if (TxtStatGame != null) TxtStatGame.Text = gameFound ? (isPl ? "Wykryta" : "Detected") : (isPl ? "Nie znaleziono" : "Not found");
            });

            try
            {
                await cardDb.InitializeAsync(config.Locale);
                logWatcher.SetupLogConfig();

                if (gameFound)
                {
                    logWatcher.Start(config.HearthstonePath);
                    Dispatcher.Invoke(() =>
                    {
                        if (TxtStatus != null) TxtStatus.Text = isPl ? "Oczekiwanie na mecz" : "Waiting for match";
                        if (TxtStatTracking != null) TxtStatTracking.Text = isPl ? "Aktywne" : "Active";
                        if (StatusDot != null) StatusDot.Fill = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(76, 175, 80));
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (TxtStatus != null) TxtStatus.Text = isPl ? "Gra nie wykryta" : "Game not found";
                        if (TxtStatTracking != null) TxtStatTracking.Text = isPl ? "Nieaktywne" : "Inactive";
                        if (StatusDot != null) StatusDot.Fill = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(229, 115, 115));
                    });
                }

                // Auto-load last deck if present
                if (!string.IsNullOrEmpty(config.LastDeckCode))
                    await LoadDeckFromCode(config.LastDeckCode);

                // Auto-sync collection on startup
                BtnSyncCollection_Click(null, null);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    if (TxtStatus != null) TxtStatus.Text = "Błąd inicjalizacji";
                });
                MessageBox.Show($"Błąd: {ex.Message}", "Hearthstone Deck Tracker", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            logWatcher.Stop();
            if (overlayWindow != null)
            {
                overlayWindow.Close();
            }
            if (opponentOverlayWindow != null)
            {
                opponentOverlayWindow.Close();
            }
            if (widgetWindow != null)
            {
                widgetWindow.Close();
            }
            SaveConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<AppConfig>(json);
                }
                catch
                {
                    config = new AppConfig();
                }
            }
            else
            {
                config = new AppConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                config.HearthstonePath = TxtHsPath.Text.Trim();
                config.LastDeckCode = TxtDeckCode.Text.Trim();
                if (ComboLocale.SelectedItem is ComboBoxItem selected)
                {
                    config.Locale = selected.Tag.ToString();
                }
                
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch
            {
                // Ignore config write errors
            }
        }

        private void TxtDeckCode_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Save the deck code immediately whenever it changes so it persists between restarts
            string code = TxtDeckCode.Text.Trim();
            if (!string.IsNullOrEmpty(code) && config != null)
            {
                config.LastDeckCode = code;
                try
                {
                    string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(configPath, json);
                }
                catch { }
            }
        }

        private void LogToConsole(string text)
        {
            Dispatcher.Invoke(() =>
            {
                TxtConsole.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n");
                TxtConsole.ScrollToEnd();
            });
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Wybierz folder instalacyjny Hearthstone",
                InitialDirectory = string.IsNullOrEmpty(TxtHsPath.Text) ? @"C:\Program Files (x86)" : TxtHsPath.Text
            };

            if (dialog.ShowDialog() == true)
            {
                TxtHsPath.Text = dialog.FolderName;
                SaveConfig();
            }
        }

        private async void BtnLaunchHS_Click(object sender, RoutedEventArgs e)
        {
            // Always reconfigure logs silently before launching
            logWatcher.SetupLogConfig();

            if (TxtCollectionStatus != null)
                TxtCollectionStatus.Text = "Uruchamianie Hearthstone przez Battle.net...";
            if (BtnLaunchHS != null) BtnLaunchHS.IsEnabled = false;
            if (TxtLaunchBtnLabel != null) TxtLaunchBtnLabel.Text = "Uruchamianie...";

            bool launched = false;

            try
            {
                // Method 1: Battle.net client executable with --exec="launch WTCG" argument
                string[] bnetPaths = {
                    @"C:\Program Files (x86)\Battle.net\Battle.net.exe",
                    @"C:\Program Files (x86)\Battle.net\Battle.net Launcher.exe",
                    @"C:\Program Files\Battle.net\Battle.net.exe",
                    @"C:\Program Files\Battle.net\Battle.net Launcher.exe"
                };

                foreach (string bnetExe in bnetPaths)
                {
                    if (File.Exists(bnetExe))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = bnetExe,
                            Arguments = "--exec=\"launch WTCG\"",
                            UseShellExecute = true
                        });
                        launched = true;
                        break;
                    }
                }

                // Method 2: battlenet:// URL protocol fallback
                if (!launched)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "battlenet://WTCG",
                        UseShellExecute = true
                    });
                    launched = true;
                }

                if (launched)
                {
                    if (TxtCollectionStatus != null)
                        TxtCollectionStatus.Text = "✓ Logi skonfigurowane. Gra się uruchamia — wejdź do Mojej Kolekcji!";
                    if (TxtLaunchBtnLabel != null) TxtLaunchBtnLabel.Text = "Hearthstone działa ✓";

                    // Monitor HS process and re-enable button when it closes
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(10000);
                        while (true)
                        {
                            bool hsRunning = System.Diagnostics.Process.GetProcessesByName("Hearthstone").Length > 0;
                            Dispatcher.Invoke(() =>
                            {
                                if (TxtLaunchBtnLabel != null)
                                    TxtLaunchBtnLabel.Text = hsRunning ? "Hearthstone działa ✓" : "Uruchom Hearthstone";
                                if (BtnLaunchHS != null)
                                    BtnLaunchHS.IsEnabled = !hsRunning;
                            });
                            if (!hsRunning) break;
                            await Task.Delay(5000);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                if (TxtCollectionStatus != null)
                    TxtCollectionStatus.Text = $"Błąd uruchamiania: {ex.Message}";
                if (BtnLaunchHS != null) BtnLaunchHS.IsEnabled = true;
                if (TxtLaunchBtnLabel != null) TxtLaunchBtnLabel.Text = "Uruchom Hearthstone";
            }
        }

        private async void BtnSyncFull_Click(object sender, RoutedEventArgs e)
        {
            if (TxtCollectionStatus != null)
                TxtCollectionStatus.Text = "Próba połączenia z grą...";
            if (BtnSyncFull != null) BtnSyncFull.IsEnabled = false;

            try
            {
                // 1. Locate HearthMirror.dll
                string hdtLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HearthstoneDeckTracker");
                string hmPath = null;
                if (Directory.Exists(hdtLocal))
                {
                    var files = Directory.GetFiles(hdtLocal, "HearthMirror.dll", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        hmPath = files[0];
                    }
                }

                if (hmPath == null)
                {
                    MessageBox.Show("Aby skanować 100% pamięci RAM, potrzebujesz oryginalnego Hearthstone Deck Trackera. Pobierz go z hsreplay.net lub użyj Skanu Częściowego.", "Brak bibliotek", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (TxtCollectionStatus != null) TxtCollectionStatus.Text = "Brak HearthMirror.dll. Użyj skanu częściowego z talii.";
                    return;
                }

                // 2. Load DLL and invoke GetCollection via Reflection
                var asm = System.Reflection.Assembly.LoadFrom(hmPath);
                var refType = asm.GetType("HearthMirror.Reflection");
                var clientProp = refType.GetProperty("Client");
                var client = clientProp.GetValue(null);

                var getCollMethod = client.GetType().GetMethod("GetCollection");
                var collection = (IEnumerable<object>)getCollMethod.Invoke(client, null);

                if (collection == null)
                {
                    MessageBox.Show("Nie wykryto kolekcji w pamięci! Upewnij się, że gra Hearthstone jest włączona i wszedłeś w zakładkę 'Moja Kolekcja'.", "Brak danych z RAM", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (TxtCollectionStatus != null) TxtCollectionStatus.Text = "Gra nie jest w kolekcji. Wejdź do kolekcji i ponów próbę.";
                    return;
                }

                // 3. Parse collection
                var collectionMap = new Dictionary<int, int>();
                foreach (var item in collection)
                {
                    var type = item.GetType();
                    string idString = (string)type.GetProperty("Id").GetValue(item);
                    int count = (int)type.GetProperty("Count").GetValue(item);
                    
                    if (cardDb != null)
                    {
                        var cardInfo = cardDb.GetCardById(idString);
                        if (cardInfo != null && cardInfo.DbfId > 0)
                        {
                            int existing = collectionMap.ContainsKey(cardInfo.DbfId) ? collectionMap[cardInfo.DbfId] : 0;
                            collectionMap[cardInfo.DbfId] = Math.Max(existing, count);
                        }
                    }
                }

                // 4. Also add Core Set
                if (cardDb != null)
                {
                    foreach (var card in cardDb.GetAllCards())
                    {
                        if (card.Rarity == "FREE" || (card.Id != null && card.Id.StartsWith("CORE_")))
                        {
                            int maxQty = card.Rarity == "LEGENDARY" ? 1 : 2;
                            if (!collectionMap.ContainsKey(card.DbfId) || collectionMap[card.DbfId] < maxQty)
                            {
                                collectionMap[card.DbfId] = maxQty;
                            }
                        }
                    }
                }

                // 4b. Extract Arcane Dust from RAM via GetFullCollection
                int arcaneDust = 0;
                try
                {
                    var getFullCollMethod = client.GetType().GetMethod("GetFullCollection");
                    if (getFullCollMethod != null)
                    {
                        var fullColl = getFullCollMethod.Invoke(client, null);
                        if (fullColl != null)
                        {
                            var dustProp = fullColl.GetType().GetProperty("Dust");
                            if (dustProp != null)
                            {
                                object dustVal = dustProp.GetValue(fullColl);
                                if (dustVal != null) arcaneDust = Convert.ToInt32(dustVal);
                            }
                        }
                    }
                }
                catch { }

                if (TxtCollectionStatus != null)
                    TxtCollectionStatus.Text = $"Pomyślnie odczytano {collectionMap.Count} kart i {arcaneDust} pyłu z RAM. Wysyłanie...";

                // 5. Send to server
                using var httpClient = new System.Net.Http.HttpClient();
                var payload = new { collection = collectionMap, dust = arcaneDust, isFullSync = true };
                string json = JsonSerializer.Serialize(payload);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                string apiUrl = config?.ApiUrl ?? "https://hs-rival-meta.onrender.com";
                var response = await httpClient.PostAsync($"{apiUrl.TrimEnd('/')}/api/collection", content);

                bool isPl = config?.Locale == "plPL";
                if (response.IsSuccessStatusCode)
                {
                    if (TxtCollectionStatus != null)
                        TxtCollectionStatus.Text = isPl ? $"✓ Zsynchronizowano {collectionMap.Count} kart oraz {arcaneDust} pyłu z grą!" : $"✓ Synced {collectionMap.Count} cards & {arcaneDust} dust with game!";
                    if (TxtStatCollection != null)
                        TxtStatCollection.Text = isPl ? $"{collectionMap.Count} kart" : $"{collectionMap.Count} cards";
                }
                else
                {
                    if (TxtCollectionStatus != null)
                        TxtCollectionStatus.Text = isPl ? $"Błąd zapisu na serwerze: {response.StatusCode}" : $"Server error: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odczytu pamięci: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                if (TxtCollectionStatus != null)
                    TxtCollectionStatus.Text = "Błąd skanowania RAM.";
            }
            finally
            {
                if (BtnSyncFull != null) BtnSyncFull.IsEnabled = true;
            }
        }

        private void LinkInstructions_Click(object sender, RoutedEventArgs e)
        {
            if (GridInstructions != null) GridInstructions.Visibility = Visibility.Visible;
        }

        private void BtnCloseInstructions_Click(object sender, RoutedEventArgs e)
        {
            if (GridInstructions != null) GridInstructions.Visibility = Visibility.Collapsed;
        }

        private void BtnDownloadHDT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://hsreplay.net/downloads/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się otworzyć strony: {ex.Message}");
            }
        }

        private void BtnOpenWebApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string apiUrl = config?.ApiUrl ?? "https://hs-rival-meta.onrender.com";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = apiUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie udało się otworzyć strony: {ex.Message}");
            }
        }

        private async void BtnSyncCollection_Click(object sender, RoutedEventArgs e)
        {
            if (TxtCollectionStatus != null)
                TxtCollectionStatus.Text = "Skanowanie kolekcji...";
            if (BtnSync != null) BtnSync.IsEnabled = false;

            try
            {
                var collectionMap = new Dictionary<int, int>();

                // Core Set & FREE cards (always owned)
                if (cardDb != null)
                {
                    foreach (var card in cardDb.GetAllCards())
                    {
                        if (card.Rarity == "FREE" || (card.Id != null && card.Id.StartsWith("CORE_")))
                        {
                            int maxQty = card.Rarity == "LEGENDARY" ? 1 : 2;
                            collectionMap[card.DbfId] = maxQty;
                        }
                    }
                }

                // Scan PlayerDecks.xml from HDT AppData
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string xmlPath = Path.Combine(appData, "HearthstoneDeckTracker", "PlayerDecks.xml");
                if (File.Exists(xmlPath))
                {
                    string xml = File.ReadAllText(xmlPath);
                    var cardRegex = new System.Text.RegularExpressions.Regex(@"<Card>[\s\S]*?<Id>(.*?)<\/Id>[\s\S]*?<Count>(.*?)<\/Count>[\s\S]*?<\/Card>");
                    var matches = cardRegex.Matches(xml);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        string cardId = match.Groups[1].Value;
                        if (int.TryParse(match.Groups[2].Value, out int count))
                        {
                            var cardInfo = cardDb?.GetCardById(cardId);
                            if (cardInfo != null)
                            {
                                int maxQty = cardInfo.Rarity == "LEGENDARY" ? 1 : 2;
                                int existing = collectionMap.ContainsKey(cardInfo.DbfId) ? collectionMap[cardInfo.DbfId] : 0;
                                collectionMap[cardInfo.DbfId] = Math.Max(existing, Math.Min(count, maxQty));
                            }
                        }
                    }
                }

                // Scan Decks.log from active HS Logs directory
                try
                {
                    string hsPath = logWatcher.LocateHearthstonePath();
                    if (!string.IsNullOrEmpty(hsPath))
                    {
                        string logsDir = Path.Combine(hsPath, "Logs");
                        if (Directory.Exists(logsDir))
                        {
                            var dirs = Directory.GetDirectories(logsDir, "Hearthstone_*");
                            foreach (var dir in dirs)
                            {
                                string decksLog = Path.Combine(dir, "Decks.log");
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
                                                int existing = collectionMap.ContainsKey(kvp.Key) ? collectionMap[kvp.Key] : 0;
                                                collectionMap[kvp.Key] = Math.Max(existing, kvp.Value);
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                using var client = new System.Net.Http.HttpClient();
                var payload = new { collection = collectionMap };
                string json = JsonSerializer.Serialize(payload);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                string apiUrl = config?.ApiUrl ?? "https://hs-rival-meta.onrender.com";
                var response = await client.PostAsync($"{apiUrl.TrimEnd('/')}/api/collection", content);

                if (response.IsSuccessStatusCode)
                {
                    if (TxtCollectionStatus != null)
                        TxtCollectionStatus.Text = $"✓ Zsynchronizowano {collectionMap.Count} zweryfikowanych kart z aplikacją Web!";
                    if (TxtStatCollection != null)
                        TxtStatCollection.Text = $"{collectionMap.Count} kart";
                }
                else
                {
                    if (TxtCollectionStatus != null)
                        TxtCollectionStatus.Text = $"Błąd wysyłania (HTTP {response.StatusCode})";
                }
            }
            catch (Exception ex)
            {
                if (TxtCollectionStatus != null)
                    TxtCollectionStatus.Text = $"Błąd: {ex.Message}";
            }
            finally
            {
                if (BtnSync != null) BtnSync.IsEnabled = true;
            }
        }

        private async void LogWatcher_OnCollectionDetected(object sender, Dictionary<int, int> collectionFromLog)
        {
            Dispatcher.Invoke(() =>
            {
                if (TxtCollectionStatus != null)
                    TxtCollectionStatus.Text = $"Wykryto {collectionFromLog.Count} kart — wysyłanie do aplikacji Web...";
            });

            try
            {
                var merged = new Dictionary<int, int>(collectionFromLog);
                if (cardDb != null)
                {
                    foreach (var card in cardDb.GetAllCards())
                    {
                        if (card.Rarity == "FREE" || (card.Id != null && card.Id.StartsWith("CORE_")))
                        {
                            int maxQty = card.Rarity == "LEGENDARY" ? 1 : 2;
                            if (!merged.ContainsKey(card.DbfId))
                                merged[card.DbfId] = maxQty;
                        }
                    }
                }

                using var httpClient = new System.Net.Http.HttpClient();
                var payload = new { collection = merged, isFullSync = true };
                string json = JsonSerializer.Serialize(payload);
                var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                string apiUrl = config?.ApiUrl ?? "https://hs-rival-meta.onrender.com";
                var response = await httpClient.PostAsync($"{apiUrl.TrimEnd('/')}/api/collection", content);

                bool isPl = config?.Locale == "plPL";
                if (response.IsSuccessStatusCode)
                {
                    if (TxtCollectionStatus != null)
                        TxtCollectionStatus.Text = isPl ? $"✓ Zsynchronizowano {merged.Count} kart z aplikacją Web!" : $"✓ Synced {merged.Count} cards with Web App!";
                    if (TxtStatCollection != null)
                        TxtStatCollection.Text = isPl ? $"{merged.Count} kart" : $"{merged.Count} cards";
                }
                else
                {
                    if (TxtCollectionStatus != null)
                        TxtCollectionStatus.Text = isPl ? $"Błąd synchronizacji (HTTP {response.StatusCode})" : $"Sync error (HTTP {response.StatusCode})";
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    if (TxtCollectionStatus != null)
                        TxtCollectionStatus.Text = $"Błąd: {ex.Message}";
                });
            }
        }

        private void UpdateLanguageUI(string locale)
        {
            bool isPl = locale == "plPL";

            if (TxtHeaderSubtitle != null) TxtHeaderSubtitle.Text = isPl ? "Automatyczna synchronizacja kolekcji i meczów" : "Automatic collection & match sync";
            if (BtnOpenWebDashboard != null) BtnOpenWebDashboard.Content = isPl ? "🌐 Otwórz Web Dashboard" : "🌐 Open Web Dashboard";
            
            bool gameFound = !string.IsNullOrEmpty(config?.HearthstonePath) && Directory.Exists(config.HearthstonePath);
            if (TxtGamePathDisplay != null) TxtGamePathDisplay.Text = gameFound ? config.HearthstonePath : (isPl ? "Nie znaleziono Hearthstone" : "Hearthstone not found");
            if (TxtGameDetected != null) TxtGameDetected.Text = gameFound ? (isPl ? "✓ Wykryto" : "✓ Detected") : (isPl ? "✗ Brak" : "✗ Missing");
            
            if (TxtCollectionHeader != null) TxtCollectionHeader.Text = isPl ? "Synchronizacja Kolekcji" : "Collection Sync";
            if (TxtCollectionStatus != null) TxtCollectionStatus.Text = isPl ? "Wejdź do kolekcji w grze — synchronizacja nastąpi automatycznie" : "Open collection in game — sync happens automatically";
            if (RunInstructions != null) RunInstructions.Text = isPl ? "Instrukcja ❓" : "Instructions ❓";
            if (BtnSyncFull != null) BtnSyncFull.Content = isPl ? "📥 Synchronizuj kolekcję" : "📥 Sync Collection";
            if (TxtLaunchBtnLabel != null) TxtLaunchBtnLabel.Text = isPl ? "Uruchom grę" : "Launch Game";

            if (TxtStat1Label != null) TxtStat1Label.Text = isPl ? "GRA" : "GAME";
            if (TxtStatGame != null) TxtStatGame.Text = gameFound ? (isPl ? "Wykryta" : "Detected") : (isPl ? "Nie znaleziono" : "Not found");
            
            if (TxtStat2Label != null) TxtStat2Label.Text = isPl ? "KOLEKCJA" : "COLLECTION";
            if (TxtStat3Label != null) TxtStat3Label.Text = isPl ? "ŚLEDZENIE" : "TRACKING";
            if (TxtStatTracking != null) TxtStatTracking.Text = gameFound ? (isPl ? "Aktywne" : "Active") : (isPl ? "Nieaktywne" : "Inactive");
            if (TxtStatus != null) TxtStatus.Text = gameFound ? (isPl ? "Oczekiwanie na mecz" : "Waiting for match") : (isPl ? "Gra nie wykryta" : "Game not found");

            // Stat cards
            if (TxtStatCollection != null && !string.IsNullOrEmpty(TxtStatCollection.Text))
            {
                string match = System.Text.RegularExpressions.Regex.Match(TxtStatCollection.Text, @"\d+").Value;
                if (!string.IsNullOrEmpty(match))
                {
                    TxtStatCollection.Text = isPl ? $"{match} kart" : $"{match} cards";
                }
            }

            // Modal instructions
            if (TxtInstTitle != null) TxtInstTitle.Text = isPl ? "Instrukcja synchronizacji kolekcji" : "Collection Sync Instructions";
            if (TxtInstIntro != null) TxtInstIntro.Text = isPl ? "Aby automatycznie zsynchronizować 100% swoich posiadanych kart w mgnieniu oka, korzystamy z technologii pamięci gry Hearthstone." : "To automatically sync 100% of your owned cards instantly, we utilize Hearthstone game memory scanner.";
            if (TxtInstStep1Title != null) TxtInstStep1Title.Text = isPl ? "Krok 1: Wymagany HDT" : "Step 1: HDT Required";
            if (TxtInstStep1Body != null) TxtInstStep1Body.Text = isPl ? "Upewnij się, że masz zainstalowany darmowy oficjalny Hearthstone Deck Tracker (HDT):" : "Make sure you have the free official Hearthstone Deck Tracker (HDT) installed:";
            if (BtnDownloadHDT != null) BtnDownloadHDT.Content = isPl ? "Pobierz Hearthstone Deck Tracker" : "Download Hearthstone Deck Tracker";
            if (TxtInstStep2Title != null) TxtInstStep2Title.Text = isPl ? "Krok 2: Uruchomienie" : "Step 2: Launch";
            if (TxtInstStep2Body != null) TxtInstStep2Body.Text = isPl ? "1. Włącz grę Hearthstone.\n2. Wejdź w menu głównym w zakładkę \"Moja Kolekcja\".\n3. Synchronizacja z aplikacją Web nastąpi automatycznie, lub kliknij \"Synchronizuj kolekcję\"." : "1. Launch Hearthstone.\n2. Open \"My Collection\" in the main menu.\n3. Sync with Web App will happen automatically, or click \"Sync Collection\".";
            if (BtnCloseInstructions != null) BtnCloseInstructions.Content = isPl ? "Zrozumiałem, zamknij" : "Understood, close";
        }

        private async void ComboLocale_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboLocale.SelectedItem is ComboBoxItem item && cardDb != null)
            {
                string newLocale = item.Tag.ToString();
                UpdateLanguageUI(newLocale);
                if (newLocale != cardDb.CurrentLocale)
                {
                    LogToConsole($"Zmieniono język na {newLocale}. Pobieranie nowej bazy danych...");
                    try
                    {
                        await cardDb.InitializeAsync(newLocale);
                        LogToConsole("Nowa baza danych gotowa.");
                        SaveConfig();
                        
                        // Re-import if there is a deck code
                        if (!string.IsNullOrEmpty(TxtDeckCode.Text))
                        {
                            BtnImport_Click(this, new RoutedEventArgs());
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToConsole($"Błąd ładowania nowego języka: {ex.Message}");
                    }
                }
            }
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            string deckstring = TxtDeckCode.Text.Trim();
            await LoadDeckFromCode(deckstring);
        }

        private async Task LoadDeckFromCode(string deckstring)
        {
            currentDeckList.Clear();

            if (!string.IsNullOrEmpty(deckstring))
            {
                try
                {
                    LogToConsole("Dekodowanie kodu talii...");
                    var parsedDeck = DeckstringParser.Parse(deckstring);
                    LogToConsole($"Zdekodowano talię (Format: {parsedDeck.Format}, Liczba kart: {parsedDeck.CardCounts.Values.Sum()})");

                    var list = new List<DeckItemViewModel>();
                    foreach (var pair in parsedDeck.CardCounts)
                    {
                        var cardInfo = cardDb.GetCardByDbfId(pair.Key);
                        if (cardInfo != null)
                        {
                            var vm = new DeckItemViewModel
                            {
                                DbfId = cardInfo.DbfId,
                                CardId = cardInfo.Id,
                                Name = cardInfo.Name,
                                Cost = cardInfo.Cost,
                                Rarity = cardInfo.Rarity,
                                Count = pair.Value
                            };
                            list.Add(vm);
                        }
                        else
                        {
                            LogToConsole($"Ostrzeżenie: Nie znaleziono karty o DBF ID {pair.Key} w bazie.");
                        }
                    }

                    var sortedList = list.OrderBy(c => c.Cost).ThenBy(c => c.Name).ToList();
                    foreach (var item in sortedList)
                    {
                        currentDeckList.Add(item);
                    }

                    foreach (var item in currentDeckList)
                    {
                        LoadTileAsync(item);
                    }
                }
                catch (Exception ex)
                {
                    LogToConsole($"Błąd importowania talii: {ex.Message}");
                    return;
                }
            }

            // Save deck code to config immediately
            if (config != null && !string.IsNullOrEmpty(deckstring))
            {
                config.LastDeckCode = deckstring;
                SaveConfig();
            }

            // If the overlay is already active, update its deck list immediately!
            if (overlayWindow != null && overlayWindow.IsLoaded)
            {
                overlayWindow.UpdateDeck(currentDeckList.ToList());
                LogToConsole("Zaktualizowano listę kart w aktywnej nakładce.");
            }
            else
            {
                // Open overlay and widget if HS path is set
                string hsPath = TxtHsPath.Text.Trim();
                if (!string.IsNullOrEmpty(hsPath) && Directory.Exists(hsPath))
                {
                    if (!logWatcher.IsRunning)
                    {
                        logWatcher.Start(hsPath);
                    }

                    overlayWindow = new OverlayWindow(cardDb, currentDeckList.ToList());
                    overlayWindow.DebugLog = LogToConsole;
                    overlayWindow.Show();

                    if (opponentOverlayWindow != null)
                    {
                        opponentOverlayWindow.Close();
                    }
                    opponentOverlayWindow = new OpponentOverlayWindow(cardDb);
                    opponentOverlayWindow.Show();

                    if (widgetWindow != null)
                    {
                        widgetWindow.Close();
                    }

                    widgetWindow = new WidgetWindow(() =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            logWatcher.Stop();
                            if (overlayWindow != null)
                            {
                                overlayWindow.Close();
                                overlayWindow = null;
                            }
                            if (opponentOverlayWindow != null)
                            {
                                opponentOverlayWindow.Close();
                                opponentOverlayWindow = null;
                            }
                            widgetWindow = null;
                            BorderStatus.Background = System.Windows.Media.Brushes.Gray;
                            TxtStatus.Text = "Śledzenie zatrzymane";
                            LogToConsole("Wyłączono śledzenie przez nakładkę widgetu.");
                        });
                    });
                    widgetWindow.Show();

                    BorderStatus.Background = System.Windows.Media.Brushes.Orange;
                    TxtStatus.Text = "Śledzenie: Oczekiwanie na mecz";
                    LogToConsole("Włączono nakładki gracza, przeciwnika i widget statusu.");
                }
                else
                {
                    LogToConsole("Talia załadowana. Nakładki otworzą się automatycznie po wejściu do gry.");
                }
            }
        }

        private async void LoadTileAsync(DeckItemViewModel item)
        {
            string path = await cardDb.GetTilePathAsync(item.CardId);
            if (path != null)
            {
                item.TilePath = path;
            }
        }

        private void LogWatcher_OnStatusMessage(object sender, string msg)
        {
            LogToConsole(msg);
        }

        private void LogWatcher_OnGameStart(object sender, EventArgs e)
        {
            LogToConsole("Nowy mecz rozpoczęty! Automatyczne inicjowanie nakładki.");
            Dispatcher.Invoke(() =>
            {
                BorderStatus.Background = System.Windows.Media.Brushes.Green;
                TxtStatus.Text = "Mecz w toku";

                // Open overlay if not open yet
                if (overlayWindow == null || !overlayWindow.IsLoaded)
                {
                    overlayWindow = new OverlayWindow(cardDb, currentDeckList.ToList());
                    overlayWindow.DebugLog = LogToConsole;
                    overlayWindow.Show();
                    LogToConsole("Nakładka kart gracza wyświetlona.");
                }
                else
                {
                    overlayWindow.Visibility = System.Windows.Visibility.Visible;
                }

                // Open opponent overlay if not open yet
                if (opponentOverlayWindow == null || !opponentOverlayWindow.IsLoaded)
                {
                    opponentOverlayWindow = new OpponentOverlayWindow(cardDb);
                    opponentOverlayWindow.Show();
                    LogToConsole("Nakładka kart przeciwnika wyświetlona.");
                }
                else
                {
                    opponentOverlayWindow.Visibility = System.Windows.Visibility.Visible;
                }

                // Open widget if not open yet
                if (widgetWindow == null || !widgetWindow.IsLoaded)
                {
                    widgetWindow = new WidgetWindow(() =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            logWatcher.Stop();
                            if (overlayWindow != null) { overlayWindow.Close(); overlayWindow = null; }
                            if (opponentOverlayWindow != null) { opponentOverlayWindow.Close(); opponentOverlayWindow = null; }
                            widgetWindow = null;
                            BorderStatus.Background = System.Windows.Media.Brushes.Gray;
                            TxtStatus.Text = "Śledzenie zatrzymane";
                            LogToConsole("Wyłączono śledzenie przez nakładkę widgetu.");
                        });
                    });
                    widgetWindow.Show();
                }
                else
                {
                    widgetWindow.Visibility = System.Windows.Visibility.Visible;
                }

                overlayWindow.ResetMatch();
                opponentOverlayWindow.Reset();
            });
        }

        private void LogWatcher_OnGameEnd(object sender, EventArgs e)
        {
            LogToConsole("Mecz zakończony — ukrywam nakładki.");
            Dispatcher.Invoke(() =>
            {
                if (overlayWindow != null && overlayWindow.IsLoaded)
                    overlayWindow.Visibility = System.Windows.Visibility.Hidden;

                if (opponentOverlayWindow != null && opponentOverlayWindow.IsLoaded)
                    opponentOverlayWindow.Visibility = System.Windows.Visibility.Hidden;

                if (widgetWindow != null && widgetWindow.IsLoaded)
                    widgetWindow.Visibility = System.Windows.Visibility.Hidden;

                BorderStatus.Background = System.Windows.Media.Brushes.Orange;
                TxtStatus.Text = "Śledzenie: Oczekiwanie na mecz";
            });
        }

        private void LogWatcher_OnCardTransition(object sender, CardTransitionEventArgs e)
        {
            LogToConsole($"[Karta] {e.Name} ({e.CardId}) z {e.FromZone} do {e.ToZone} (Ja: {e.IsFriendly})");
            
            if (e.IsFriendly)
            {
                if (overlayWindow != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        overlayWindow.ProcessCardTransition(e);
                    });
                }
            }
            else
            {
                // Opponent card play / reveal (when it goes to Play or Graveyard)
                if (e.ToZone == "PLAY" || e.ToZone == "GRAVEYARD")
                {
                    if (!string.IsNullOrEmpty(e.CardId) || !string.IsNullOrEmpty(e.Name))
                    {
                        if (opponentOverlayWindow != null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                opponentOverlayWindow.ProcessOpponentCard(e.CardId, e.Name);
                            });
                        }
                    }
                }
            }
        }

        private void LogWatcher_OnDeckDetected(object sender, DeckDetectedEventArgs e)
        {
            LogToConsole($"[Auto-Talia] Wykryto tali\u0119 '{e.DeckName}' z Decks.log \u2014 \u0142aduj\u0119 automatycznie...");

            Dispatcher.Invoke(async () =>
            {
                try
                {
                    var parsedDeck = DeckstringParser.Parse(e.Deckstring);
                    var list = new List<DeckItemViewModel>();

                    foreach (var pair in parsedDeck.CardCounts)
                    {
                        var cardInfo = cardDb.GetCardByDbfId(pair.Key);
                        if (cardInfo != null)
                        {
                            list.Add(new DeckItemViewModel
                            {
                                DbfId = cardInfo.DbfId,
                                CardId = cardInfo.Id,
                                Name = cardInfo.Name,
                                Cost = cardInfo.Cost,
                                Rarity = cardInfo.Rarity,
                                Count = pair.Value
                            });
                        }
                        else
                        {
                            LogToConsole($"[Auto-Talia] Nieznana karta DBF ID: {pair.Key}");
                        }
                    }

                    if (list.Count == 0)
                    {
                        LogToConsole("[Auto-Talia] Nie rozpoznano \u017cadnych kart \u2014 baza danych mog\u0142a si\u0119 jeszcze nie za\u0142adowa\u0107.");
                        return;
                    }

                    var sorted = list.OrderBy(c => c.Cost).ThenBy(c => c.Name).ToList();

                    currentDeckList.Clear();
                    foreach (var item in sorted)
                    {
                        currentDeckList.Add(item);
                        LoadTileAsync(item);
                    }

                    LogToConsole($"[Auto-Talia] Za\u0142adowano {sorted.Count} unikalnych kart ({sorted.Sum(c => c.Count)} \u0142\u0105cznie). Talia gotowa!");

                    // Push to overlay if already open
                    if (overlayWindow != null && overlayWindow.IsLoaded)
                    {
                        overlayWindow.UpdateDeck(currentDeckList.ToList());
                        LogToConsole("[Auto-Talia] Zaktualizowano nak\u0142adk\u0119 kart gracza.");
                    }
                }
                catch (Exception ex)
                {
                    LogToConsole($"[Auto-Talia] B\u0142\u0105d \u0142adowania talii: {ex.Message}");
                }
            });
        }
    }
}