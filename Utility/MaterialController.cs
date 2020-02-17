using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MaterialController
{
    [SerializeField] protected Material _material = null;
    [SerializeField] protected Texture _diffuseTexture = null;
    [SerializeField] protected Color _diffuseColor = Color.white;
    [SerializeField] protected Texture _normalMap = null;
    [SerializeField] protected float _normalStregth = 0f;

    [SerializeField] protected Texture _emissiveTexture = null;
    [SerializeField] protected Color _emissionColor = Color.black;
    [SerializeField] protected float _emissionScale = 1f;

    protected MaterialController _backup = null;
    protected bool _started = false;

    public Material material { get { return _material; } }

    public void OnStart() {
        if (_material == null || _started) return;

        _started = true;

        _backup = new MaterialController();
        _backup._diffuseColor = _material.GetColor("_Color");
        _backup._diffuseTexture = _material.GetTexture("_MainText");
        _backup._emissionColor = _material.GetColor("_EmissionColor");
        _backup._emissionScale = 1;
        _backup._emissiveTexture = _material.GetTexture("_EmissionMap");
        _backup._normalMap = _material.GetTexture("_BumpMap");
        _backup._normalStregth = _material.GetFloat("_BumpScale");

        if (GameSceneManager.instance) GameSceneManager.instance.RegisterMaterialController(_material.GetInstanceID(), this);
    }

    public void Activate(bool activate) {
        if (_material == null || !_started) return;

        if (activate) {
            _material.SetColor("_Color", _diffuseColor);
            material.SetTexture("_MainText", _diffuseTexture);
            _material.SetColor("_EmissionColor", _emissionColor);
            _material.SetTexture("_EmissionMap", _emissiveTexture);
            _material.SetTexture("_BumpMap", _normalMap);
            _material.SetFloat("_BumpScale", _normalStregth);
        } else {
            _material.SetColor("_Color", _backup._diffuseColor);
            material.SetTexture("_MainText", _backup._diffuseTexture);
            _material.SetColor("_EmissionColor", _backup._emissionColor);
            _material.SetTexture("_EmissionMap", _backup._emissiveTexture);
            _material.SetTexture("_BumpMap", _backup._normalMap);
            _material.SetFloat("_BumpScale", _backup._normalStregth);
        }
    }

    public void OnReset() {
        if (_material == null || !_started) return;

        _material.SetColor("_Color", _backup._diffuseColor);
        material.SetTexture("_MainText", _backup._diffuseTexture);
        _material.SetColor("_EmissionColor", _backup._emissionColor);
        _material.SetTexture("_EmissionMap", _backup._emissiveTexture);
        _material.SetTexture("_BumpMap", _backup._normalMap);
        _material.SetFloat("_BumpScale", _backup._normalStregth);
    }

    public int GetInstanceID() {
        if (_material == null) return -1;

        return _material.GetInstanceID();
    }
}
