using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Shared Variables/New Shared String")]
public class SharedString : ScriptableObject
{
    [SerializeField] string _value = "";

    string _runtimeValue = "";

    public string value
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
