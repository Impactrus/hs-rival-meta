using System.Collections.Generic;
using UnityEngine;
using CCG.UI;

namespace CCG.Core
{
    public class GameDebugger : MonoBehaviour
    {
        [Header("Mock Database of Available Cards in the Game")]
        public List<CardData> allGameCards = new List<CardData>();

        [Header("UI Screen Reference")]
        [SerializeField] private ScreenType activeDebugScreen = ScreenType.Login;

        // Login inputs
        private string loginUsername = "Gracz1";
        private string loginPassword = "password";

        // Deckbuilder state
        private SerializableDeck editingDeck;
        private string newDeckName = "Nowy Deck";

        // Gemini Test State
        private string geminiPrompt = "Opowiedz jednozdaniową legendę o tej karczmie.";
        private string geminiResponse = "";

        private void Start()
        {
            GameManager.OnGameLog += Debug.Log;
            GenerateAllGameCards();
            
            // Connect to ScreenManager if present
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.SwitchToScreen(ScreenType.Login);
            }
        }

        private void OnDestroy()
        {
            GameManager.OnGameLog -= Debug.Log;
        }

        private void GenerateAllGameCards()
        {
            allGameCards.Clear();
            
            // Generate a global mock catalog of cards
            for (int i = 1; i <= 20; i++)
            {
                CardData card = ScriptableObject.CreateInstance<CardData>();
                card.cardId = $"Card_{i}";
                card.cardName = $"Stronnik #{i}";
                card.description = $"Zwykły stronnik o koszcie {Mathf.CeilToInt(i/3f)}.";
                card.cardType = CardType.Minion;
                card.manaCost = Mathf.Clamp(Mathf.CeilToInt(i / 3f), 1, 10);
                card.attack = Mathf.Clamp(i / 2, 1, 8);
                card.maxHealth = Mathf.Clamp(i, 2, 10);

                allGameCards.Add(card);
            }

            // Spells
            for (int i = 1; i <= 5; i++)
            {
                CardData card = ScriptableObject.CreateInstance<CardData>();
                card.cardId = $"Spell_{i}";
                card.cardName = $"Czar Mocy #{i}";
                card.description = $"Zadaje obrażenia lub leczy.";
                card.cardType = CardType.Spell;
                card.manaCost = i;

                allGameCards.Add(card);
            }
        }

        private CardData FindCardDataById(string id)
        {
            return allGameCards.Find(c => c.cardId == id);
        }

