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

        private SpinnerApiService api;
        private readonly CompositeDisposable cd = new CompositeDisposable();

        private void Awake()
        {
            api = new SpinnerApiService();

            ui.ShowSpinner(false);
            ui.ShowLoading(true);
        }

        private void Start()
    {
        api.GetValues()
            .ObserveOnMainThread()
            .Subscribe(
                res =>
                {
                    Debug.Log($"Spinner values received: {string.Join(", ", res.spinnerValues)}");

                    view.BuildReel(res.spinnerValues);

                    ui.ShowLoading(false);
                    ui.ShowSpinner(true);
                },
                err =>
                {
                    Debug.LogError(err);
                    // TODO: Retry UI later
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
