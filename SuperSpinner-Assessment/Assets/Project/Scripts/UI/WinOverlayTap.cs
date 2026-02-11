using UnityEngine;
using UnityEngine.EventSystems;
using SuperSpinner.Core;

public class WinOverlayTap : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SpinnerFlow flow;

    public void OnPointerClick(PointerEventData eventData)
    {
        flow?.ForceRestartFlow();
    }
}
