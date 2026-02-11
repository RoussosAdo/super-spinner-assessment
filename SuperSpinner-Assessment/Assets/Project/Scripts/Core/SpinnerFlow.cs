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

        [Header("Win Overlay")]
        [SerializeField] private CanvasGroup winOverlay;   // το panel/overlay (WinOverlay) με CanvasGroup
        [SerializeField] private GameObject winContent; 
        [SerializeField] private TMP_Text winTitleText;     // "BIG WIN!"
        [SerializeField] private TMP_Text winAmountText;    // το μεγάλο ποσό 
        [SerializeField] private TMP_Text winPromptText;    // "TAP TO CONTINUE"



        [Header("Win Tiers")]
        [SerializeField] private int bigWinThreshold = 10000;
        [SerializeField] private int megaWinThreshold = 100000;



        [Header("End Micro FX")]
        [SerializeField] private float endShakeDuration = 0.18f;
        [SerializeField] private float endShakeStrength = 10f;
        [SerializeField] private SuperSpinner.Audio.SpinnerAudio audioFx;
        [SerializeField] private CanvasGroup leftPointer;
        [SerializeField] private CanvasGroup rightPointer;  

        [Header("Error UI")]
        [SerializeField] private SuperSpinner.UI.SpinnerErrorUi errorUi;
        [SerializeField] private float spinTimeoutSeconds = 6f;
        [SerializeField] private int spinRetries = 0;

        [Header("Glow FX")]
        [SerializeField] private CanvasGroup resultGlow;
        [SerializeField] private float glowFadeIn = 0.15f;
        [SerializeField] private float glowFadeOut = 0.25f;
        [SerializeField] private float glowHold = 0.35f;
        [SerializeField] private float glowScaleUp = 1.08f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem coinRingBurst;

        [Header("Win Flash")]
        [SerializeField] private CanvasGroup winFlash;
        [SerializeField] private float flashIn = 0.06f;
        [SerializeField] private float flashOut = 0.22f;
        [SerializeField] private float hitFreeze = 0.04f;

        // -------------------------
        // IDLE AMBIENCE (NEW)
        // -------------------------
        [Header("Idle Ambience")]
        [SerializeField] private CanvasGroup idleGlow;        // optional: CanvasGroup σε ένα idle glow image
        [SerializeField] private CanvasGroup tapToSpinGroup;  // optional: CanvasGroup στο "TAP TO SPIN"
        [SerializeField] private float idleRingRpm = 25f;     // αργό rotate στο idle
        [SerializeField] private float idleBreathScale = 1.02f;
        [SerializeField] private float idleBreathDuration = 1.4f;
        [SerializeField] private float idleGlowMin = 0.03f;
        [SerializeField] private float idleGlowMax = 0.10f;
        [SerializeField] private float idleGlowPulseDuration = 1.2f;
        [SerializeField] private float idleTapPulseMin = 0.55f;
        [SerializeField] private float idleTapPulseMax = 1.00f;
        [SerializeField] private float idleTapPulseDuration = 0.9f;

        [Header("Idle Particles")]
        [SerializeField] private ParticleSystem idleSparkles;
        [SerializeField] private ParticleSystem idleTwinkles;

        

        

        [Header("Count Up")]
        [SerializeField] private float smallCountTime = 0.55f;
        [SerializeField] private float bigCountTime = 1.1f;
        [SerializeField] private float megaCountTime = 1.8f;
        [SerializeField] private Ease countEase = Ease.OutCubic;

        [Header("Pointer Spin Anim")]
        [SerializeField] private RectTransform leftPointerRt;
        [SerializeField] private RectTransform rightPointerRt;
        [SerializeField] private float pointerDip = 10f;
        [SerializeField] private float pointerDipDuration = 0.08f;
        [SerializeField] private float pointerReturnDuration = 0.10f;
        [SerializeField] private float pointerLoopGap = 0.10f;

        private Tween leftPointerSpinTween;
        private Tween rightPointerSpinTween;

        private Vector2 leftPointerStartPos;
        private Vector2 rightPointerStartPos;

        [Header("Pointer Particles (Spin only)")]
        [SerializeField] private ParticleSystem leftPointerParticles;
        [SerializeField] private ParticleSystem rightPointerParticles;






        private SpinnerApiService api;
        private readonly CompositeDisposable cd = new();

        private float travel;
        private State state = State.Idle;
        private int lastCenterIndex = -1;

        // running refs
        private Sequence activeSpinSeq;
        private Tween tapTween;
        private Tween ringTween;

        // idle refs
        private Tween idleRingTween;
        private Tween idleBreathTween;
        private Tween idleGlowTween;
        private Tween idleTapTween;

        


