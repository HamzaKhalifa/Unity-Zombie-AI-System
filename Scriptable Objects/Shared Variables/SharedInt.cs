using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Shared Variables/New Shared Int")]
public class SharedInt : ScriptableObject
{
    [SerializeField] int _value = 0;

    int _runtimeValue = 0;

    public int value
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
