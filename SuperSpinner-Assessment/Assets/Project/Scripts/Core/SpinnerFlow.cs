using DG.Tweening;
using UniRx;
using UnityEngine;
using SuperSpinner.Networking;
using SuperSpinner.UI;
using TMPro;

namespace SuperSpinner.Core
{
    public sealed class SpinnerFlow : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SpinnerView view;
        [SerializeField] private RectTransform spinnerRoot;
        [SerializeField] private GameObject tapOverlay;

        [Header("Result UI")]
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private float resultFadeIn = 0.25f;
        [SerializeField] private float resultHoldSeconds = 1.0f;

        private SpinnerApiService api;
        private readonly CompositeDisposable cd = new();

        private float travel;
        private bool isSpinning;

        private void Awake()
        {
            api = new SpinnerApiService();
            HideResultInstant();
        }

        public void EnableTap()
        {
            if (tapOverlay != null)
                tapOverlay.SetActive(true);

            isSpinning = false;
        }

        public void OnTap()
        {
            if (isSpinning) return;

            isSpinning = true;
            HideResultInstant();

            if (tapOverlay != null)
                tapOverlay.SetActive(false);

            spinnerRoot.DOScale(1.15f, 0.35f)
                .SetEase(Ease.OutBack)
                .OnComplete(StartSpin);
        }

        public void ResetTravelToCurrent()
        {
            if (view == null) return;

            float loopH = view.LoopHeight;
            if (loopH <= 0.01f) return;

            float currentY = view.ReelContent.anchoredPosition.y;
            travel = SpinnerView.Mod(currentY, loopH);
        }

        private void StartSpin()
        {
            api.Spin()
                .ObserveOnMainThread()
                .Subscribe(
                    res =>
                    {
                        Debug.Log("SPIN RESULT: " + res.spinnerValue);
                        PlaySpinAnimation(res.spinnerValue);
                    },
                    err =>
                    {
                        Debug.LogError(err);
                        isSpinning = false;
                        EnableTap();
                    }
                )
                .AddTo(cd);
        }

        private void PlaySpinAnimation(int result)
        {
            if (view == null)
            {
                Debug.LogError("SpinnerFlow: view is NULL.");
                isSpinning = false;
                EnableTap();
                return;
            }

            float loopH = view.LoopHeight;
            if (loopH <= 0.01f)
            {
                Debug.LogError("SpinnerFlow: LoopHeight invalid.");
                isSpinning = false;
                EnableTap();
                return;
            }

            float targetMod = view.GetTargetModYForValue(result);
            float currentMod = SpinnerView.Mod(travel, loopH);

            float deltaToTarget = targetMod - currentMod;
            if (deltaToTarget < 0) deltaToTarget += loopH;

            int loops = 6;
            float endTravel = travel + loops * loopH + deltaToTarget;

            DOTween.Kill(view.ReelContent);
            DOTween.Kill(spinnerRoot);
            if (resultText != null) DOTween.Kill(resultText);

            Sequence s = DOTween.Sequence();

            // Spin
            s.Append(DOTween.To(
                () => travel,
                x =>
                {
                    travel = x;
                    float y = SpinnerView.Mod(travel, loopH);
                    view.ReelContent.anchoredPosition = new Vector2(0f, y);
                },
                endTravel,
                2.8f
            ).SetEase(Ease.InOutCubic));

            // Micro settle
            s.Append(DOTween.To(
                () => travel,
                x =>
                {
                    travel = x;
                    float y = SpinnerView.Mod(travel, loopH);
                    view.ReelContent.anchoredPosition = new Vector2(0f, y);
                },
                endTravel,
                0.25f
            ).SetEase(Ease.OutQuad));

            // Show result
            s.AppendCallback(() => ShowResult(result));

            // Zoom out
            s.Append(spinnerRoot.DOScale(1f, 0.3f).SetEase(Ease.OutQuad));

            // Hold result λίγο (να προλάβει να φανεί)
            s.AppendInterval(resultHoldSeconds);

            // Re-enable tap
            s.AppendCallback(() =>
            {
                HideResultInstant();      //  κρύβει το result
                isSpinning = false;
                EnableTap();              //  μετά δείχνει tap overlay
            });

        }

        private void ShowResult(int result)
        {
            if (resultText == null) return;

            // Βεβαιώσου ότι δεν σε σκεπάζει το Tap overlay
            if (tapOverlay != null) tapOverlay.SetActive(false);

            resultText.gameObject.SetActive(true);
            resultText.text = result.ToString("N0");

            // start invisible
            var c = resultText.color;
            c.a = 0f;
            resultText.color = c;

            // Fade + pulse
            Sequence r = DOTween.Sequence();
            r.Append(resultText.DOFade(1f, resultFadeIn).SetEase(Ease.OutQuad));

            var rt = resultText.rectTransform;
            rt.localScale = Vector3.one * 0.9f;
            r.Join(rt.DOScale(1.08f, 0.18f).SetEase(Ease.OutBack));
            r.Append(rt.DOScale(1.0f, 0.12f).SetEase(Ease.OutQuad));
        }

        private void HideResultInstant()
        {
            if (resultText == null) return;

            resultText.text = "";
            var c = resultText.color;
            c.a = 0f;
            resultText.color = c;

            // Μπορείς να το αφήσεις active, δεν πειράζει
            resultText.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            cd.Dispose();
        }
    }
}
