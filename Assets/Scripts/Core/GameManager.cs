using System;
using System.Collections.Generic;
using UnityEngine;

namespace CCG.Core
{
    // GameManager to główny skrypt zarządzający grą (tzw. silnik reguł meczu).
    // Dziedziczy po MonoBehaviour, co oznacza, że możemy go przypisać do obiektu w Unity.
    public class GameManager : MonoBehaviour
    {
        // Singleton – wzorzec projektowy, który gwarantuje, że w grze będzie istniał
        // tylko jeden obiekt GameManager, łatwo dostępny z dowolnego innego skryptu (poprzez GameManager.Instance).
        public static GameManager Instance { get; private set; }

        public PlayerState Player1 { get; private set; } // Stan Gracza 1
        public PlayerState Player2 { get; private set; } // Stan Gracza 2
        public PlayerState ActivePlayer { get; private set; } // Gracz, który wykonuje aktualnie ruch
        
        // Wygodna właściwość (Property) zwracająca przeciwnika aktywnego gracza
        public PlayerState OpponentPlayer => (ActivePlayer == Player1) ? Player2 : Player1;

        public int TurnNumber { get; private set; } // Licznik tur

        // Zdarzenia (Events) – pozwalają powiadomić system UI (interfejs graficzny) o zmianach w grze.
        // Gdy te zdarzenia się uruchomią, UI wie, że musi się odświeżyć (np. zmienić teksty punktów życia).
        public static event Action OnGameStateChanged;
        public static event Action<string> OnGameLog; // Do wysyłania tekstowych informacji do konsoli gry

        // Awake uruchamia się przed metodą Start
        private void Awake()
        {
            // Konstrukcja zabezpieczająca Singleton przed duplikowaniem
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // GameManager nie zniknie przy zmianie sceny
            }
            else
            {
                Destroy(gameObject); // Usuwamy duplikat
            }
        }

        // Uruchamia nowy mecz, przyjmując talie obu graczy
        public void StartMatch(List<CardData> deck1, List<CardData> deck2)
        {
            // Tworzymy stany graczy (dajemy im nicki i talie)
            Player1 = new PlayerState("Player 1", deck1);
            Player2 = new PlayerState("Player 2", deck2);

            // Losowo tasujemy talie obu graczy
            Player1.ShuffleDeck();
            Player2.ShuffleDeck();

            TurnNumber = 0;

            // Rozdajemy karty startowe na rękę (Hearthstone: Gracz 1 dostaje 3, Gracz 2 dostaje 4)
            for (int i = 0; i < 3; i++) Player1.DrawCard();
            for (int i = 0; i < 4; i++) Player2.DrawCard();

            // Losujemy, kto zaczyna jako pierwszy
            ActivePlayer = (UnityEngine.Random.value > 0.5f) ? Player1 : Player2;
            OnGameLog?.Invoke($"Mecz rozpoczęty! Pierwsza tura: {ActivePlayer.PlayerName}");

            StartTurn(); // Rozpoczynamy pierwszą turę
        }

        // Obsługuje początek tury aktywnego gracza
        private void StartTurn()
        {
            TurnNumber++;
            
            // Przyrost many w stylu Hearthstone (każdy gracz co turę ma o 1 więcej max many, max 10)
            // Używamy wzoru: tura dzielona przez 2 (zaokrąglona w górę)
            int targetMana = Mathf.CeilToInt(TurnNumber / 2f);
            ActivePlayer.RefillMana(targetMana);

            OnGameLog?.Invoke($"--- Tura gracza {ActivePlayer.PlayerName} (Mana: {ActivePlayer.Mana}/{ActivePlayer.MaxMana}) ---");

            // Gracz dobiera kartę na początku swojej tury
            ActivePlayer.DrawCard();

            // Odświeżamy możliwość ataku dla jednostek na stole (usuwamy tzw. stronniczą bezczynność / summon sickness)
            foreach (var minion in ActivePlayer.Board)
            {
                minion.CanAttack = true;
            }

            OnGameStateChanged?.Invoke(); // Informujemy UI o nowej turze
        }

        // Kończy turę aktualnego gracza i przekazuje ją przeciwnikowi
        public void EndTurn()
        {
            OnGameLog?.Invoke($"{ActivePlayer.PlayerName} zakończył swoją turę.");
            ActivePlayer = OpponentPlayer; // Zmiana aktywnego gracza
            StartTurn();
        }

