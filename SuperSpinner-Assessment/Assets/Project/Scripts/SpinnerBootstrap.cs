using System;
using UniRx;
using UnityEngine;
using SuperSpinner.Networking;
using SuperSpinner.UI;

namespace SuperSpinner.Core
{
    public sealed class SpinnerBootstrap : MonoBehaviour
    {
        [SerializeField] private SpinnerUiRefs ui;
        [SerializeField] private SpinnerView view;
        [SerializeField] private SpinnerFlow flow;
        [SerializeField] private SpinnerErrorUi errorUi;

        [Header("Networking")]
        [SerializeField] private float valuesTimeoutSeconds = 8f;
        [SerializeField] private int valuesRetries = 1;

        private SpinnerApiService api;
        private readonly CompositeDisposable cd = new CompositeDisposable();

        private void Awake()
        {
            api = new SpinnerApiService();

            ui?.ShowSpinner(false);
            ui?.ShowLoading(true);

            errorUi?.HideInstant();
        }

        private void Start()
        {
            LoadValues();
        }

        private void LoadValues()
        {
            errorUi?.HideInstant();
            ui?.ShowSpinner(false);
            ui?.ShowLoading(true);

            api.GetValues()
                .Timeout(TimeSpan.FromSeconds(valuesTimeoutSeconds))
                .Retry(valuesRetries) // 1 retry
                .ObserveOnMainThread()
                .Subscribe(
                    res =>
                    {
                        view.BuildReel(res.spinnerValues);
                        flow.ResetTravelToCurrent();

                        ui?.ShowLoading(false);
                        ui?.ShowSpinner(true);

                        flow.EnableTap();
                    },
                    err =>
                    {
                        ui?.ShowLoading(false);

                        // error
                        errorUi?.Show("Network error. Please try again.");

                        // Εδώ επιλέγεις:
                        // A) auto retry σε 1.5s
                        Observable.Timer(TimeSpan.FromSeconds(1.5f))
                            .ObserveOnMainThread()
                            .Subscribe(_ => LoadValues())
                            .AddTo(cd);
                    }
                )
                .AddTo(cd);
        }

        private void OnDestroy()
        {
            cd.Dispose();
        }
    }
}
