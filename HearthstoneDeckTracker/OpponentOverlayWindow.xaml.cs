using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class OpponentOverlayWindow : Window
    {
        private CardDatabase db;
        private ObservableCollection<OverlayCardItem> playedCards = new ObservableCollection<OverlayCardItem>();
        private DispatcherTimer positionTimer;
        private int totalPlayedCount = 0;

        public OpponentOverlayWindow(CardDatabase database)
        {
            InitializeComponent();
            db = database;
            
            TxtOpponentHeader.Text = db?.CurrentLocale == "plPL" ? "KARTY PRZECIWNIKA" : "OPPONENT CARDS";
            TxtOpponentCount.Text = db?.CurrentLocale == "plPL" ? "0 kart" : "0 cards";

            ItemsPlayedList.ItemsSource = playedCards;

            positionTimer = new DispatcherTimer();
            positionTimer.Interval = TimeSpan.FromMilliseconds(200);
            positionTimer.Tick += PositionTimer_Tick;
            positionTimer.Start();

            UpdatePosition();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            Win32.SetWindowToolWindow(helper.Handle);
        }


        public void Reset()
        {
            playedCards.Clear();
            totalPlayedCount = 0;
            TxtOpponentCount.Text = "0 kart";
        }

        public void ProcessOpponentCard(string cardId, string name)
        {
            OverlayCardItem targetCard = null;

            if (!string.IsNullOrEmpty(cardId))
            {
                targetCard = playedCards.FirstOrDefault(c => c.CardId == cardId);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                targetCard = playedCards.FirstOrDefault(c => c.Name == name);
            }

            if (targetCard != null)
            {
                targetCard.Count++;
            }
            else
            {
                CardInfo cardInfo = db.GetCardById(cardId);
                
                // If it's a spell or hero card or minion, we map details
                string cardName = !string.IsNullOrEmpty(name) ? name : (cardInfo != null ? cardInfo.Name : (db?.CurrentLocale == "plPL" ? "Karta" : "Card"));
                int cardCost = cardInfo != null ? cardInfo.Cost : 0;
                string cardRarity = cardInfo != null ? cardInfo.Rarity : "FREE";

                var newItem = new OverlayCardItem
                {
                    DbfId = cardInfo != null ? cardInfo.DbfId : 0,
                    CardId = cardId,
                    Name = cardName,
                    Cost = cardCost,
                    Rarity = cardRarity,
                    Count = 1
                };
                
                if (cardInfo != null)
                {
                    LoadTileAsync(newItem);
                }

                // Insert in sorted order (Cost then Name)
                int insertIdx = 0;
                while (insertIdx < playedCards.Count && 
                       (playedCards[insertIdx].Cost < newItem.Cost || 
                       (playedCards[insertIdx].Cost == newItem.Cost && string.Compare(playedCards[insertIdx].Name, newItem.Name) < 0)))
                {
                    insertIdx++;
                }
                playedCards.Insert(insertIdx, newItem);
            }

            totalPlayedCount++;
            TxtOpponentCount.Text = db?.CurrentLocale == "plPL" ? $"{totalPlayedCount} kart" : $"{totalPlayedCount} cards";
        }

        private async void LoadTileAsync(OverlayCardItem item)
        {
            string path = await db.GetTilePathAsync(item.CardId);
            if (path != null)
            {
                item.TilePath = path;
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
                var processes = Process.GetProcessesByName("Hearthstone");
                if (processes.Length > 0)
                {
                    var hsProcess = processes[0];
                    IntPtr handle = hsProcess.MainWindowHandle;

                    if (handle != IntPtr.Zero && Win32.GetWindowRect(handle, out Win32.RECT rect))
                    {
                        IntPtr activeHandle = Win32.GetForegroundWindow();
                        bool isHearthstoneActive = (activeHandle == handle);

                        if (isHearthstoneActive)
                        {
                            double scaleX = 1.0;
                            double scaleY = 1.0;
                            var source = PresentationSource.FromVisual(this);
                            if (source != null && source.CompositionTarget != null)
                            {
                                scaleX = source.CompositionTarget.TransformToDevice.M11;
                                scaleY = source.CompositionTarget.TransformToDevice.M22;
                            }

                            double hsLeft = rect.Left / scaleX;
                            double hsTop = rect.Top / scaleY;
                            double hsBottom = rect.Bottom / scaleY;
                            double hsHeight = hsBottom - hsTop;

                            // Position on the left side of the window (below the widget status)
                            this.Left = hsLeft + 15;
                            this.Top = hsTop + 110; // 60 (widget start) + 40 (widget height) + 10 (spacing)
                            this.Height = hsHeight - 170; // adjusted to leave room at the bottom

                            if (this.Visibility != Visibility.Visible)
                            {
                                this.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            if (this.Visibility == Visibility.Visible)
                            {
                                this.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    else
                    {
                        if (this.Visibility == Visibility.Visible)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
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

