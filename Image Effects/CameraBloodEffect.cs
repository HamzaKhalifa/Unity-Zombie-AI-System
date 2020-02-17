using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraBloodEffect : MonoBehaviour
{
    // Inspector assigned
    [SerializeField] Texture2D _bloodTexture = null;

    [SerializeField] Texture2D _bloodNormalMap = null;

    [SerializeField] float _bloodAmount = 0;

    [SerializeField] float _minBloodAmount = 0;

    [SerializeField] float _distortion = 1;

    [SerializeField] bool _autoFade = true;

    [SerializeField] float _fadeSpeed = 0.05f;

    [SerializeField] Shader _shader = null;


    // Private 
    Material _material = null;

    // Properties
    public float bloodAmount { get { return _bloodAmount; } set { _bloodAmount = value; } }
    public float minBloodAmount { get { return minBloodAmount; } set { _minBloodAmount = value; } }
    public float fadeSpeed { get { return _fadeSpeed; } set { _fadeSpeed = value; } }
    public bool autoFade { get { return _autoFade; } set { _autoFade = value; } }

    void Update()
    {
        if (_autoFade) {
            _bloodAmount -= _fadeSpeed * Time.deltaTime;
            _bloodAmount = Mathf.Max(_bloodAmount, _minBloodAmount);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_shader == null) return;

        if (_material == null) {
            _material = new Material(_shader);
        }
        if (_material == null) return;

        // Send data into shader
        if (_bloodTexture != null)
            _material.SetTexture("_BloodTex", _bloodTexture);
        if (_bloodNormalMap != null)
            _material.SetTexture("_BloodBump", _bloodNormalMap);

        _material.SetFloat("_Distortion", _distortion);
        _material.SetFloat("_BloodAmount", _bloodAmount);

        // Perform image effect
        Graphics.Blit(source, destination, _material);
    }
}
