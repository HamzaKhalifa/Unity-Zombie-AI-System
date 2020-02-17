using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class AISoundEmitter : MonoBehaviour
{
    // Inspector variables
    [SerializeField] float _decayRate = 1f;

    // Private variables
    SphereCollider _collider = null;
    float _srcRadius = 0f;
    float _targetRadius = 0f;
    float _interpolator = 0f;
    float _interpolatorSpeed = 0f;

    // Start is called before the first frame update
    void Awake()
    {
        _collider = GetComponent<SphereCollider>();
        if (_collider == null) return;

        _srcRadius = _targetRadius = _collider.radius;

        if (_decayRate > 0.02f) {
            _interpolatorSpeed = 1f / _decayRate;
        } else {
            _interpolatorSpeed = 0f;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (_collider == null) return;

        _interpolator = Mathf.Clamp01(_interpolator + Time.deltaTime * _interpolatorSpeed);
        _collider.radius = Mathf.Lerp(_srcRadius, _targetRadius, _interpolator);

        if (_collider.radius < Mathf.Epsilon) _collider.enabled = false;
        else _collider.enabled = true;
    }

    public void SetRadius(float newRadius, bool instantResize = false) {
        if (_collider == null || newRadius.Equals(_targetRadius)) return;

        _srcRadius = (instantResize || newRadius > _collider.radius) ? newRadius : _collider.radius;
        _targetRadius = newRadius;
        _interpolator = 0f;
    }
}
