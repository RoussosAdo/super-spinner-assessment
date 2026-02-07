using System;
using System.Collections.Generic;

namespace SuperSpinner.Networking
{
    [Serializable]
    public sealed class SpinnerValuesResponse
    {
        // JSON: { "spinnerValues": [1000, 2000, ...] }
        public List<int> spinnerValues;
    }

    [Serializable]
    public sealed class SpinnerSpinResponse
    {
        // JSON: { "spinnerValue": 150000 }
        public int spinnerValue;
    }
}
