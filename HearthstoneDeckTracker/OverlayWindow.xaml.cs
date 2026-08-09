using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace HearthstoneDeckTracker
{
    public class OverlayCardItem : INotifyPropertyChanged
    {
        private int count;
        private string drawChanceText = "0.0%";
        private string tilePath;

        public int DbfId { get; set; }
        public string CardId { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public string Rarity { get; set; }

        public int Count
        {
            get => count;
            set
            {
                if (count != value)
                {
                    count = value;
                    OnPropertyChanged(nameof(Count));
                    OnPropertyChanged(nameof(CountText));
                    OnPropertyChanged(nameof(CardOpacity));
                }
            }
        }

        public string DrawChanceText
        {
            get => drawChanceText;
            set
            {
                if (drawChanceText != value)
                {
                    drawChanceText = value;
                    OnPropertyChanged(nameof(DrawChanceText));
                }
            }
        }

        public string TilePath
        {
            get => tilePath;
            set
            {
                if (tilePath != value)
                {
                    tilePath = value;
                    OnPropertyChanged(nameof(TilePath));
                }
            }
        }

        public string CountText => Count > 1 ? $"x{Count}" : (Count == 0 ? "x0" : (Rarity == "LEGENDARY" ? "★" : "x1"));

        public double CardOpacity => Count > 0 ? 1.0 : 0.35;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class OverlayWindow : Window
    {
        private CardDatabase db;
        private List<DeckItemViewModel> startingDeck;
        private ObservableCollection<OverlayCardItem> remainingDeck = new ObservableCollection<OverlayCardItem>();
        
        private DispatcherTimer positionTimer;
        private int totalRemainingCards = 30;
        private int playedCardsCount = 0;
        private bool hasStartingDeck;

        public OverlayWindow(CardDatabase database, List<DeckItemViewModel> deck)
        {
            InitializeComponent();
            
            db = database;
            startingDeck = deck;
            hasStartingDeck = deck.Count > 0;
            
            TxtDeckHeader.Text = hasStartingDeck 
                ? (db?.CurrentLocale == "plPL" ? "TALIA GRACZA" : "PLAYER DECK") 
                : (db?.CurrentLocale == "plPL" ? "DOBRANE KARTY" : "DRAWN CARDS");


            ItemsDeckList.ItemsSource = remainingDeck;

            ResetMatch();

            // Set up timer to track Hearthstone window position
            positionTimer = new DispatcherTimer();
            positionTimer.Interval = TimeSpan.FromMilliseconds(200);
            positionTimer.Tick += PositionTimer_Tick;
            positionTimer.Start();

            Loaded += OverlayWindow_Loaded;
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Position overlay initially
            UpdatePosition();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Set tool window style (allows mouse hover and preview popups)
            var helper = new WindowInteropHelper(this);
            Win32.SetWindowToolWindow(helper.Handle);
        }


        public void ResetMatch()
        {
            remainingDeck.Clear();
            playedCardsCount = 0;
            totalRemainingCards = 0;

            if (hasStartingDeck)
            {
                foreach (var card in startingDeck)
                {
                    remainingDeck.Add(new OverlayCardItem
                    {
                        DbfId = card.DbfId,
                        CardId = card.CardId,
                        Name = card.Name,
                        Cost = card.Cost,
                        Rarity = card.Rarity,
                        Count = card.Count,
                        TilePath = card.TilePath
                    });
                    totalRemainingCards += card.Count;
                }
            }

            RecalculateChances();
        }

        public void UpdateDeck(List<DeckItemViewModel> deck)
        {
            startingDeck = deck;
            hasStartingDeck = deck.Count > 0;
            
            TxtDeckHeader.Text = hasStartingDeck ? "TALIA GRACZA" : "DOBRANE KARTY";

            
            ResetMatch();
        }

        public Action<string> DebugLog { get; set; }

        private OverlayCardItem FindCardInDeck(string cardId, string cardName)
        {
            if (!string.IsNullOrEmpty(cardId))
            {
                var exact = remainingDeck.FirstOrDefault(c => c.CardId == cardId);
                if (exact != null) return exact;

                // Handle CORE_ prefix differences (e.g. CORE_CS2_032 vs CS2_032)
                string normId = cardId.StartsWith("CORE_") ? cardId.Substring(5) : "CORE_" + cardId;
                var coreMatch = remainingDeck.FirstOrDefault(c => c.CardId == normId);
                if (coreMatch != null) return coreMatch;
            }

            if (!string.IsNullOrEmpty(cardName))
            {
                var nameMatch = remainingDeck.FirstOrDefault(c => string.Equals(c.Name, cardName, StringComparison.OrdinalIgnoreCase));
                if (nameMatch != null) return nameMatch;
            }

            return null;
        }

        public void ProcessCardTransition(CardTransitionEventArgs e)
        {
            // We only track friendly transitions affecting the deck
            if (!e.IsFriendly) return;

            DebugLog?.Invoke($"[Overlay] CardTransition: ID={e.CardId}, Name={e.Name}, From={e.FromZone}, To={e.ToZone}, hasStartingDeck={hasStartingDeck}, remCount={remainingDeck.Count}");

            if (hasStartingDeck)
            {
                // Trigger: card moves out of deck (drawn, summoned directly, burned)
                // FromZone=="INVALID" is used by Zone.log for initial deals (mulligan hand)
                if ((e.FromZone == "DECK" || e.FromZone == "INVALID") && (e.ToZone == "HAND" || e.ToZone == "PLAY" || e.ToZone == "GRAVEYARD"))
                {
                    OverlayCardItem targetCard = FindCardInDeck(e.CardId, e.Name);

                    if (targetCard != null)
                    {
                        targetCard.Count--;
                        totalRemainingCards--;
                        playedCardsCount++;

                        if (targetCard.Count <= 0)
                        {
                            remainingDeck.Remove(targetCard);
                        }

                        RecalculateChances();
                        DebugLog?.Invoke($"[Overlay] Usunięto/zmniejszono kartę: {targetCard.Name} (zostało: {targetCard.Count}, w talii: {totalRemainingCards})");
                    }
                    else
                    {
                        DebugLog?.Invoke($"[Overlay] Nie znaleziono karty do usunięcia z talii: ID={e.CardId}, Name={e.Name}");
                    }
                }
                // Trigger: card is put back in deck (e.g. mulligan, shuffle back)
                else if (e.ToZone == "DECK" && (e.FromZone == "HAND" || e.FromZone == "PLAY" || e.FromZone == "SETASIDE" || e.FromZone == "INVALID"))
                {
                    OverlayCardItem targetCard = FindCardInDeck(e.CardId, e.Name);

                    if (targetCard != null)
                    {
                        targetCard.Count++;
                    }
                    else
                    {
                        // Recreate from starting deck if it was removed because count was 0
                        var startItem = startingDeck.FirstOrDefault(c => c.CardId == e.CardId || string.Equals(c.Name, e.Name, StringComparison.OrdinalIgnoreCase));
                        if (startItem != null)
                        {
                            var newItem = new OverlayCardItem
                            {
                                DbfId = startItem.DbfId,
                                CardId = startItem.CardId,
                                Name = startItem.Name,
                                Cost = startItem.Cost,
                                Rarity = startItem.Rarity,
                                Count = 1,
                                TilePath = startItem.TilePath
                            };

                            int insertIdx = 0;
                            while (insertIdx < remainingDeck.Count && 
                                   (remainingDeck[insertIdx].Cost < newItem.Cost || 
                                   (remainingDeck[insertIdx].Cost == newItem.Cost && string.Compare(remainingDeck[insertIdx].Name, newItem.Name, StringComparison.OrdinalIgnoreCase) < 0)))
                            {
                                insertIdx++;
                            }
                            remainingDeck.Insert(insertIdx, newItem);
                        }
                        else
                        {
                            // Card was not originally in the starting deck (created/shuffled card)
                            CardInfo cardInfo = db.GetCardById(e.CardId) ?? db.GetCardByDbfId(0);
                            if (cardInfo != null)
                            {
                                var newItem = new OverlayCardItem
                                {
                                    DbfId = cardInfo.DbfId,
                                    CardId = cardInfo.Id,
                                    Name = cardInfo.Name,
                                    Cost = cardInfo.Cost,
                                    Rarity = cardInfo.Rarity,
                                    Count = 1
                                };
                                LoadTileAsync(newItem);
                                
                                int insertIdx = 0;
                                while (insertIdx < remainingDeck.Count && 
                                       (remainingDeck[insertIdx].Cost < newItem.Cost || 
                                       (remainingDeck[insertIdx].Cost == newItem.Cost && string.Compare(remainingDeck[insertIdx].Name, newItem.Name, StringComparison.OrdinalIgnoreCase) < 0)))
                                {
                                    insertIdx++;
                                }
                                remainingDeck.Insert(insertIdx, newItem);
                            }
                        }
                    }

                    totalRemainingCards++;
                    if (playedCardsCount > 0) playedCardsCount--;
                    RecalculateChances();
                    DebugLog?.Invoke($"[Overlay] Dopisano/wtasowano kartę: {e.Name ?? e.CardId} (w talii: {totalRemainingCards})");
                }
            }

            else
            {
                // Mode: Drawn cards tracking (starting deck was empty)
                // FromZone=="INVALID" is used by Zone.log for initial deals
                if ((e.FromZone == "DECK" || e.FromZone == "INVALID") && (e.ToZone == "HAND" || e.ToZone == "PLAY" || e.ToZone == "GRAVEYARD"))
                {
                    string cardIdToMatch = e.CardId;
                    OverlayCardItem targetCard = null;
                    if (!string.IsNullOrEmpty(cardIdToMatch))
                    {
                        targetCard = remainingDeck.FirstOrDefault(c => c.CardId == cardIdToMatch);
                    }
                    else if (!string.IsNullOrEmpty(e.Name))
                    {
                        targetCard = remainingDeck.FirstOrDefault(c => c.Name == e.Name);
                    }

                    if (targetCard != null)
                    {
                        targetCard.Count++;
                    }
                    else
                    {
                        CardInfo cardInfo = db.GetCardById(e.CardId);
                        string cardName = !string.IsNullOrEmpty(e.Name) ? e.Name : (cardInfo != null ? cardInfo.Name : "Nieznana Karta");
                        int cardCost = cardInfo != null ? cardInfo.Cost : 0;
                        string cardRarity = cardInfo != null ? cardInfo.Rarity : "COMMON";
                        
                        var newItem = new OverlayCardItem
                        {
                            DbfId = cardInfo != null ? cardInfo.DbfId : 0,
                            CardId = e.CardId,
                            Name = cardName,
                            Cost = cardCost,
                            Rarity = cardRarity,
                            Count = 1
                        };
                        if (cardInfo != null)
                        {
                            LoadTileAsync(newItem);
                        }
                        
                        int insertIdx = 0;
                        while (insertIdx < remainingDeck.Count && 
                               (remainingDeck[insertIdx].Cost < newItem.Cost || 
                               (remainingDeck[insertIdx].Cost == newItem.Cost && string.Compare(remainingDeck[insertIdx].Name, newItem.Name) < 0)))
                        {
                            insertIdx++;
                        }
                        remainingDeck.Insert(insertIdx, newItem);
                    }

                    playedCardsCount++;
                    RecalculateChances();
                }
                else if (e.ToZone == "DECK" && (e.FromZone == "HAND" || e.FromZone == "PLAY" || e.FromZone == "SETASIDE" || e.FromZone == "INVALID"))
                {
                    // Card was put back into the deck, decrement its drawn count
                    string cardIdToMatch = e.CardId;
                    OverlayCardItem targetCard = null;
                    if (!string.IsNullOrEmpty(cardIdToMatch))
                    {
                        targetCard = remainingDeck.FirstOrDefault(c => c.CardId == cardIdToMatch);
                    }
                    else if (!string.IsNullOrEmpty(e.Name))
                    {
                        targetCard = remainingDeck.FirstOrDefault(c => c.Name == e.Name);
                    }

                    if (targetCard != null)
                    {
                        targetCard.Count--;
                        if (targetCard.Count <= 0)
                        {
                            remainingDeck.Remove(targetCard);
                        }
                    }

                    if (playedCardsCount > 0) playedCardsCount--;
                    RecalculateChances();
                }
            }
        }

        private async void LoadTileAsync(OverlayCardItem item)
        {
            string path = await db.GetTilePathAsync(item.CardId);
            if (path != null)
            {
                item.TilePath = path;
            }
        }

        private void RecalculateChances()
        {
            if (hasStartingDeck)
            {
                TxtDeckCount.Text = $"{totalRemainingCards}/{startingDeck.Sum(c => c.Count)}";

                if (totalRemainingCards > 0)
                {
                    foreach (var card in remainingDeck)
                    {
                        double cardDrawChance = card.Count > 0 ? ((double)card.Count / totalRemainingCards) * 100 : 0.0;
                        card.DrawChanceText = card.Count > 0 ? $"{cardDrawChance:F1}%" : "0.0%";
                    }
                }
                else
                {
                    foreach (var card in remainingDeck)
                    {
                        card.DrawChanceText = "0.0%";
                    }
                }
            }
            else
            {
                TxtDeckCount.Text = $"{playedCardsCount} kart";
                foreach (var card in remainingDeck)
                {
                    card.DrawChanceText = ""; // Clear chances in drawn cards mode
                }
            }
        }


        private void PositionTimer_Tick(object sender, EventArgs e)
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            try
            {
                // Find Hearthstone process
                var processes = Process.GetProcessesByName("Hearthstone");
                if (processes.Length > 0)
                {
                    var hsProcess = processes[0];
                    IntPtr handle = hsProcess.MainWindowHandle;

                    if (handle != IntPtr.Zero && Win32.GetWindowRect(handle, out Win32.RECT rect))
                    {
                        // Check if Hearthstone is active window
                        IntPtr activeHandle = Win32.GetForegroundWindow();
                        bool isHearthstoneActive = (activeHandle == handle);

                        if (isHearthstoneActive)
                        {
                            // Get DPI scaling
                            double scaleX = 1.0;
                            double scaleY = 1.0;
                            var source = PresentationSource.FromVisual(this);
                            if (source != null && source.CompositionTarget != null)
                            {
                                scaleX = source.CompositionTarget.TransformToDevice.M11;
                                scaleY = source.CompositionTarget.TransformToDevice.M22;
                            }

                            // Convert physical screen coordinates to WPF logical pixels
                            double hsLeft = rect.Left / scaleX;
                            double hsRight = rect.Right / scaleX;
                            double hsTop = rect.Top / scaleY;
                            double hsBottom = rect.Bottom / scaleY;
                            double hsHeight = hsBottom - hsTop;

                            // Position overlay on the right side of the window
                            this.Left = hsRight - this.Width - 15;
                            this.Top = hsTop + 60;
                            this.Height = hsHeight - 120;

                            if (this.Visibility != Visibility.Visible)
                            {
                                this.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            // Hide overlay if Hearthstone is not in focus
                            if (this.Visibility == Visibility.Visible)
                            {
                                this.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    else
                    {
                        // Window not ready or rect failed
                        if (this.Visibility == Visibility.Visible)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    // Hearthstone not running
                    if (this.Visibility == Visibility.Visible)
                    {
                        this.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch
            {
                // Prevent crash during process lookup
            }
        }

        private async void CardRow_MouseEnter(object sender, MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            var cardItem = element?.DataContext as OverlayCardItem;
            if (cardItem == null || string.IsNullOrEmpty(cardItem.CardId)) return;

            CardPreviewPopup.PlacementTarget = element;
            ImgCardPreview.Source = null;
            CardPreviewPopup.IsOpen = true;

            string renderPath = await db.GetCardRenderPathAsync(cardItem.CardId);
            if (!string.IsNullOrEmpty(renderPath) && File.Exists(renderPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(renderPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImgCardPreview.Source = bitmap;
                }
                catch { }
            }
        }


        private void CardRow_MouseLeave(object sender, MouseEventArgs e)
        {
            CardPreviewPopup.IsOpen = false;
        }
    }
}

