using UnityEngine;
using CCG.Core;

namespace CCG.UI
{
    public class ThreeDMenuController : MonoBehaviour
    {
        [Header("Camera & Anchors")]
        [SerializeField] private Transform mainCamera;
        [SerializeField] private float transitionSpeed = 5f;

        [Header("Predefined Camera Views")]
        [SerializeField] private Transform mainMenuAnchor;
        [SerializeField] private Transform collectionAnchor;
        [SerializeField] private Transform shopAnchor;
        [SerializeField] private Transform gameplayAnchor;

        [Header("3D Buttons")]
        [SerializeField] private ThreeDButton playButton;
        [SerializeField] private ThreeDButton collectionButton;
        [SerializeField] private ThreeDButton shopButton;
        [SerializeField] private ThreeDButton backToMenuButton;

        private Transform targetAnchor;

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main.transform;
            }

            // Set initial camera target to Main Menu
            targetAnchor = mainMenuAnchor;

            // Hook up 3D button events
            if (playButton != null) playButton.OnClicked += () => TransitionTo(ScreenType.Matchmaking);
            if (collectionButton != null) collectionButton.OnClicked += () => TransitionTo(ScreenType.Collection);
            if (shopButton != null) shopButton.OnClicked += () => TransitionTo(ScreenType.Shop);
            if (backToMenuButton != null) backToMenuButton.OnClicked += () => TransitionTo(ScreenType.MainMenu);
        }

        private void Update()
        {
            if (targetAnchor == null || mainCamera == null) return;

            // Smoothly move and rotate camera to the active view anchor
            mainCamera.position = Vector3.Lerp(mainCamera.position, targetAnchor.position, Time.deltaTime * transitionSpeed);
            mainCamera.rotation = Quaternion.Slerp(mainCamera.rotation, targetAnchor.rotation, Time.deltaTime * transitionSpeed);
        }

        public void TransitionTo(ScreenType targetScreen)
        {
            switch (targetScreen)
            {
                case ScreenType.MainMenu:
                    targetAnchor = mainMenuAnchor;
                    break;
                case ScreenType.Collection:
                    targetAnchor = collectionAnchor;
                    break;
                case ScreenType.Shop:
                    targetAnchor = shopAnchor;
                    break;
                case ScreenType.Matchmaking:
                    targetAnchor = gameplayAnchor; // Transition directly to board view
                    // Also trigger match start logic
                    break;
            }

            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.SwitchToScreen(targetScreen);
            }
        }
    }
}
