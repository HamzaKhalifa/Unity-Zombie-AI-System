using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveSound : InteractiveItem
{
    [TextArea(3, 10)] [SerializeField] string _infoText = null;
    [TextArea(3, 10)] [SerializeField] string _activatedText = null;
    [SerializeField] float _activatedTextDuration = 3.0f;
    [SerializeField] AudioCollection _audioCollection = null;
    [SerializeField] int _bank = 0;

    IEnumerator _coroutine = null;
    float _hideActivatedTextTime = 0f;

    public override string GetText() {
        if (_coroutine != null || Time.time < _hideActivatedTextTime) {
            return _activatedText;
        } else {
            return _infoText;
        }
    }

    public override void Activate(CharacterManager characterManager)
    {
        base.Activate(characterManager);

        if (_coroutine == null) {
            _hideActivatedTextTime = Time.time + _activatedTextDuration;
            _coroutine = DoActivation();
            StartCoroutine(_coroutine);
        } else {

        }
    }

    IEnumerator DoActivation() {
        if (_audioCollection == null || AudioManager.instance == null) yield break;

        AudioClip clip = _audioCollection[_bank];
        if (clip == null) yield break;

        AudioManager.instance.PlayOneShotSound(_audioCollection.audioGroup, clip, transform.position, _audioCollection.volume, _audioCollection.spatialBlend, _audioCollection.priority);
        yield return new WaitForSeconds(clip.length);

        _coroutine = null;
    }
}
