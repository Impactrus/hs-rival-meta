using System.Collections.Generic;
using UnityEngine;
using CCG.Core;

namespace CCG.UI
{
    public class UIHandManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform handContainer;

        [Header("Hand Fan Layout Settings")]
        [SerializeField] private float cardSpacing = 120f;
        [SerializeField] private float maxFanAngle = 15f;
        [SerializeField] private float fanHeightOffset = 20f;

        private List<UICard> spawnedCards = new List<UICard>();

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += RefreshHand;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= RefreshHand;
        }

        public void RefreshHand()
        {
            if (GameManager.Instance == null || GameManager.Instance.Player1 == null) return;

            var handList = GameManager.Instance.Player1.Hand;

            // Clear old cards
            foreach (var card in spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            spawnedCards.Clear();

            // Spawn new cards
            for (int i = 0; i < handList.Count; i++)
            {
                GameObject cardGo = Instantiate(cardPrefab, handContainer);
                UICard uiCard = cardGo.GetComponent<UICard>();
                if (uiCard != null)
                {
                    uiCard.Setup(handList[i]);
                    spawnedCards.Add(uiCard);
                }
            }

            ArrangeCards();
        }

        private void ArrangeCards()
        {
            int count = spawnedCards.Count;
            if (count == 0) return;

            // Calculate fan layout positions to look premium (like HS)
            float totalWidth = (count - 1) * cardSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                float x = startX + (i * cardSpacing);
                
                // Parabola effect for fan curve
                float y = Mathf.Sin(t * Mathf.PI) * fanHeightOffset;
                
                // Rotation angle for fan effect
                float angle = Mathf.Lerp(maxFanAngle, -maxFanAngle, t);
                if (count == 1) angle = 0;

                RectTransform rect = spawnedCards[i].GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(x, y);
                    rect.localRotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }
    }
}
