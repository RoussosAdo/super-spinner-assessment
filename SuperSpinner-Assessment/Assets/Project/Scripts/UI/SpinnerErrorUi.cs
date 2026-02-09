using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SuperSpinner.UI
{
    public sealed class SpinnerErrorUi : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text text;
        [SerializeField] private float fadeIn = 0.2f;
        [SerializeField] private float fadeOut = 0.2f;

        private void Awake()
        {
            HideInstant();
        }

        public void Show(string message)
        {
            if (group == null || text == null) return;

            group.DOKill();
            text.text = message;

            group.gameObject.SetActive(true);
            group.alpha = 0f;
            group.DOFade(1f, fadeIn).SetEase(Ease.OutQuad);
        }

        public void Hide()
        {
            if (group == null) return;

            group.DOKill();
            group.DOFade(0f, fadeOut).SetEase(Ease.OutQuad)
                .OnComplete(HideInstant);
        }

        public void HideInstant()
        {
            if (group == null) return;
            group.alpha = 0f;
            group.gameObject.SetActive(false);
            if (text != null) text.text = "";
        }
    }
}
