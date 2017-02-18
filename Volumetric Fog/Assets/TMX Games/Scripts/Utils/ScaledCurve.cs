using UnityEngine;
using System.Collections;

namespace TMX.Utils
{
    [System.Serializable]
    public struct ScaledCurve
    {
        public AnimationCurve curve;
        public Vector2 inputRange;
        public Vector2 outputRange;

        public float GetValue (float input)
        {
            return MathUtils.Remap(curve.Evaluate(MathUtils.PercentBetween(input, inputRange)), outputRange);
        }

        public ScaledCurve Clone ()
        {
            var clone = new ScaledCurve();

            clone.curve = new AnimationCurve(curve.keys);
            clone.inputRange = inputRange;
            clone.outputRange = outputRange;

            return clone;
        }
    }
}