private bool pointerPosCached;

private void Start()
{
    CachePointerStartPositions();
}

private void CachePointerStartPositions()
{
    if (pointerPosCached) return;

    // force UI layout to settle
    Canvas.ForceUpdateCanvases();

    if (leftPointerRt != null) leftPointerStartPos = leftPointerRt.anchoredPosition;
    if (rightPointerRt != null) rightPointerStartPos = rightPointerRt.anchoredPosition;

    pointerPosCached = true;
}


        private void Awake()
        {
            api = new SpinnerApiService();
            HideResultInstant();
            HideWinScreenInstant();
            SetTapVisible(true);
            state = State.Idle;

            if (resultGlow != null)
            {
                resultGlow.alpha = 0f;
                resultGlow.gameObject.SetActive(false);
            }

            if (winFlash != null)
            {
                winFlash.alpha = 0f;
                winFlash.gameObject.SetActive(false);
            }

            if (idleGlow != null)
            {
                idleGlow.alpha = 0f;
                idleGlow.gameObject.SetActive(false);
            }

            StartIdleAmbience();
        }

        public void EnableTap()
        {
            state = State.Idle;
            SetTapVisible(true);
            StartIdleAmbience();
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

            // If showing result -> close result and go idle
            if (state == State.ShowingResult)
            {
                HideWinScreenInstant();

                if (coinRingBurst != null)
                    coinRingBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                EnableTap();
                StopPointerParticles();
                return;
            }


            // Block double tap
            if (state == State.Spinning) return;

            if (view != null) lastCenterIndex = view.GetCenterIndex();

            // Enter spinning state immediately (hard lock)
            state = State.Spinning;
            StopIdleAmbience();
            StartPointerParticles();
            StartPointerSpinAnim();

            

            // Kill anything that could re-trigger
            tapTween?.Kill();
            activeSpinSeq?.Kill();
            ringTween?.Kill();

            audioFx?.PlaySpinLoop();
            SetTapVisible(false);
            HideResultInstant();

            spinnerRoot.DOKill();

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

                        StopPointerParticles();
                        StopPointerSpinAnim();
                        audioFx?.StopSpinLoop();
                        errorUi?.Show("Spin failed. Tap to try again.");

                        state = State.Idle;
                        SetTapVisible(true);
                        StartIdleAmbience();
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
                StartIdleAmbience();
                return;
            }

            float loopH = view.LoopHeight;
            if (loopH <= 0.01f)
            {
                Debug.LogError("SpinnerFlow: LoopHeight invalid.");
                audioFx?.StopSpinLoop();
                state = State.Idle;
                SetTapVisible(true);
                StartIdleAmbience();
                return;
            }

            // kill previous
            activeSpinSeq?.Kill();
            ringTween?.Kill();
            view.ReelContent.DOKill();
            spinnerRoot.DOKill();

            float targetMod = view.GetTargetModYForValue(result);
            float currentMod = SpinnerView.Mod(travel, loopH);

            float deltaToTarget = targetMod - currentMod;
            if (deltaToTarget < 0) deltaToTarget += loopH;

            float endTravel = travel + (loops * loopH) + deltaToTarget;

            float totalSpinTime = fastSpinDuration + slowMotionDuration + 0.12f;
            ringTween = StartOuterRingSpin(totalSpinTime);

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

            // 3) VISUAL micro settle
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
                StopPointerParticles();
                StopPointerSpinAnim();

                FlashPointer(leftPointer);
                FlashPointer(rightPointer);

                spinnerRoot.DOKill();
                spinnerRoot.DOShakeAnchorPos(endShakeDuration, endShakeStrength, 12, 90, false, true);

                audioFx?.StopSpinLoop();
                audioFx?.PlayStop();
                audioFx?.PlayWin();

                PlayWinFlash();
                PlayWinPresentation(result);
            });

            // ZOOM OUT
            s.Append(spinnerRoot.DOScale(1f, zoomOutDuration).SetEase(Ease.OutQuad));

            // Done -> showing result
            s.AppendCallback(() =>
            {
                state = State.ShowingResult;
                activeSpinSeq = null;
            });

            s.OnKill(() =>
            {
                if (activeSpinSeq == s) activeSpinSeq = null;
            });
        }

        private void StartPointerParticles()
{
    if (leftPointerParticles != null)
    {
        leftPointerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        leftPointerParticles.Play(true);
    }

    if (rightPointerParticles != null)
    {
        rightPointerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        rightPointerParticles.Play(true);
    }
}

