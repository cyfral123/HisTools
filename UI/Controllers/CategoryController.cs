using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace UI.Controllers
{
    public class CategoryController : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public RectTransform categoryRect;
        public GameObject buttonsContainer;
        public VerticalLayoutGroup layoutGroup;
        public CanvasGroup buttonsCanvasGroup;

        public float padding = 10f;

        private Vector2 pointerOffset;
        private const float HEADER_HEIGHT = 25f;
        private bool isExpanded = true;

        private Tween currentTween;

        public void Awake()
        {
            if (categoryRect == null)
                categoryRect = transform.parent.GetComponent<RectTransform>();
        }

        public void ToggleCollapse()
        {
            if (this == null || transform == null)
            {
                return;
            }
            
            currentTween?.Kill();

            isExpanded = !isExpanded;

            float targetAlpha = isExpanded ? Plugin.MaxBackgroundAlpha : 0f;
            float targetHeight = isExpanded ? GetExpandedHeight() : HEADER_HEIGHT;

            if (!isExpanded)
            {
                buttonsCanvasGroup.interactable = false;
                buttonsCanvasGroup.blocksRaycasts = false;
                layoutGroup.enabled = false;
            }

            Sequence seq = DOTween.Sequence();

            seq.Join(buttonsCanvasGroup.DOFade(targetAlpha, Plugin.AnimationDuration)
                .SetEase(Ease.OutBack));

            seq.Join(categoryRect.DOSizeDelta(
                new Vector2(categoryRect.sizeDelta.x, targetHeight),
                Plugin.AnimationDuration
            ).SetEase(Ease.OutBack));

            seq.OnComplete(() =>
            {
                if (isExpanded)
                {
                    buttonsCanvasGroup.interactable = true;
                    buttonsCanvasGroup.blocksRaycasts = true;
                    layoutGroup.enabled = true;
                }
            });

            currentTween = seq;
        }

        private float GetExpandedHeight()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsContainer.GetComponent<RectTransform>());
            return ((RectTransform)buttonsContainer.transform).rect.height + HEADER_HEIGHT;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ToggleCollapse();
            }

            if (eventData.button == PointerEventData.InputButton.Left && categoryRect != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(categoryRect.parent as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPointerPosition
                );

                pointerOffset = categoryRect.anchoredPosition - localPointerPosition;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && categoryRect != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(categoryRect.parent as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPointerPosition))
                {
                    Vector2 newPosition = localPointerPosition + pointerOffset;
                    categoryRect.anchoredPosition = ClampPosition(newPosition);
                }
            }
        }

        private Vector2 ClampPosition(Vector2 position)
        {
            RectTransform parentRect = categoryRect.parent as RectTransform;
            if (parentRect == null) return position;

            float panelWidth = categoryRect.sizeDelta.x * categoryRect.localScale.x;
            float panelHeight = categoryRect.sizeDelta.y * categoryRect.localScale.y;

            float halfWidth = panelWidth / 2f;
            float halfHeight = panelHeight / 2f;

            float parentHalfWidth = parentRect.rect.width / 2f;
            float parentHalfHeight = parentRect.rect.height / 2f;

            float maxX = parentHalfWidth - halfWidth - padding;
            float minX = -maxX;
            float maxY = parentHalfHeight - halfHeight - padding - HEADER_HEIGHT;
            float minY = -maxY;

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);

            return position;
        }


    }
}
