using System.Collections.Generic;
using UnityEngine;

namespace CCG.Core
{
    public class PlayerState
    {
        public string PlayerName { get; private set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        
        public int Mana { get; set; }
        public int MaxMana { get; set; } // Current mana capacity for the turn
        public int TempMana { get; set; } // Used for temporary mana crystals (e.g. The Coin)

        public List<CardInstance> Deck { get; private set; }
        public List<CardInstance> Hand { get; private set; }
        public List<CardInstance> Board { get; private set; }
        public List<CardInstance> Graveyard { get; private set; }

        public PlayerState(string name, List<CardData> deckData)
        {
            PlayerName = name;
            Health = 30;
            MaxHealth = 30;
            Mana = 0;
            MaxMana = 0;

            Deck = new List<CardInstance>();
            Hand = new List<CardInstance>();
            Board = new List<CardInstance>();
            Graveyard = new List<CardInstance>();

            // Initialize deck
            foreach (var cardData in deckData)
            {
                Deck.Add(new CardInstance(cardData));
            }
        }

        public void ShuffleDeck()
        {
            // Simple Fisher-Yates shuffle
            System.Random rand = new System.Random();
            for (int i = Deck.Count - 1; i > 0; i--)
            {
                int k = rand.Next(i + 1);
                var temp = Deck[i];
                Deck[i] = Deck[k];
                Deck[k] = temp;
            }
        }

        public CardInstance DrawCard()
        {
            if (Deck.Count == 0)
            {
                // Fatigue system or simple out of cards penalty can be added here
                Debug.Log($"{PlayerName} has no cards left to draw!");
                return null;
            }

            CardInstance card = Deck[0];
            Deck.RemoveAt(0);

            if (Hand.Count >= 10) // Hearthstone maximum hand size
            {
                Debug.Log($"{PlayerName}'s hand is full! Discarded {card.Data.cardName}.");
                Graveyard.Add(card);
                return null;
            }

            Hand.Add(card);
            return card;
        }

        public void RefillMana(int capacity)
        {
            MaxMana = Mathf.Min(capacity, 10);
            Mana = MaxMana;
        }
    }
}