private void StopPointerParticles()
{
    if (leftPointerParticles != null)
        leftPointerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

    if (rightPointerParticles != null)
        rightPointerParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
}


        private void StartPointerSpinAnim()
{
    CachePointerStartPositions();
    StopPointerSpinAnim();

    if (leftPointerRt != null)
    {
        leftPointerRt.anchoredPosition = leftPointerStartPos;

        leftPointerSpinTween = DOTween.Sequence()
            .Append(leftPointerRt.DOAnchorPosY(leftPointerStartPos.y - pointerDip, pointerDipDuration).SetEase(Ease.OutQuad))
            .Append(leftPointerRt.DOAnchorPosY(leftPointerStartPos.y, pointerReturnDuration).SetEase(Ease.OutQuad))
            .AppendInterval(pointerLoopGap)
            .SetLoops(-1, LoopType.Restart);
    }

    if (rightPointerRt != null)
    {
        rightPointerRt.anchoredPosition = rightPointerStartPos;

        // μικρό offset για να μην είναι 100% sync (πιο “alive”)
        rightPointerSpinTween = DOTween.Sequence()
            .AppendInterval(0.03f)
            .Append(rightPointerRt.DOAnchorPosY(rightPointerStartPos.y - pointerDip, pointerDipDuration).SetEase(Ease.OutQuad))
            .Append(rightPointerRt.DOAnchorPosY(rightPointerStartPos.y, pointerReturnDuration).SetEase(Ease.OutQuad))
            .AppendInterval(pointerLoopGap)
            .SetLoops(-1, LoopType.Restart);
    }
}

private void StopPointerSpinAnim()
{
    leftPointerSpinTween?.Kill();
    rightPointerSpinTween?.Kill();
    leftPointerSpinTween = null;
    rightPointerSpinTween = null;

    if (!pointerPosCached) return;

    if (leftPointerRt != null) leftPointerRt.anchoredPosition = leftPointerStartPos;
    if (rightPointerRt != null) rightPointerRt.anchoredPosition = rightPointerStartPos;
}


        private Sequence winSeq;

        private void ShowWinOverlay(bool show)
{
    if (winOverlay == null) return;

    // IMPORTANT: το SetActive(true) στο parent είναι αυτό που ξε-γκριζάρει τα παιδιά
    winOverlay.gameObject.SetActive(show);

    if (winContent != null) winContent.SetActive(show);
    if (winTitleText != null) winTitleText.gameObject.SetActive(show);
    if (winAmountText != null) winAmountText.gameObject.SetActive(show);
    if (winPromptText != null) winPromptText.gameObject.SetActive(show);

    winOverlay.DOKill();
    winOverlay.alpha = show ? 1f : 0f;
}


