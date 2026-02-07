using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SuperSpinner.UI
{
    public sealed class SpinnerView : MonoBehaviour
    {
        [Header("Reel")]
        [SerializeField] private RectTransform reelContent;
        [SerializeField] private RectTransform reelMask;
        [SerializeField] private TMP_Text prizePrefab;

        [Header("Layout")]
        [SerializeField] private float itemSpacing = 120f;
        [SerializeField] private int idleStartIndex = 1; // 0=1000, 1=2000

        private readonly List<TMP_Text> items = new();
        private readonly List<int> builtValues = new();

        public RectTransform ReelContent => reelContent;
        public RectTransform ReelMask => reelMask;

        public float ItemSpacing => itemSpacing;
        public int UniqueCount { get; private set; }
        public float LoopHeight => UniqueCount * itemSpacing;

        public void BuildReel(IReadOnlyList<int> values)
        {
            Clear();

            UniqueCount = values.Count;

            // 3 copies
            var extended = new List<int>(values.Count * 3);
            extended.AddRange(values);
            extended.AddRange(values);
            extended.AddRange(values);

            for (int i = 0; i < extended.Count; i++)
            {
                int v = extended[i];
                builtValues.Add(v);

                var txt = Instantiate(prizePrefab, reelContent);
                txt.text = v.ToString("N0");
                txt.rectTransform.anchoredPosition = new Vector2(0, -i * itemSpacing);
                items.Add(txt);
            }

            SetIdlePosition();
        }

        public void SetIdlePosition()
        {
            if (UniqueCount <= 0) return;

            float y = idleStartIndex * itemSpacing;
            y = Mod(y, LoopHeight);

            reelContent.anchoredPosition = new Vector2(0f, y);
        }

        // target y πάντα μέσα σε [0, LoopHeight)
        public float GetTargetModYForValue(int value)
        {
            if (UniqueCount <= 0) return 0f;

            // Πρώτο block values (0..UniqueCount-1)
            for (int i = 0; i < UniqueCount; i++)
            {
                if (builtValues[i] == value)
                {
                    float y = i * itemSpacing;
                    return Mod(y, LoopHeight);
                }
            }

            return 0f;
        }

        public static float Mod(float a, float m)
        {
            if (m <= 0.0001f) return 0f;
            float r = a % m;
            return r < 0 ? r + m : r;
        }

        private void Clear()
        {
            foreach (var t in items)
            {
                if (t) Destroy(t.gameObject);
            }

            items.Clear();
            builtValues.Clear();
            UniqueCount = 0;
        }
    }
}
