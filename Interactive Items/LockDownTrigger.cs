using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LockDownTrigger : MonoBehaviour
{
    [SerializeField] protected float _downloadTime = 10f;
    [SerializeField] protected Slider _downloadBar = null;
    [SerializeField] protected Text _hintText = null;
    [SerializeField] protected MaterialController _materialController = null;
    [SerializeField] protected GameObject _lockedLight = null;
    [SerializeField] protected GameObject _unlockedLight = null;

    ApplicationManager _applicationManager = null;
    GameSceneManager _gameScenceManager = null;
    bool _inTrigger = false;
    float _downloadProgress = 0f;
    AudioSource _audioSource = null;
    bool _downloadComplete = false;

    void OnEnable()
    {
        _applicationManager = ApplicationManager.instance;
        _audioSource = GetComponent<AudioSource>();

        _downloadProgress = 0f;

        if (_materialController != null)
            _materialController.OnStart();

        if (_applicationManager != null) {
            string lockedDown = _applicationManager.GetGameState("LOCKDOWN");

            if (string.IsNullOrEmpty(lockedDown) || lockedDown.Equals("TRUE")) {
                if (_materialController != null) _materialController.Activate(false);
                if (_unlockedLight != null) _unlockedLight.SetActive(false);
                if (_lockedLight != null) _lockedLight.SetActive(true);
                _downloadComplete = false;
            } else if (lockedDown.Equals("FALSE")) {
                if (_materialController != null) _materialController.Activate(true);
                if (_unlockedLight != null) _unlockedLight.SetActive(true);
                if (_lockedLight != null) _lockedLight.SetActive(false);
                _downloadComplete = true;
            }
        }

        ResetSoundAndUi();
    }

    void Update()
    {
        if (_downloadComplete) return;

        if (_inTrigger) {
            if (Input.GetButton("Use")) {
                if (_audioSource && !_audioSource.isPlaying) {
                    _audioSource.Play();
                }

                _downloadProgress = Mathf.Clamp(_downloadProgress += Time.deltaTime, 0f, _downloadTime);

                if (!_downloadProgress.Equals(_downloadTime)) {
                    if (_downloadBar) {
                        _downloadBar.gameObject.SetActive(true);
                        _downloadBar.value = _downloadProgress / _downloadTime;
                    }
                    return;
                } else {
                    _downloadComplete = true;
                    ResetSoundAndUi();

                    if (_hintText) _hintText.text = "Successful Deactivation";

                    _applicationManager.SetGameState("LOCKDOWN", "FALSE");

                    if (_materialController != null) _materialController.Activate(true);
                    if (_unlockedLight) _unlockedLight.gameObject.SetActive(true);
                    if (_lockedLight) _lockedLight.gameObject.SetActive(false);

                    return;
                }
            }
        }

        _downloadProgress = 0f;
        ResetSoundAndUi();
    }

    void ResetSoundAndUi() {
        if (_audioSource && _audioSource.isPlaying) _audioSource.Stop();
        if (_downloadBar) {
            _downloadBar.value = _downloadProgress;
            _downloadBar.gameObject.SetActive(false);
        }

        if (_hintText) _hintText.text = "Hold 'Use' Button To Deactivate";
    }

    void OnTriggerEnter(Collider other)
    {
        if (_inTrigger || _downloadComplete) return;
        if (other.CompareTag("Player")) _inTrigger = true;

    }

    void OnTriggerExit(Collider other)
    {
        if (_downloadComplete) return;
        if (other.CompareTag("Player")) _inTrigger = false;
    }
}