private void PlayWinPresentation(int result)
{

    ShowWinOverlay(true);
    // kill παλιό win sequence
    winSeq?.Kill();
    winSeq = null;

    // (προαιρετικό) σταμάτα resultText αν χρησιμοποιείς overlay
    // HideResultInstant();

    // Tier
    WinTier tier = GetTier(result);

    string title = tier switch
    {
        WinTier.Mega => "MEGA WIN!",
        WinTier.Big  => "BIG WIN!",
        _            => "YOU WIN!"
    };

    float t = tier switch
    {
        WinTier.Mega => megaCountTime,
        WinTier.Big  => bigCountTime,
        _            => smallCountTime
    };

    // Δείξε overlay
    ShowWinScreenBase(title);

    // Παίξε έξτρα FX ανά tier (λίγο/πολύ)
    // Small: normal glow/coins
    // Big: +1 burst + λίγο παραπάνω shake
    // Mega: +2 bursts + πιο δυνατό glow
    if (tier == WinTier.Big)
    {
        // π.χ. δεύτερο coin burst (αν έχεις)
        // coinRingBurst?.Play();
        spinnerRoot?.DOPunchScale(Vector3.one * 0.06f, 0.22f, 10, 1f);
    }
    else if (tier == WinTier.Mega)
    {
        spinnerRoot?.DOPunchScale(Vector3.one * 0.09f, 0.28f, 12, 1f);
        // εδώ μπορείς να κάνεις και δεύτερο flash / extra particles
    }

    // Count-up + end punch
    winSeq = DOTween.Sequence().SetUpdate(true);

    winSeq.AppendCallback(() =>
{
    if (coinRingBurst != null)
    {
        coinRingBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        coinRingBurst.Play();
    }

    PlayGlow();
});


    winSeq.Append(CountUpTo(result, t));

    winSeq.AppendCallback(() =>
    {
        // τελικό “clink”
        if (winAmountText != null)
        {
            var rt = winAmountText.rectTransform;
            rt.DOKill();
            rt.localScale = Vector3.one * 0.95f;
            DOTween.Sequence().SetUpdate(true)
                .Append(rt.DOScale(1.12f, 0.18f).SetEase(Ease.OutBack))
                .Append(rt.DOScale(1.0f, 0.12f).SetEase(Ease.OutQuad));
        }

        // εδώ αφήνεις τον χρήστη σε ShowingResult
        state = State.ShowingResult;
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

        private enum WinTier { Small, Big, Mega }

        private WinTier GetTier(int value)
        {
            if (value >= megaWinThreshold) return WinTier.Mega;
            if (value >= bigWinThreshold) return WinTier.Big;
            return WinTier.Small;
        }

    private void HideWinScreenInstant()
    {
    if (winOverlay != null)
    {
        winOverlay.DOKill();
        winOverlay.alpha = 0f;
        winOverlay.gameObject.SetActive(false);
    }

    if (winTitleText != null)
    {
        winTitleText.DOKill();
        winTitleText.text = "";
        winTitleText.gameObject.SetActive(false);
    }

    if (winAmountText != null)
    {
        winAmountText.DOKill();
        winAmountText.text = "";
        winAmountText.gameObject.SetActive(false);
    }

    if (winPromptText != null)
    {
        winPromptText.DOKill();
        winPromptText.text = "";
        winPromptText.gameObject.SetActive(false);
    }
}


        private Tween CountUpTo(int target, float duration)
{
    if (winAmountText == null) return null;

    int current = 0;
    winAmountText.text = "0";

    // Unscaled ώστε να μη σπάει από hit-freeze
    return DOTween.To(() => current, x =>
    {
        current = x;
        winAmountText.text = current.ToString("N0");
    }, target, duration).SetEase(countEase).SetUpdate(true);
}


private void ShowWinScreenBase(string title)
{
    if (winOverlay != null)
    {
        winOverlay.DOKill();
        winOverlay.gameObject.SetActive(true);
        winOverlay.alpha = 0f;
        winOverlay.DOFade(1f, 0.18f).SetEase(Ease.OutQuad);
    }

    if (winTitleText != null)
    {
        winTitleText.gameObject.SetActive(true);
        winTitleText.text = title;

        var c = winTitleText.color;
        c.a = 1f;
        winTitleText.color = c;
    }

    if (winPromptText != null)
    {
        winPromptText.gameObject.SetActive(true);
        winPromptText.text = "TAP TO CONTINUE";

        var c = winPromptText.color;
        c.a = 0f;
        winPromptText.color = c;

        winPromptText.DOFade(1f, 0.18f).SetDelay(0.15f).SetEase(Ease.OutQuad);
    }
}


        private void PlayWinFlash()
        {
            if (winFlash == null) return;

            winFlash.DOKill();
            winFlash.alpha = 0f;
            winFlash.gameObject.SetActive(true);

            Sequence f = DOTween.Sequence();

            // IMPORTANT: intervals must ignore timescale if you freeze time
            f.SetUpdate(true);

            // impact frame freeze
            f.AppendCallback(() => Time.timeScale = 0f);
            f.AppendInterval(hitFreeze);
            f.AppendCallback(() => Time.timeScale = 1f);

            // flash
            f.Append(winFlash.DOFade(1f, flashIn).SetUpdate(true));
            f.Append(winFlash.DOFade(0f, flashOut).SetUpdate(true));

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

        // -------------------------
        // IDLE AMBIENCE
        // -------------------------
        private void StartIdleAmbience()
        {
            StopIdleAmbience();

            // OuterRing slow rotate
            if (outerRing != null)
            {
                float dur = 60f / Mathf.Max(1f, idleRingRpm);
                idleRingTween = outerRing
                    .DORotate(new Vector3(0f, 0f, -360f), dur, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart);
            }

            // Root breathing
            if (spinnerRoot != null)
            {
                spinnerRoot.DOKill();
                spinnerRoot.localScale = Vector3.one;

                idleBreathTween = spinnerRoot
                    .DOScale(idleBreathScale, idleBreathDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            // Idle glow pulse (optional)
            if (idleGlow != null)
            {
                idleGlow.DOKill();
                idleGlow.gameObject.SetActive(true);
                idleGlow.alpha = idleGlowMin;

                idleGlowTween = DOTween.To(
                        () => idleGlow.alpha,
                        a => idleGlow.alpha = a,
                        idleGlowMax,
                        idleGlowPulseDuration
                    )
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            // Tap pulse (optional)
            if (tapToSpinGroup != null)
            {
                tapToSpinGroup.DOKill();
                tapToSpinGroup.alpha = idleTapPulseMax;

                idleTapTween = DOTween.To(
                        () => tapToSpinGroup.alpha,
                        a => tapToSpinGroup.alpha = a,
                        idleTapPulseMin,
                        idleTapPulseDuration
                    )
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            idleSparkles?.Play();
            idleTwinkles?.Play();
        }

        private void StopIdleAmbience()
        {

            idleSparkles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            idleTwinkles?.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            idleRingTween?.Kill();
            idleRingTween = null;

            idleBreathTween?.Kill();
            idleBreathTween = null;

            idleGlowTween?.Kill();
            idleGlowTween = null;

            idleTapTween?.Kill();
            idleTapTween = null;

            if (spinnerRoot != null) spinnerRoot.localScale = Vector3.one;

            if (idleGlow != null)
            {
                idleGlow.alpha = 0f;
                idleGlow.gameObject.SetActive(false);
            }

            if (tapToSpinGroup != null)
            {
                tapToSpinGroup.alpha = 1f;
            }
        }

        private void OnDestroy()
        {
            activeSpinSeq?.Kill();
            tapTween?.Kill();
            ringTween?.Kill();

            StopIdleAmbience();
            StopPointerSpinAnim();
            cd.Dispose();
        }
    }
}
