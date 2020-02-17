using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Shared Variables/New Shared Bool")]
public class SharedBool : ScriptableObject
{
    [SerializeField] bool _value = false;

    bool _runtimeValue = false;

    public bool value
    {
        get { return _runtimeValue; }
        set { _runtimeValue = value; }
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
