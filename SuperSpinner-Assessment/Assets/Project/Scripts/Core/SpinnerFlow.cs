using DG.Tweening;
using UniRx;
using UnityEngine;
using SuperSpinner.Networking;
using SuperSpinner.UI;

namespace SuperSpinner.Core
{
    public sealed class SpinnerFlow : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SpinnerView view;
        [SerializeField] private RectTransform spinnerRoot;
        [SerializeField] private GameObject tapOverlay;

        private SpinnerApiService api;
        private readonly CompositeDisposable cd = new();

        // Virtual “total travel” που αυξάνει συνεχώς
        private float travel;

        private void Awake()
        {
            api = new SpinnerApiService();
        }

        public void EnableTap()
        {
            if (tapOverlay != null)
                tapOverlay.SetActive(true);
        }

        public void OnTap()
        {
            if (tapOverlay != null)
                tapOverlay.SetActive(false);

            spinnerRoot.DOScale(1.15f, 0.35f)
                .SetEase(Ease.OutBack)
                .OnComplete(StartSpin);
        }

        // Καλείται από Bootstrap αμέσως μετά το BuildReel() + SetIdlePosition()
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
                    err => Debug.LogError(err)
                )
                .AddTo(cd);
        }

        private void PlaySpinAnimation(int result)
        {
            if (view == null)
            {
                Debug.LogError("SpinnerFlow: view is NULL (assign SpinnerView in Inspector).");
                return;
            }

            float loopH = view.LoopHeight;
            if (loopH <= 0.01f)
            {
                Debug.LogError("SpinnerFlow: LoopHeight invalid. Check UniqueCount/itemSpacing.");
                return;
            }

            float targetMod = view.GetTargetModYForValue(result);
            float currentMod = SpinnerView.Mod(travel, loopH);

            float deltaToTarget = targetMod - currentMod;
            if (deltaToTarget < 0) deltaToTarget += loopH;

            int loops = 6;
            float endTravel = travel + loops * loopH + deltaToTarget;

            DOTween.Kill(view.ReelContent);

            Sequence s = DOTween.Sequence();

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
                ).SetEase(Ease.InOutCubic)
            );

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
                ).SetEase(Ease.OutQuad)
            );

            s.Append(spinnerRoot.DOScale(1f, 0.3f).SetEase(Ease.OutQuad));
        }

        private void OnDestroy()
        {
            cd.Dispose();
        }
    }
}
