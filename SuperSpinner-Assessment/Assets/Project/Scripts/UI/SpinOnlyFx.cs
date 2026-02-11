using UnityEngine;
using UnityEngine.UI;

public class SpinOnlyFx : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private CanvasGroup group;

    [Header("Rotation")]
    [SerializeField] private float zDegreesPerSecond = 180f; // πόσο γρήγορα γυρνάει
    [SerializeField] private bool rotateWhenActive = true;

    [Header("Optional: reset rotation on stop")]
    [SerializeField] private bool resetRotationOnStop = true;

    private bool active;

    private void Awake()
    {
        if (!rect) rect = GetComponent<RectTransform>();
        if (!group) group = GetComponent<CanvasGroup>();
        StopFx();
    }

    private void Update()
    {
        if (!active || !rotateWhenActive) return;

        
        rect.Rotate(0f, 0f, -zDegreesPerSecond * Time.deltaTime);
    }

    public void StartFx()
    {
        active = true;
        if (group) group.alpha = 1f;
    }

    public void StopFx()
    {
        active = false;
        if (group) group.alpha = 0f;

        if (resetRotationOnStop && rect)
            rect.localRotation = Quaternion.identity;
    }
}
