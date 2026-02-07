using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SuperSpinner.UI
{
    public sealed class SpinnerView : MonoBehaviour
    {
        [Header("Reel")]
        [SerializeField] private RectTransform reelContent;
        [SerializeField] private TMP_Text prizePrefab;

        [Header("Layout")]
        [SerializeField] private float itemSpacing = 120f;

        private readonly List<TMP_Text> items = new();

        public void BuildReel(IReadOnlyList<int> values)
        {
            Clear();

            //  3 copies για άπειρο scrolling feeling
            var extended = new List<int>();
            extended.AddRange(values);
            extended.AddRange(values);
            extended.AddRange(values);

            for (int i = 0; i < extended.Count; i++)
            {
                var txt = Instantiate(prizePrefab, reelContent);
                txt.text = extended[i].ToString("N0");
                txt.rectTransform.anchoredPosition = new Vector2(0, -i * itemSpacing);
                items.Add(txt);
            }

            // Reset θέση
            reelContent.anchoredPosition = Vector2.zero;
        }

        public float GetTargetPositionForValue(int value)
        {
            // Βρίσκουμε το index 
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].text.Replace(",", "") == value.ToString())
                {
                    return i * itemSpacing;
                }
            }

            return 0;
        }

        private void Clear()
        {
            foreach (var t in items)
                if (t) Destroy(t.gameObject);

            items.Clear();
        }
    }
}
