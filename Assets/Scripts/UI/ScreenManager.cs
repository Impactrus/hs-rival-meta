using System.Collections.Generic;
using UnityEngine;

namespace CCG.UI
{
    public enum ScreenType
    {
        Login,
        MainMenu,
        Collection,
        Shop,
        Matchmaking,
        Gameplay
    }

    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("UI Screen Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject collectionPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject matchmakingPanel;
        [SerializeField] private GameObject gameplayPanel;

        private Dictionary<ScreenType, GameObject> screens;
        private ScreenType currentScreenType;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeScreens();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Start with the Login Screen
            SwitchToScreen(ScreenType.Login);
        }

        private void InitializeScreens()
        {
            screens = new Dictionary<ScreenType, GameObject>
            {
                { ScreenType.Login, loginPanel },
                { ScreenType.MainMenu, mainMenuPanel },
                { ScreenType.Collection, collectionPanel },
                { ScreenType.Shop, shopPanel },
                { ScreenType.Matchmaking, matchmakingPanel },
                { ScreenType.Gameplay, gameplayPanel }
            };
        }

        public void SwitchToScreen(ScreenType targetScreen)
        {
            foreach (var screen in screens)
            {
                if (screen.Value != null)
                {
                    screen.Value.SetActive(screen.Key == targetScreen);
                }
            }
            currentScreenType = targetScreen;
            Debug.Log($"Switched to screen: {targetScreen}");
        }
    }
}
