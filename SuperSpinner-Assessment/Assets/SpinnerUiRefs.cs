using UnityEngine;

namespace SuperSpinner.UI
{
    public sealed class SpinnerUiRefs : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject loadingPanel;
        public GameObject spinnerPanel;

        public void ShowLoading(bool show)
        {
            if (loadingPanel) loadingPanel.SetActive(show);
        }

        public void ShowSpinner(bool show)
        {
            if (spinnerPanel) spinnerPanel.SetActive(show);
        }
    }
}
