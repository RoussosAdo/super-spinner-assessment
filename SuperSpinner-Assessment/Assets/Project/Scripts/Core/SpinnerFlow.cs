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
        private enum State { Idle, Spinning, ShowingResult }

        [Header("Refs")]
        [SerializeField] private SpinnerView view;
        [SerializeField] private RectTransform spinnerRoot;
        [SerializeField] private GameObject tapOverlay;

        [Header("Result UI")]
        [SerializeField] private TMP_Text resultText;

        [Header("Tuning")]
        [SerializeField] private int loops = 6;
        [SerializeField] private float spinDuration = 2.8f;
        [SerializeField] private float settleDuration = 0.25f;
        [SerializeField] private float zoomInScale = 1.15f;
        [SerializeField] private float zoomInDuration = 0.35f;
        [SerializeField] private float zoomOutDuration = 0.30f;

        [Header("Result Anim")]
        [SerializeField] private float resultFadeIn = 0.20f;
        [SerializeField] private float resultPulseUp = 1.10f;
        [SerializeField] private float resultPulseIn = 0.18f;
        [SerializeField] private float resultPulseOut = 0.12f;

        [Header("End Micro FX")]
        [SerializeField] private float endShakeDuration = 0.18f;
        [SerializeField] private float endShakeStrength = 10f;
        [SerializeField] private SuperSpinner.Audio.SpinnerAudio audioFx;
        [SerializeField] private CanvasGroup leftPointer;
        [SerializeField] private CanvasGroup rightPointer;



        private SpinnerApiService api;
        private readonly CompositeDisposable cd = new();

        private float travel;
        private State state = State.Idle;

        private void Awake()
        {
            api = new SpinnerApiService();
            SetTapVisible(true);
            HideResultInstant();
            state = State.Idle;
        }

        public void EnableTap()
        {
            state = State.Idle;
            SetTapVisible(true);
        }

        public void OnTap()
        {
            // Το tap απλά κλείνει το αποτέλεσμα και ξαναμπαίνει idle
            if (state == State.ShowingResult)
            {
                HideResultInstant();
                EnableTap();
                return;
            }

            if (state == State.Spinning) return;

            // Idle
            state = State.Spinning;
            audioFx?.PlaySpinLoop();
            SetTapVisible(false);
            HideResultInstant();

            spinnerRoot.DOKill();
            spinnerRoot.DOScale(zoomInScale, zoomInDuration)
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
                        state = State.Idle;
                        SetTapVisible(true);
                    }
                )
                .AddTo(cd);
        }

        private void PlaySpinAnimation(int result)
        {
            if (view == null)
            {
                Debug.LogError("SpinnerFlow: view is NULL.");
                state = State.Idle;
                SetTapVisible(true);
                return;
            }

            float loopH = view.LoopHeight;
            if (loopH <= 0.01f)
            {
                Debug.LogError("SpinnerFlow: LoopHeight invalid.");
                state = State.Idle;
                SetTapVisible(true);
                return;
            }

            float targetMod = view.GetTargetModYForValue(result);
            float currentMod = SpinnerView.Mod(travel, loopH);

            float deltaToTarget = targetMod - currentMod;
            if (deltaToTarget < 0) deltaToTarget += loopH;

            float endTravel = travel + (loops * loopH) + deltaToTarget;

            view.ReelContent.DOKill();

            Sequence s = DOTween.Sequence();

            // SPIN
            s.Append(DOTween.To(
                () => travel,
                x =>
                {
                    travel = x;
                    float y = SpinnerView.Mod(travel, loopH);
                    view.ReelContent.anchoredPosition = new Vector2(0f, y);
                    view.UpdateHighlight();
                },
                endTravel,
                spinDuration
            ).SetEase(Ease.InOutCubic));

            // SETTLE (micro)
            s.Append(DOTween.To(
                () => travel,
                x =>
                {
                    travel = x;
                    float y = SpinnerView.Mod(travel, loopH);
                    view.ReelContent.anchoredPosition = new Vector2(0f, y);
                    view.UpdateHighlight();
                },
                endTravel,
                settleDuration
            ).SetEase(Ease.OutQuad));

            // END FX
            s.AppendCallback(() =>
            {
                FlashPointer(leftPointer);
                FlashPointer(rightPointer);

                spinnerRoot.DOKill();
                spinnerRoot.DOShakeAnchorPos(endShakeDuration, endShakeStrength, 12, 90, false, true);
                audioFx?.StopSpinLoop();
                audioFx?.PlayStop();
                audioFx?.PlayWin();

                ShowResult(result);
            });

            

            // ZOOM OUT
            s.Append(spinnerRoot.DOScale(1f, zoomOutDuration).SetEase(Ease.OutQuad));

            // State -> ShowingResult 
            s.AppendCallback(() => state = State.ShowingResult);
        }

        private void FlashPointer(CanvasGroup cg)
        {
            if (cg == null) return;
            cg.DOKill();
            cg.alpha = 1f;
            cg.DOFade(0.2f, 0.06f).SetLoops(6, LoopType.Yoyo);
        }


        private void ShowResult(int result)
        {
            if (resultText == null) return;

            resultText.DOKill();
            resultText.rectTransform.DOKill();

            resultText.gameObject.SetActive(true);
            resultText.text = result.ToString("N0");

            // Fade in
            var c = resultText.color;
            c.a = 0f;
            resultText.color = c;

            // Pulse
            var rt = resultText.rectTransform;
            rt.localScale = Vector3.one * 0.92f;

            Sequence r = DOTween.Sequence();
            r.Append(resultText.DOFade(1f, resultFadeIn).SetEase(Ease.OutQuad));
            r.Join(rt.DOScale(resultPulseUp, resultPulseIn).SetEase(Ease.OutBack));
            r.Append(rt.DOScale(1f, resultPulseOut).SetEase(Ease.OutQuad));
        }

        private void HideResultInstant()
        {
            if (resultText == null) return;

            resultText.DOKill();
            resultText.rectTransform.DOKill();

            resultText.text = "";
            var c = resultText.color;
            c.a = 0f;
            resultText.color = c;

            resultText.gameObject.SetActive(false);
        }

        private void SetTapVisible(bool visible)
        {
            if (tapOverlay != null)
                tapOverlay.SetActive(visible);
        }

        private void OnDestroy()
        {
            cd.Dispose();
        }
    }
}
