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

    // Force UI state κάθε φορά που ξεκινά request
    ui?.ShowSpinner(false);
    ui?.ShowLoading(true);

    api.GetValues()
        .Timeout(TimeSpan.FromSeconds(valuesTimeoutSeconds))
        .Retry(valuesRetries)
        .ObserveOnMainThread()
        .Subscribe(
            res =>
            {
                // Build UI content
                view.BuildReel(res.spinnerValues);
                flow.ResetTravelToCurrent();

                // Force UI state 
                errorUi?.HideInstant();
                ui?.ShowLoading(false);
                ui?.ShowSpinner(true);

                flow.EnableTap();
            },
            err =>
            {
                // Force UI state σε failure
                ui?.ShowLoading(false);
                ui?.ShowSpinner(false);

                errorUi?.Show("Network error. Retrying...");

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
