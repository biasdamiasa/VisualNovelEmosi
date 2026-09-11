using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Emotionalaw
{
    public sealed class NotesClueIndicator : MonoBehaviour
    {
        [Header("Authored Badge")]
        [SerializeField] private GameObject unreadBadge;
        [SerializeField] private RectTransform animatedTarget;
        [SerializeField] private Image notesGraphic;

        [Header("Micro Animation")]
        [SerializeField, Min(0.05f)] private float duration = 0.42f;
        [SerializeField, Range(1f, 1.35f)] private float peakScale = 1.12f;
        [SerializeField, Range(0f, 12f)] private float tiltDegrees = 4f;

        [Header("Clue Unlock Red Blink")]
        [SerializeField] private Color blinkColor = new Color(.92f, .13f, .16f, 1f);
        [SerializeField, Min(0.2f)] private float blinkDuration = 1.6f;
        [SerializeField, Min(0.2f)] private float blinkCycleDuration = 1.1f;
        [SerializeField, Range(0f, 1f)] private float blinkStrength = .72f;

        private Coroutine animationRoutine;
        private bool hasUnreadClue;
        private bool transformCached;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private Color baseGraphicColor;

        public bool HasUnreadClue => hasUnreadClue;
        public GameObject UnreadBadge => unreadBadge;

        private void Awake()
        {
            CacheBaseVisuals();
            if (unreadBadge != null) unreadBadge.SetActive(hasUnreadClue);
        }

        private void OnEnable() => CacheBaseVisuals();
        private void OnDisable() => StopAnimationAndRestore();

        public void NotifyClueFound()
        {
            CacheBaseVisuals();
            hasUnreadClue = true;
            if (unreadBadge != null) unreadBadge.SetActive(true);
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(AnimateUnread());
        }

        public void MarkRead()
        {
            hasUnreadClue = false;
            if (unreadBadge != null) unreadBadge.SetActive(false);
            StopAnimationAndRestore();
        }

        public void ResetIndicator() => MarkRead();

        private void CacheBaseVisuals()
        {
            if (transformCached || animatedTarget == null) return;
            baseScale = animatedTarget.localScale;
            baseRotation = animatedTarget.localRotation;
            if (notesGraphic != null) baseGraphicColor = notesGraphic.color;
            transformCached = true;
        }

        private void StopAnimationAndRestore()
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = null;
            if (!transformCached) return;
            if (animatedTarget != null)
            {
                animatedTarget.localScale = baseScale;
                animatedTarget.localRotation = baseRotation;
            }
            if (notesGraphic != null) notesGraphic.color = baseGraphicColor;
        }

        private IEnumerator AnimateUnread()
        {
            float elapsed = 0f;
            while (elapsed < blinkDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed < duration)
                {
                    float progress = Mathf.Clamp01(elapsed / duration);
                    float wave = Mathf.Sin(progress * Mathf.PI);
                    animatedTarget.localScale = baseScale * Mathf.Lerp(1f, peakScale, wave);
                    animatedTarget.localRotation = baseRotation * Quaternion.Euler(
                        0f, 0f, Mathf.Sin(progress * Mathf.PI * 2f) * tiltDegrees * wave);
                }
                else
                {
                    animatedTarget.localScale = baseScale;
                    animatedTarget.localRotation = baseRotation;
                }

                float blinkProgress = Mathf.Repeat(elapsed, blinkCycleDuration) / blinkCycleDuration;
                float fade = (0.5f - 0.5f * Mathf.Cos(blinkProgress * Mathf.PI * 2f)) * blinkStrength;
                if (notesGraphic != null) notesGraphic.color = Color.Lerp(baseGraphicColor, blinkColor, fade);
                yield return null;
            }

            if (animatedTarget != null)
            {
                animatedTarget.localScale = baseScale;
                animatedTarget.localRotation = baseRotation;
            }
            if (notesGraphic != null) notesGraphic.color = baseGraphicColor;
            animationRoutine = null;
        }
    }
}
