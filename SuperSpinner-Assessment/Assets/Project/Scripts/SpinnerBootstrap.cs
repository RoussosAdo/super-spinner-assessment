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

        private SpinnerApiService api;
        private readonly CompositeDisposable cd = new CompositeDisposable();

        private void Awake()
        {
            api = new SpinnerApiService();

            if (ui == null) Debug.LogError("SpinnerBootstrap: ui is not assigned.");
            if (view == null) Debug.LogError("SpinnerBootstrap: view is not assigned.");
            if (flow == null) Debug.LogError("SpinnerBootstrap: flow is not assigned.");

            ui?.ShowSpinner(false);
            ui?.ShowLoading(true);
        }

        private void Start()
        {
            api.GetValues()
                .ObserveOnMainThread()
                .Subscribe(
                    res =>
                    {
                        Debug.Log($"Spinner values received: {string.Join(", ", res.spinnerValues)}");

                        // 1) Build reel
                        view.BuildReel(res.spinnerValues);

                        // 2) Sync travel with idle position
                        flow.ResetTravelToCurrent();

                        // 3) Show UI
                        ui?.ShowLoading(false);
                        ui?.ShowSpinner(true);

                        // 4) Enable tap
                        flow?.EnableTap();
                    },
                    err => Debug.LogError(err)
                )
                .AddTo(cd);
        }

        private void OnDestroy()
        {
            cd.Dispose();
        }
    }
}
