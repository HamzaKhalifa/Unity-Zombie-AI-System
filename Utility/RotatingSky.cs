using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingSky : MonoBehaviour
{
    [SerializeField] protected Material _skyMaterial = null;
    [SerializeField] protected float _speed = 1f;

    protected float _angle = 0f;
    protected float _originalAngle = 0f;

    void OnEnable()
    {
        if (_skyMaterial) _originalAngle = _skyMaterial.GetFloat("_Rotation");
    }

    void OnDisable()
    {
        if (_skyMaterial) _skyMaterial.SetFloat("_Rotation", _originalAngle);
    }

    void Update()
    {
        if (_skyMaterial == null) return;

        _angle += _speed * Time.deltaTime;
        if (_angle > 360)
            _angle -= 360f;
        else if (_angle < 0)
            _angle += 360;

        _skyMaterial.SetFloat("_Rotation", _angle);
    }

}
