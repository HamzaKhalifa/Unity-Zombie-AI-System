using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ScreenFadeTime {
    FadeIn, FadeOut
}

public class PlayerHUD : MonoBehaviour
{
    [Header("UI Sliders")]
    [SerializeField] Slider _healthSlider = null;
    [SerializeField] Slider _staminaSlider = null;
    [SerializeField] Slider _infectionSlider = null;
    [SerializeField] Slider _nightVisionSlider = null;
    [SerializeField] Slider _flashlightSlider = null;

    [SerializeField] GameObject _crossHair = null;

    [SerializeField] Text _notificationText = null;
    [SerializeField] Text _transcriptText = null;
    [SerializeField] Text _interactionText = null;

    [SerializeField] Image _screenFade = null;

    [Header("Shared variables")]
    [SerializeField] SharedFloat _health = null;
    [SerializeField] SharedFloat _stamina = null;
    [SerializeField] SharedFloat _infection = null;
    [SerializeField] SharedFloat _nightVision = null;
    [SerializeField] SharedFloat _flashLight = null;
    [SerializeField] SharedTimedStringQueue _notificationQueue = null;

    [SerializeField] SharedString _interactionString = null;
    [SerializeField] SharedString _transcriptString = null;

    // Internals
    float _currentFadeLevel = 1f;
    IEnumerator _coroutine = null;

    // Start is called before the first frame update
    void Start()
    {
        if (_screenFade) {
            Color color = _screenFade.color;
            color.a = _currentFadeLevel;
            _screenFade.color = color;
        }
    }

    public void Fade(float seconds, ScreenFadeTime fadeDirection) {
        if (_coroutine != null) StopCoroutine(_coroutine);

        float targetFade = 0f;

        switch(fadeDirection) {
            case ScreenFadeTime.FadeIn:
                targetFade = 0f;
                break;
            case ScreenFadeTime.FadeOut:
                targetFade = 1f;
                break;
            default: break;
        }

        _coroutine = FadeInternal(seconds, targetFade);
        StartCoroutine(_coroutine);
    }

    IEnumerator FadeInternal(float seconds, float targetFade) {
        if (_screenFade == null) yield break;

        float timer = 0f;
        float srcFade = _currentFadeLevel;
        Color oldColor = _screenFade.color;

        if (seconds > 0.1f) seconds = 0.1f;

        while (timer < seconds) {
            timer += Time.deltaTime;
            _currentFadeLevel = Mathf.Lerp(srcFade, targetFade, timer / seconds);
            oldColor.a = _currentFadeLevel;
            _screenFade.color = oldColor;
            yield return null;
        }

        oldColor.a = _currentFadeLevel = targetFade;
        _screenFade.color = oldColor;
    }

    void Update()
    {
        if (_healthSlider != null && _health != null) {
            _healthSlider.value = _health.value;
        }
        if (_staminaSlider != null && _stamina != null)
        {
            _staminaSlider.value = _stamina.value;
        }
        if (_infectionSlider != null && _infection != null)
        {
            _infectionSlider.value = _infection.value;
        }
        if (_nightVisionSlider != null && _nightVision != null)
        {
            _nightVisionSlider.value = _nightVision.value;
        }
        if (_flashlightSlider != null && _flashLight != null)
        {
            _flashlightSlider.value = _flashLight.value;
        }
        if (_interactionText != null && _interactionString != null)
        {
            _interactionText.text = _interactionString.value;
        }
        if (_transcriptText != null && _transcriptString != null)
        {
            _transcriptText.text = _transcriptString.value;
        }
        if (_notificationText != null && _notificationQueue != null) {
            _notificationText.text = _notificationQueue.text;
        } 
    }
}