        private void SwitchScreen(ScreenType screen)
        {
            activeDebugScreen = screen;
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.SwitchToScreen(screen);
            }
        }

        private void OnGUI()
        {
            // Set up a large styling block
            GUI.skin.button.fontSize = 14;
            GUI.skin.label.fontSize = 14;
            GUI.skin.box.fontSize = 14;

            GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
            GUILayout.Label($"<b>HEARTHSTONE CLONE MOCKUP MENU</b>", GUILayout.Height(30));
            GUILayout.Space(10);

            switch (activeDebugScreen)
            {
                case ScreenType.Login:
                    DrawLoginGUI();
                    break;
                case ScreenType.MainMenu:
                    DrawMainMenuGUI();
                    break;
                case ScreenType.Collection:
                    DrawCollectionGUI();
                    break;
                case ScreenType.Shop:
                    DrawShopGUI();
                    break;
                case ScreenType.Matchmaking:
                    DrawMatchmakingGUI();
                    break;
                case ScreenType.Gameplay:
                    DrawGameplayGUI();
                    break;
            }

            GUILayout.EndArea();
        }

        private void DrawLoginGUI()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(350));
            GUILayout.Label("<b>LOGOWANIE / REJESTRACJA LOKALNA</b>");
            GUILayout.Space(10);

            GUILayout.Label("Nazwa użytkownika:");
            loginUsername = GUILayout.TextField(loginUsername, 20);

            GUILayout.Label("Hasło:");
            loginPassword = GUILayout.PasswordField(loginPassword, '*', 20);

            GUILayout.Space(15);
            if (GUILayout.Button("Zaloguj / Stwórz Profil", GUILayout.Height(40)))
            {
                if (PlayerProfileManager.Instance.Login(loginUsername, loginPassword))
                {
                    SwitchScreen(ScreenType.MainMenu);
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawMainMenuGUI()
        {
            var profile = PlayerProfileManager.Instance.CurrentProfile;
            if (profile == null) return;

            GUILayout.BeginVertical("box", GUILayout.Width(400));
            GUILayout.Label($"Witaj, <b>{profile.username}</b>!");
            GUILayout.Label($"Złoto: <b>{profile.gold} szt.</b>");
            GUILayout.Space(20);

            if (GUILayout.Button("GRAJ (Matchmaking)", GUILayout.Height(50)))
            {
                SwitchScreen(ScreenType.Matchmaking);
            }
            GUILayout.Space(10);
            if (GUILayout.Button("KOLEKCJA I TALIE", GUILayout.Height(50)))
            {
                SwitchScreen(ScreenType.Collection);
            }
            GUILayout.Space(10);
            if (GUILayout.Button("SKLEP (Kupuj Pakiety)", GUILayout.Height(50)))
            {
                SwitchScreen(ScreenType.Shop);
            }
            // Gemini Test Section
            GUILayout.Space(15);
            GUILayout.Label("<b>ZAPYTAJ KARCZMARZA (GEMINI API)</b>");
            geminiPrompt = GUILayout.TextField(geminiPrompt, 100);
            if (GUILayout.Button("Zadaj pytanie", GUILayout.Height(30)))
            {
                geminiResponse = "Karczmarz myśli...";
                GeminiConnector.Instance.AskGemini(geminiPrompt, (resp) => {
                    geminiResponse = resp;
                });
            }
            if (!string.IsNullOrEmpty(geminiResponse))
            {
                GUILayout.Box(geminiResponse, GUILayout.Width(380));
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Wyloguj", GUILayout.Height(30)))
            {
                SwitchScreen(ScreenType.Login);
            }
            GUILayout.EndVertical();
        }

        private void DrawCollectionGUI()
        {
            var profile = PlayerProfileManager.Instance.CurrentProfile;
            if (profile == null) return;

            GUILayout.BeginHorizontal();

            // Left Side: Owned Cards Album
            GUILayout.BeginVertical("box", GUILayout.Width(Screen.width * 0.6f));
            GUILayout.Label("<b>TWOJA KOLEKCJA KART</b>");
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            int cardsPerRow = 4;
            int currentCardInRow = 0;

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            foreach (var cardId in profile.ownedCardIds)
            {
                CardData cardData = FindCardDataById(cardId);
                if (cardData == null) continue;

                string buttonLabel = $"{cardData.cardName}\nCost: {cardData.manaCost} (A:{cardData.attack} H:{cardData.maxHealth})";
                
                if (GUILayout.Button(buttonLabel, GUILayout.Width(130), GUILayout.Height(60)))
                {
                    if (editingDeck != null)
                    {
                        editingDeck.cardIds.Add(cardId);
                        PlayerProfileManager.Instance.SaveProfile();
                    }
                }

                currentCardInRow++;
                if (currentCardInRow >= cardsPerRow)
                {
                    currentCardInRow = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            // Right Side: Decks List & Deckbuilder
            GUILayout.BeginVertical("box", GUILayout.Width(Screen.width * 0.3f));
            GUILayout.Label("<b>TALIE (DECKS)</b>");

            if (editingDeck == null)
            {
                foreach (var deck in profile.decks)
                {
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label($"{deck.deckName} ({deck.cardIds.Count} kart)");
                    if (GUILayout.Button("Edytuj", GUILayout.Width(60)))
                    {
                        editingDeck = deck;
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(15);
                newDeckName = GUILayout.TextField(newDeckName, 20);
                if (GUILayout.Button("Utwórz nową talię", GUILayout.Height(30)))
                {
                    var newDeck = new SerializableDeck { deckName = newDeckName, cardIds = new List<string>() };
                    profile.decks.Add(newDeck);
                    PlayerProfileManager.Instance.SaveProfile();
                    editingDeck = newDeck;
                }
            }
            else
            {
                GUILayout.Label($"Edycja: <b>{editingDeck.deckName}</b>");
                GUILayout.Label("Kliknij kartę po lewej stronie, aby ją dodać.");
                GUILayout.Space(10);

                for (int i = editingDeck.cardIds.Count - 1; i >= 0; i--)
                {
                    string cardId = editingDeck.cardIds[i];
                    CardData data = FindCardDataById(cardId);
                    if (data == null) continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(data.cardName);
                    if (GUILayout.Button("Usuń", GUILayout.Width(50)))
                    {
                        editingDeck.cardIds.RemoveAt(i);
                        PlayerProfileManager.Instance.SaveProfile();
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(20);
                if (GUILayout.Button("Zapisz i Wyjdź", GUILayout.Height(35)))
                {
                    editingDeck = null;
                }
            }

            GUILayout.Space(30);
            if (GUILayout.Button("Powrót do Menu", GUILayout.Height(40)))
            {
                editingDeck = null;
                SwitchScreen(ScreenType.MainMenu);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawShopGUI()
        {
            var profile = PlayerProfileManager.Instance.CurrentProfile;
            if (profile == null) return;

            GUILayout.BeginVertical("box", GUILayout.Width(400));
            GUILayout.Label("<b>SKLEP Z KARTAMI</b>");
            GUILayout.Label($"Twoje Złoto: <b>{profile.gold} szt.</b>");
            GUILayout.Space(20);

            GUILayout.Box("Pakiet Klasyczny CCG\nZawiera 5 losowych kart\nKoszt: 100 złota", GUILayout.Height(80));
            
            GUILayout.Space(10);
            if (GUILayout.Button("KUP PAKIET (100 Golda)", GUILayout.Height(45)))
            {
                if (PlayerProfileManager.Instance.SpendGold(100))
                {
                    // Generate 5 random cards from global database
                    List<string> rewardCards = new List<string>();
                    for (int i = 0; i < 5; i++)
                    {
                        var randomCard = allGameCards[Random.Range(0, allGameCards.Count)];
                        PlayerProfileManager.Instance.AddCardToCollection(randomCard.cardId);
                        rewardCards.Add(randomCard.cardName);
                    }
                    Debug.Log($"Otwarto pakiet! Wylosowano: {string.Join(", ", rewardCards)}");
                }
                else
                {
                    Debug.LogWarning("Brak wystarczającej ilości złota!");
                }
            }

            GUILayout.Space(15);
            if (GUILayout.Button("Darmowe +500 Golda (Test)", GUILayout.Height(30)))
            {
                PlayerProfileManager.Instance.AddGold(500);
            }

            GUILayout.Space(30);
            if (GUILayout.Button("Powrót do Menu", GUILayout.Height(40)))
            {
                SwitchScreen(ScreenType.MainMenu);
            }
            GUILayout.EndVertical();
        }

        private void DrawMatchmakingGUI()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(400));
            GUILayout.Label("<b>WYSZUKIWANIE PRZECIWNIKA (MOCK)</b>");
            GUILayout.Space(20);
            GUILayout.Label("Szukanie rywala o podobnym rankingu...");
            GUILayout.Box("Wyszukiwanie...", GUILayout.Height(60));
            GUILayout.Space(20);

            if (GUILayout.Button("Symuluj znalezienie meczu", GUILayout.Height(45)))
            {
                // Setup and Start game match
                var profile = PlayerProfileManager.Instance.CurrentProfile;
                
                // Get player's active deck or create default mock
                List<CardData> p1Deck = new List<CardData>();
                if (profile.decks.Count > 0 && profile.decks[0].cardIds.Count > 0)
                {
                    foreach (var cardId in profile.decks[0].cardIds)
                    {
                        var data = FindCardDataById(cardId);
                        if (data != null) p1Deck.Add(data);
                    }
                }
                
                // Fill if deck empty
                while (p1Deck.Count < 10)
                {
                    p1Deck.Add(allGameCards[Random.Range(0, 10)]);
                }

                // AI Opponent Deck
                List<CardData> p2Deck = new List<CardData>();
                for (int i = 0; i < 15; i++)
                {
                    p2Deck.Add(allGameCards[Random.Range(0, allGameCards.Count)]);
                }

                GameManager.Instance.StartMatch(p1Deck, p2Deck);
                SwitchScreen(ScreenType.Gameplay);
            }

            GUILayout.Space(15);
            if (GUILayout.Button("Anuluj", GUILayout.Height(30)))
            {
                SwitchScreen(ScreenType.MainMenu);
            }
            GUILayout.EndVertical();
        }

        private void DrawGameplayGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Player1 == null) return;

            GUILayout.Label($"Mecz w toku. Tura: {gm.TurnNumber} (Aktywny: {gm.ActivePlayer.PlayerName})");

            GUILayout.BeginHorizontal();

            // Left Side: Boards and Hands
            GUILayout.BeginVertical(GUILayout.Width(Screen.width * 0.7f));
            
            // Opponent State (Player 2)
            DrawPlayerStatusGUI(gm.Player2, "PRZECIWNIK");
            
            GUILayout.Space(20);
            
            // Player State (Player 1)
            DrawPlayerStatusGUI(gm.Player1, "TY (GRACZ)");

            GUILayout.EndVertical();

            // Right Side: Game Action Controls
            GUILayout.BeginVertical("box", GUILayout.Width(Screen.width * 0.25f));
            GUILayout.Label("<b>KONTROLA</b>");
            
            if (GUILayout.Button("Zakończ Turę", GUILayout.Height(40)))
            {
                gm.EndTurn();
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Poddaj się (Wyjście)", GUILayout.Height(30)))
            {
                SwitchScreen(ScreenType.MainMenu);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawPlayerStatusGUI(PlayerState player, string role)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>{role}: {player.PlayerName}</b> - HP: <b>{player.Health}/30</b> | Mana: <b>{player.Mana}/{player.MaxMana}</b>");
            
            // Hand
            GUILayout.Label("Ręka:");
            GUILayout.BeginHorizontal();
            foreach (var card in player.Hand)
            {
                string label = $"{card.Data.cardName}\n(Cost:{card.CurrentManaCost} A:{card.CurrentAttack} H:{card.CurrentHealth})";
                if (GameManager.Instance.ActivePlayer == player)
                {
                    if (GUILayout.Button(label, GUILayout.Width(130), GUILayout.Height(55)))
                    {
                        GameManager.Instance.PlayCard(card);
                    }
                }
                else
                {
                    GUILayout.Box(label, GUILayout.Width(130), GUILayout.Height(55));
                }
            }
            GUILayout.EndHorizontal();

            // Board
            GUILayout.Label("Stół:");
            GUILayout.BeginHorizontal();
            foreach (var card in player.Board)
            {
                string canAttackMark = card.CanAttack ? "*" : "";
                string label = $"{card.Data.cardName}{canAttackMark}\n({card.CurrentAttack}/{card.CurrentHealth})";

                if (GameManager.Instance.ActivePlayer == player && card.CanAttack)
                {
                    GUILayout.BeginVertical();
                    GUILayout.Box(label, GUILayout.Width(110), GUILayout.Height(45));
                    if (GUILayout.Button("Atak Twarz", GUILayout.Width(110)))
                    {
                        GameManager.Instance.AttackFace(card);
                    }

                    var opponent = (player == GameManager.Instance.Player1) ? GameManager.Instance.Player2 : GameManager.Instance.Player1;
                    foreach (var target in opponent.Board)
                    {
                        if (GUILayout.Button($"Atak {target.Data.cardName}", GUILayout.Width(110)))
                        {
                            GameManager.Instance.AttackTarget(card, target);
                        }
                    }
                    GUILayout.EndVertical();
                }
                else
                {
                    GUILayout.Box(label, GUILayout.Width(110), GUILayout.Height(45));
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}