        // Obsługuje zagranie karty z ręki na stół
        public bool PlayCard(CardInstance card, int boardIndex = -1)
        {
            // Walidacja: Czy karta w ogóle istnieje i czy gracz ma ją na ręce?
            if (card == null || !ActivePlayer.Hand.Contains(card))
            {
                Debug.LogWarning("Brak karty na ręce!");
                return false;
            }

            // Walidacja: Czy gracz ma wystarczająco dużo many?
            if (ActivePlayer.Mana < card.CurrentManaCost)
            {
                OnGameLog?.Invoke("Za mało many!");
                return false;
            }

            // Odejmujemy manę i usuwamy kartę z ręki
            ActivePlayer.Mana -= card.CurrentManaCost;
            ActivePlayer.Hand.Remove(card);

            // Sprawdzamy typ karty i wykonujemy odpowiednie akcje
            if (card.Data.cardType == CardType.Minion)
            {
                // Jeśli to potwór (stronnik), umieszczamy go na stole
                if (boardIndex < 0 || boardIndex > ActivePlayer.Board.Count)
                {
                    ActivePlayer.Board.Add(card);
                }
                else
                {
                    ActivePlayer.Board.Insert(boardIndex, card);
                }
                
                // Nowo zagrany stronnik ma "sickness" (nie może atakować w turze zagrania)
                card.CanAttack = false;
                
                OnGameLog?.Invoke($"{ActivePlayer.PlayerName} zagrał Stronnika: {card.Data.cardName} ({card.CurrentAttack}/{card.CurrentHealth})");
            }
            else if (card.Data.cardType == CardType.Spell)
            {
                // Jeśli to czar, wywołujemy efekt czaru i odrzucamy na cmentarz
                OnGameLog?.Invoke($"{ActivePlayer.PlayerName} rzucił czar: {card.Data.cardName}");
                ActivePlayer.Graveyard.Add(card);
            }

            OnGameStateChanged?.Invoke(); // Informujemy UI o zagraniu karty
            CheckVictoryConditions();      // Sprawdzamy, czy ktoś nie wygrał
            return true;
        }

        // Obsługuje walkę między dwoma stronnikami (atak jednostki na jednostkę)
        public bool AttackTarget(CardInstance attacker, CardInstance target)
        {
            // Walidacja obecności na stole i uprawnień do ataku
            if (attacker == null || target == null) return false;
            if (!ActivePlayer.Board.Contains(attacker)) return false;
            if (!OpponentPlayer.Board.Contains(target)) return false;

            if (!attacker.CanAttack)
            {
                OnGameLog?.Invoke($"{attacker.Data.cardName} nie może atakować w tej turze!");
                return false;
            }

            // W walce w stylu HS/MTG obie jednostki zadają sobie nawzajem obrażenia w tym samym momencie
            target.TakeDamage(attacker.CurrentAttack);
            attacker.TakeDamage(target.CurrentAttack);

            attacker.CanAttack = false; // Jednostka zużyła swój atak w tej turze

            OnGameLog?.Invoke($"{attacker.Data.cardName} zaatakował {target.Data.cardName}!");

            // Usuwamy martwe jednostki (których zdrowie spadło do 0)
            ResolveBoardState();

            OnGameStateChanged?.Invoke(); // Odświeżamy UI po walce
            CheckVictoryConditions();
            return true;
        }

        // Obsługuje bezpośredni atak potwora w bohatera wroga ("na twarz")
        public bool AttackFace(CardInstance attacker)
        {
            if (attacker == null || !ActivePlayer.Board.Contains(attacker)) return false;
            
            if (!attacker.CanAttack)
            {
                OnGameLog?.Invoke($"{attacker.Data.cardName} nie może atakować w tej turze!");
                return false;
            }

            // Odejmujemy punkty życia przeciwnikowi o wartości ataku stronnika
            OpponentPlayer.Health -= attacker.CurrentAttack;
            attacker.CanAttack = false; // Jednostka traci możliwość kolejnego ataku

            OnGameLog?.Invoke($"{attacker.Data.cardName} zaatakował bohatera {OpponentPlayer.PlayerName} za {attacker.CurrentAttack} pkt. obrażeń!");

            OnGameStateChanged?.Invoke();
            CheckVictoryConditions();
            return true;
        }

        // Sprawdza stół i usuwa martwych stronników u obu graczy
        private void ResolveBoardState()
        {
            RemoveDeadMinions(Player1);
            RemoveDeadMinions(Player2);
        }

        // Usuwa z planszy jednostki o zdrowiu <= 0 i przenosi je na cmentarz
        private void RemoveDeadMinions(PlayerState player)
        {
            for (int i = player.Board.Count - 1; i >= 0; i--)
            {
                if (player.Board[i].CurrentHealth <= 0)
                {
                    OnGameLog?.Invoke($"Stronnik gracza {player.PlayerName} umarł: {player.Board[i].Data.cardName}");
                    player.Graveyard.Add(player.Board[i]); // Przeniesienie na cmentarz
                    player.Board.RemoveAt(i);             // Usunięcie ze stołu
                }
            }
        }

        // Sprawdza punkty życia graczy i rozstrzyga o wygranej/remisie
        private void CheckVictoryConditions()
        {
            if (Player1.Health <= 0 && Player2.Health <= 0)
            {
                OnGameLog?.Invoke("REMIS! Obaj gracze polegli.");
                EndMatch();
            }
            else if (Player1.Health <= 0)
            {
                OnGameLog?.Invoke($"Gracz {Player2.PlayerName} WYGRYWA mecz!");
                EndMatch();
            }
            else if (Player2.Health <= 0)
            {
                OnGameLog?.Invoke($"Gracz {Player1.PlayerName} WYGRYWA mecz!");
                EndMatch();
            }
        }

        private void EndMatch()
        {
            // Miejsce na logikę zakończenia gry (np. powrót do Menu Głównego, przyznanie punktów doświadczenia)
        }
    }
}
