using UnityEngine;
using UnityEngine.EventSystems;
using SuperSpinner.Core;

namespace SuperSpinner.UI
{
    public sealed class TapOverlayClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private SpinnerFlow flow;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (flow != null)
                flow.OnTap();
        }
    }
}
