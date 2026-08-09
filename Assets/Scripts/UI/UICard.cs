using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using CCG.Core;

namespace CCG.UI
{
    public class UICard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI manaCostText;
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image cardArtImage;
        [SerializeField] private Image cardFrameImage;

        [Header("Hover Animation Settings")]
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float animationSpeed = 10f;
        
        public CardInstance Card { get; private set; }
        
        private Vector3 originalScale;
        private Vector3 targetScale;
        private int originalSiblingIndex;
        private Transform originalParent;
        private Canvas canvas;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Setup(CardInstance card)
        {
            Card = card;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (Card == null) return;

            cardNameText.text = Card.Data.cardName;
            descriptionText.text = Card.Data.description;
            manaCostText.text = Card.CurrentManaCost.ToString();

            if (Card.Data.cardType == CardType.Minion)
            {
                attackText.gameObject.SetActive(true);
                healthText.gameObject.SetActive(true);
                attackText.text = Card.CurrentAttack.ToString();
                healthText.text = Card.CurrentHealth.ToString();

                // Dynamic stat coloring (Hearthstone style: green if buffed, red if damaged)
                attackText.color = Card.CurrentAttack > Card.Data.attack ? Color.green : Color.white;
                healthText.color = Card.CurrentHealth < Card.Data.maxHealth ? Color.red : (Card.CurrentHealth > Card.Data.maxHealth ? Color.green : Color.white);
            }
            else
            {
                attackText.gameObject.SetActive(false);
                healthText.gameObject.SetActive(false);
            }

            if (Card.Data.cardArt != null)
            {
                cardArtImage.sprite = Card.Data.cardArt;
            }
        }

        private void Update()
        {
            // Smoothly lerp scale for premium hover effect
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
        }

        // --- Hover Effects ---
        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = originalScale * hoverScale;
            originalSiblingIndex = transform.GetSiblingIndex();
            // Move card to front when hovered
            transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = originalScale;
            transform.SetSiblingIndex(originalSiblingIndex);
        }

        // --- Drag & Drop Gameplay Interaction ---
        public void OnBeginDrag(PointerEventData eventData)
        {
            // Only allow dragging if it is our turn
            if (GameManager.Instance == null || GameManager.Instance.ActivePlayer == null) return;
            // Assumes Player 1 is the human player in this simple mockup setup
            if (GameManager.Instance.ActivePlayer.PlayerName != "Player 1") return;
            if (!GameManager.Instance.Player1.Hand.Contains(Card)) return;

            originalParent = transform.parent;
            
            // Unparent card so it moves freely over UI containers
            transform.SetParent(canvas.transform, true);
            canvasGroup.blocksRaycasts = false; // Allow rays to pass through card to detect drop zones
            targetScale = originalScale;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (canvasGroup.blocksRaycasts) return; // Not dragging

            // Move card with touch/cursor
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector3 worldPosition
            );
            transform.position = worldPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup.blocksRaycasts) return;

            canvasGroup.blocksRaycasts = true;

            // Check if dropped over Play Zone / Board
            bool played = false;
            
            // Simple raycast check for drop target
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (var result in results)
            {
                if (result.gameObject.GetComponent<BoardPlayZone>() != null)
                {
                    played = GameManager.Instance.PlayCard(Card);
                    break;
                }
            }

            if (played)
            {
                // Card is successfully played, GameManager will handle moving it to board.
                // UICard object will be destroyed or recycled by Hand Visual Manager.
                Destroy(gameObject);
            }
            else
            {
                // Return card back to hand layout
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }
        }
    }
}
