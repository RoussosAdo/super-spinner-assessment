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
        [SerializeField] private float settleDuration = 0.25f;
        [SerializeField] private float zoomInScale = 1.15f;
        [SerializeField] private float zoomInDuration = 0.35f;
        [SerializeField] private float zoomOutDuration = 0.30f;

        [Header("Slow Motion")]
        [SerializeField, Range(0f, 0.4f)] private float slowMotionPortion = 0.2f;
        [SerializeField] private float slowMotionDuration = 1.2f;
        [SerializeField] private float fastSpinDuration = 2.0f;

        [Header("Extra Anim")]
        [SerializeField] private RectTransform outerRing;
        [SerializeField] private float outerRingRpm = 180f;

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

        [SerializeField] private SuperSpinner.UI.SpinnerErrorUi errorUi;
        [SerializeField] private float spinTimeoutSeconds = 6f;
        [SerializeField] private int spinRetries = 0; 

        [Header("Glow FX")]
        [SerializeField] private CanvasGroup resultGlow;
        [SerializeField] private float glowFadeIn = 0.15f;
        [SerializeField] private float glowFadeOut = 0.25f;
        [SerializeField] private float glowHold = 0.35f;
        [SerializeField] private float glowScaleUp = 1.08f;

        [SerializeField] private ParticleSystem coinRingBurst;

        [Header("Win Flash")]
        [SerializeField] private CanvasGroup winFlash;
        [SerializeField] private float flashIn = 0.06f;
        [SerializeField] private float flashOut = 0.22f;
        [SerializeField] private float hitFreeze = 0.04f;





        private SpinnerApiService api;
        private readonly CompositeDisposable cd = new();

        private float travel;
        private State state = State.Idle;

        private int lastCenterIndex = -1;

        // FIX: keep refs to kill running tweens/sequences
        private Sequence activeSpinSeq;
        private Tween tapTween;
        private Tween ringTween;

        private void Awake()
        {
            api = new SpinnerApiService();
            HideResultInstant();
            SetTapVisible(true);
            state = State.Idle;

            if (resultGlow != null)
            {
                resultGlow.alpha = 0f;
                resultGlow.gameObject.SetActive(false);
            }

        }

        public void EnableTap()
        {
            state = State.Idle;
            SetTapVisible(true);
        }

        public void ResetTravelToCurrent()
        {
            if (view == null) return;

            float loopH = view.LoopHeight;
            if (loopH <= 0.01f) return;

            float currentY = view.ReelContent.anchoredPosition.y;
            travel = SpinnerView.Mod(currentY, loopH);
        }

        public void OnTap()
        {
            errorUi?.HideInstant();

            // If showing result -> just close result and go idle
            if (state == State.ShowingResult)
            {
                HideResultInstant();
                EnableTap();
                return;
            }

            // Block double tap
            if (state == State.Spinning) return;

            if (view != null) lastCenterIndex = view.GetCenterIndex();

            // Enter spinning state immediately (hard lock)
            state = State.Spinning;

            // Kill anything that could re-trigger
            tapTween?.Kill();
            activeSpinSeq?.Kill();
            ringTween?.Kill();

            audioFx?.PlaySpinLoop();
            SetTapVisible(false);
            HideResultInstant();

            spinnerRoot.DOKill();

            // Keep reference so we can kill if needed
            tapTween = spinnerRoot.DOScale(zoomInScale, zoomInDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(StartSpin);
        }

        private void StartSpin()
        {
            api.Spin()
                .Take(1)
                .Timeout(System.TimeSpan.FromSeconds(spinTimeoutSeconds))
                .Retry(spinRetries)
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

                    audioFx?.StopSpinLoop();
                    errorUi?.Show("Spin failed. Tap to try again.");

                    // βγάζουμε τον χρήστη σε idle
                    state = State.Idle;
                    SetTapVisible(true);
                }
            )
            .AddTo(cd);
        }


        private Tween StartOuterRingSpin(float duration)
        {
            if (outerRing == null) return null;

            outerRing.DOKill();

            // clockwise for UI is usually negative Z
            float degrees = -outerRingRpm * duration;

            return outerRing
                .DORotate(new Vector3(0f, 0f, degrees), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear);
        }

        private void PlaySpinAnimation(int result)
        {
            if (view == null)
            {
                Debug.LogError("SpinnerFlow: view is NULL.");
                audioFx?.StopSpinLoop();
                state = State.Idle;
                SetTapVisible(true);
                return;
            }

            float loopH = view.LoopHeight;
            if (loopH <= 0.01f)
            {
                Debug.LogError("SpinnerFlow: LoopHeight invalid.");
                audioFx?.StopSpinLoop();
                state = State.Idle;
                SetTapVisible(true);
                return;
            }

            // FIX: kill any previous running sequences/tweens
            activeSpinSeq?.Kill();
            ringTween?.Kill();
            view.ReelContent.DOKill();
            spinnerRoot.DOKill();

            float targetMod = view.GetTargetModYForValue(result);
            float currentMod = SpinnerView.Mod(travel, loopH);

            float deltaToTarget = targetMod - currentMod;
            if (deltaToTarget < 0) deltaToTarget += loopH;

            float endTravel = travel + (loops * loopH) + deltaToTarget;

            // total time for ring spin: fast + slow + tiny settle bounce
            float totalSpinTime = fastSpinDuration + slowMotionDuration + 0.12f;
            ringTween = StartOuterRingSpin(totalSpinTime);

            // --- Slow motion split ---
            float totalDelta = endTravel - travel;
            float slowDelta = totalDelta * slowMotionPortion;
            float fastEndTravel = endTravel - slowDelta;

            Sequence s = DOTween.Sequence();
            activeSpinSeq = s;

            // 1) FAST spin
            s.Append(DOTween.To(
                () => travel,
                x =>
                {
                    travel = x;
                    float y = SpinnerView.Mod(travel, loopH);
                    view.ReelContent.anchoredPosition = new Vector2(0f, y);
                    view.UpdateHighlight();
                    TickIfCenterChanged();
                },
                fastEndTravel,
                fastSpinDuration
            ).SetEase(Ease.InOutCubic));

            // 2) SLOW-MO
            s.Append(DOTween.To(
                () => travel,
                x =>
                {
                    travel = x;
                    float y = SpinnerView.Mod(travel, loopH);
                    view.ReelContent.anchoredPosition = new Vector2(0f, y);
                    view.UpdateHighlight();
                    TickIfCenterChanged();
                },
                endTravel,
                slowMotionDuration
            ).SetEase(Ease.OutQuart));

            // Stop outer ring before stop FX
            s.AppendCallback(() =>
            {
                ringTween?.Kill();
                ringTween = null;
            });

            // 3) VISUAL micro settle (bounce only, no new travel)
            s.AppendCallback(() =>
            {
                view.ReelContent.DOKill();
                float baseY = view.ReelContent.anchoredPosition.y;

                view.ReelContent.DOAnchorPosY(baseY + 6f, 0.06f)
                    .SetEase(Ease.OutQuad)
                    .SetLoops(2, LoopType.Yoyo);
            });

            // END FX + result
            s.AppendCallback(() =>
            {
                FlashPointer(leftPointer);
                FlashPointer(rightPointer);

                spinnerRoot.DOKill();
                spinnerRoot.DOShakeAnchorPos(endShakeDuration, endShakeStrength, 12, 90, false, true);

                audioFx?.StopSpinLoop();
                audioFx?.PlayStop();
                audioFx?.PlayWin();

                PlayWinFlash();
                ShowResult(result);
            });

            // ZOOM OUT
            s.Append(spinnerRoot.DOScale(1f, zoomOutDuration).SetEase(Ease.OutQuad));

            // Done -> showing result
            s.AppendCallback(() =>
            {
                state = State.ShowingResult;
                activeSpinSeq = null;
            });

            // Safety: if sequence is killed, clean refs
            s.OnKill(() =>
            {
                if (activeSpinSeq == s) activeSpinSeq = null;
            });
        }

        private void TickIfCenterChanged()
        {
            if (view == null || audioFx == null) return;

            int idx = view.GetCenterIndex();
            if (idx != lastCenterIndex)
            {
                lastCenterIndex = idx;
                audioFx.PlayTick();
            }
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
            if (coinRingBurst != null)
            {
                coinRingBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                coinRingBurst.Play();
            }

            PlayGlow();

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

        private void PlayWinFlash()
{
    if (winFlash == null) return;

    winFlash.DOKill();
    winFlash.alpha = 0f;
    winFlash.gameObject.SetActive(true);

    Sequence f = DOTween.Sequence();

    // impact frame freeze
    f.AppendCallback(() => Time.timeScale = 0f);
    f.AppendInterval(hitFreeze);
    f.AppendCallback(() => Time.timeScale = 1f);

    // flash
    f.Append(winFlash.DOFade(1f, flashIn));
    f.Append(winFlash.DOFade(0f, flashOut));

    f.OnComplete(() => winFlash.gameObject.SetActive(false));
}


        private void PlayGlow()
{
    if (resultGlow == null) return;

    resultGlow.DOKill();
    resultGlow.transform.DOKill();

    resultGlow.gameObject.SetActive(true);
    resultGlow.alpha = 0f;
    resultGlow.transform.localScale = Vector3.one * 0.9f;

    Sequence g = DOTween.Sequence();
    g.Append(resultGlow.DOFade(1f, glowFadeIn).SetEase(Ease.OutQuad));
    g.Join(resultGlow.transform.DOScale(glowScaleUp, glowFadeIn).SetEase(Ease.OutQuad));
    g.AppendInterval(glowHold);
    g.Append(resultGlow.DOFade(0f, glowFadeOut).SetEase(Ease.OutQuad));
    g.Join(resultGlow.transform.DOScale(1.0f, glowFadeOut).SetEase(Ease.OutQuad));
    g.OnComplete(() => resultGlow.gameObject.SetActive(false));
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
            activeSpinSeq?.Kill();
            tapTween?.Kill();
            ringTween?.Kill();
            cd.Dispose();
        }
    }
}
