using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Scripatable Objects/Custom Curve")]
public class CustomCurve : ScriptableObject
{
    [SerializeField] AnimationCurve _curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0f, 0f));

    public float Evaluate(float t) {
        return _curve.Evaluate(t);
    }
}
