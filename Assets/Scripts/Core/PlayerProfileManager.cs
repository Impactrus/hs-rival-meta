using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CCG.Core
{
    [Serializable]
    public class PlayerProfile
    {
        public string username;
        public int gold;
        public List<string> ownedCardIds = new List<string>();
        public List<SerializableDeck> decks = new List<SerializableDeck>();
    }

    [Serializable]
    public class SerializableDeck
    {
        public string deckName;
        public List<string> cardIds = new List<string>();
    }

    public class PlayerProfileManager : MonoBehaviour
    {
        public static PlayerProfileManager Instance { get; private set; }

        public PlayerProfile CurrentProfile { get; private set; }
        public bool IsLoggedIn => CurrentProfile != null;

        public static event Action OnProfileUpdated;

        private string savePath;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                savePath = Path.Combine(Application.persistentDataPath, "player_profile.json");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public bool Login(string username, string password)
        {
            // Simple mockup authentication: If password is "admin", let them login or register
            if (string.IsNullOrEmpty(username)) return false;

            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                CurrentProfile = JsonUtility.FromJson<PlayerProfile>(json);
                if (CurrentProfile.username != username)
                {
                    // Create new profile for different user
                    CreateNewProfile(username);
                }
            }
            else
            {
                CreateNewProfile(username);
            }

            Debug.Log($"User logged in successfully: {username}");
            OnProfileUpdated?.Invoke();
            return true;
        }

        private void CreateNewProfile(string username)
        {
            CurrentProfile = new PlayerProfile
            {
                username = username,
                gold = 100, // Starter gold
                ownedCardIds = new List<string>
                {
                    // Give starter card IDs
                    "Starter_Minion_1", "Starter_Minion_1",
                    "Starter_Minion_2", "Starter_Minion_2",
                    "Starter_Minion_3", "Starter_Minion_3",
                    "Starter_Spell_1", "Starter_Spell_1"
                },
                decks = new List<SerializableDeck>
                {
                    new SerializableDeck
                    {
                        deckName = "Starter Deck",
                        cardIds = new List<string> { "Starter_Minion_1", "Starter_Minion_2", "Starter_Minion_3", "Starter_Spell_1" }
                    }
                }
            };
            SaveProfile();
        }

        public void SaveProfile()
        {
            if (CurrentProfile == null) return;
            string json = JsonUtility.ToJson(CurrentProfile, true);
            File.WriteAllText(savePath, json);
            OnProfileUpdated?.Invoke();
        }

        public void AddGold(int amount)
        {
            if (CurrentProfile == null) return;
            CurrentProfile.gold += amount;
            SaveProfile();
        }

        public bool SpendGold(int amount)
        {
            if (CurrentProfile == null || CurrentProfile.gold < amount) return false;
            CurrentProfile.gold -= amount;
            SaveProfile();
            return true;
        }

        public void AddCardToCollection(string cardId)
        {
            if (CurrentProfile == null) return;
            CurrentProfile.ownedCardIds.Add(cardId);
            SaveProfile();
        }
    }
}
