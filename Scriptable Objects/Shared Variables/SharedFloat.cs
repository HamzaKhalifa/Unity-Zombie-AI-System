using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Shared Variables/New Shared Float")]
public class SharedFloat : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] float _value = 0f;

    float _runtimeValue = 0f;

    public float value {
        get { return _runtimeValue; }
        set { 
            _runtimeValue = value;
        }
    }

    public void OnAfterDeserialize()
    {
        _runtimeValue = _value;
    }

    public void OnBeforeSerialize()
    {
        return;
    }
}
