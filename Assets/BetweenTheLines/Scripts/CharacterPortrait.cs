using System;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace Emotionalaw
{
    public sealed class CharacterPortrait : MonoBehaviour
    {
        [Serializable]
        private struct Expression
        {
            public string mood;
            public Sprite sprite;
        }


        [Header("Introduction Animation")]
        [SerializeField, Min(0f)] private float introductionDistance = 180f;
        [SerializeField, Min(0.05f)] private float introductionDuration = 0.38f;

        private RectTransform portraitTransform;
        private Vector2 authoredPosition;
        private bool transformCached;
        private bool isVisible = true;
        private bool introductionSpotlight;

        public string CharacterName => characterName;

        private void Awake() => CacheTransform();

        private void CacheTransform()
        {
            if (transformCached) return;
            portraitTransform = (RectTransform)portrait.transform;
            authoredPosition = portraitTransform.anchoredPosition;
            transformCached = true;
        }

        public void HideForIntroduction()
        {
            CacheTransform();
            isVisible = false;
            introductionSpotlight = false;
            nameLabel.SetActive(false);
            portraitTransform.anchoredPosition = authoredPosition;
            Color color = listeningColor;
            color.a = 0f;
            portrait.color = color;
        }

        public void ShowForConversation()
        {
            CacheTransform();
            isVisible = true;
            introductionSpotlight = false;
            nameLabel.SetActive(true);
            portraitTransform.anchoredPosition = authoredPosition;
            portrait.color = listeningColor;
        }

        public async YarnTask IntroduceAsync(string side, bool spotlight)
        {
            CacheTransform();
            isVisible = true;
            introductionSpotlight = spotlight;
            nameLabel.SetActive(true);
            float direction = side.Equals("right", StringComparison.OrdinalIgnoreCase) ? 1f : -1f;
            Vector2 start = authoredPosition + Vector2.right * introductionDistance * direction;
            float elapsed = 0f;
            while (elapsed < introductionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / introductionDuration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                portraitTransform.anchoredPosition = Vector2.LerpUnclamped(start, authoredPosition, eased);
                Color color = spotlight ? speakingColor : listeningColor;
                color.a = eased;
                portrait.color = color;
                await YarnTask.Yield();
            }
            portraitTransform.anchoredPosition = authoredPosition;
            portrait.color = spotlight ? speakingColor : listeningColor;
        }

        [SerializeField] private string characterName;
        [SerializeField] private Image portrait;
        [Tooltip("Existing Name child authored in the scene hierarchy.")]
        [SerializeField] private GameObject nameLabel;
        [SerializeField] private Sprite neutral;
        [SerializeField] private Expression[] expressions;
        [SerializeField] private Color speakingColor = Color.white;
        [SerializeField] private Color listeningColor = new Color(.52f, .54f, .60f, 1);

        public void Present(string speaker, string mood)
        {
            if (!isVisible)
            {
                Color hiddenColor = listeningColor;
                hiddenColor.a = 0f;
                portrait.color = hiddenColor;
                return;
            }
            if (introductionSpotlight)
            {
                portrait.color = speakingColor;
                return;
            }
            bool speaking = speaker == characterName;
            portrait.color = speaking ? speakingColor : listeningColor;
            if (!speaking) return;
            portrait.sprite = neutral;
            foreach (var expression in expressions)
                if (expression.mood == mood && expression.sprite != null)
                {
                    portrait.sprite = expression.sprite;
                    break;
                }
        }

        public void ResetPortrait()
        {
            CacheTransform();
            isVisible = true;
            introductionSpotlight = false;
            nameLabel.SetActive(true);
            portraitTransform.anchoredPosition = authoredPosition;
            portrait.sprite = neutral;
            portrait.color = listeningColor;
        }
    }
}
