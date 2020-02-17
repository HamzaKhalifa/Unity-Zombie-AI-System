using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimatorParameterType { Trigger, Bool, Int, Float, String }

[System.Serializable]
public class AnimatorParameter {
    public AnimatorParameterType Type = AnimatorParameterType.Bool;
    public string Name = null;
    public string Value = null;
}

[System.Serializable]
public class AnimatorConfigurator {
    [SerializeField] public Animator Animator = null;
    [SerializeField] public List<AnimatorParameter> AnimatorParameters = new List<AnimatorParameter>();
}

public class InteractiveGenericSwitch : InteractiveItem
{
    [Header("Game State Management")]
    [SerializeField] protected List<GameState> _requiredStates = new List<GameState>();
    [SerializeField] protected List<GameState> _activateStates = new List<GameState>();
    [SerializeField] protected List<GameState> _deactivateStates = new List<GameState>();

    [Header("Message")]
    [TextArea(3, 10)] [SerializeField] string _stateNotSetText = "";
    [TextArea(3, 10)] [SerializeField] string _stateSetText = "";
    [TextArea(3, 10)] [SerializeField] string _objectActiveText = "";

    [Header("Activation Parameters")]
    [SerializeField] protected float _activationDelay = 1f;
    [SerializeField] protected float _deativationDelay = 1f;
    [SerializeField] AudioCollection _activationSounds = null;
    [SerializeField] AudioSource _audioSource = null;

    [Header("Operating mode")]
    [SerializeField] protected bool _startActivated = false;
    [SerializeField] protected bool _canToggle = false;

    [Header("Configurable Entities")]
    [SerializeField] List<AnimatorConfigurator> _animations = new List<AnimatorConfigurator>();
    [SerializeField] List<MaterialController> _materialControllers = new List<MaterialController>();
    [SerializeField] List<GameObject> _objectActivators = new List<GameObject>();
    [SerializeField] List<GameObject> _objectDeactivators = new List<GameObject>();

    protected IEnumerator _coroutine = null;
    protected bool _activated = false;
    protected bool _firstUse = false;

    protected override void Start()
    {
        base.Start();

        for (int i = 0; i < _materialControllers.Count; i++) {
            if (_materialControllers[i] != null) {
                _materialControllers[i].OnStart();
            }
        }

        for (int i = 0; i < _objectActivators.Count; i++)
        {
            if (_objectActivators[i] != null)
            {
                _objectActivators[i].SetActive(false);
            }
        }

        for (int i = 0; i < _objectDeactivators.Count; i++)
        {
            if (_objectDeactivators[i] != null)
            {
                _objectDeactivators[i].SetActive(true);
            }
        }

        if (_startActivated) {
            Activate(null);
            _firstUse = false;
        }
    }

    public override string GetText() {
        if (!enabled) return string.Empty;

        if (_activated) {
            return _objectActiveText;
        }

        bool requiredStates = AreRequiredStatesSet();

        if (!requiredStates) return _stateNotSetText;
        else return _stateSetText;
    }

    protected bool AreRequiredStatesSet() {
        ApplicationManager appManager = ApplicationManager.instance;
        if (appManager = null) return false;

        for (int i = 0; i < _requiredStates.Count; i++) {
            GameState state = _requiredStates[i];
            string result = appManager.GetGameState(state.key);
            if (string.IsNullOrEmpty(result) || !result.Equals(state.value)) return false;
        }

        return true;
    }

    protected void SetActivationStates() {
        ApplicationManager appManager = ApplicationManager.instance;
        if (appManager == null) return;

        if (_activated) {
            foreach(GameState state in _activateStates) {
                Debug.Log("Setting game state " + state.key + " To " + state.value);
                appManager.SetGameState(state.key, state.value);
            }
        } else {
            foreach (GameState state in _deactivateStates)
            {
                appManager.SetGameState(state.key, state.value);
            }
        }
    }

    public override void Activate(CharacterManager characterManager) {
        ApplicationManager appManager = ApplicationManager.instance;
        if (appManager = null) return;

        if (_firstUse && !_canToggle) return;

        if (!_activated) {
            bool requiredStates = AreRequiredStatesSet();
            if (!requiredStates) return;
        }

        _activated = !_activated;
        _firstUse = true;

        if (_activationSounds != null && _activated) {
            AudioClip clipToPlay = _activationSounds[0];
            if (clipToPlay == null) return;

            if (_audioSource != null) {
                _audioSource.clip = clipToPlay;
                _audioSource.volume = _activationSounds.volume;
                _audioSource.spatialBlend = _activationSounds.spatialBlend;
                _audioSource.priority = _activationSounds.priority;
                _audioSource.outputAudioMixerGroup = AudioManager.instance.GetAudioGroupFromATrackName(_activationSounds.audioGroup);
                _audioSource.Play();
            }
        }

        if (_coroutine != null) {
            StopCoroutine(_coroutine);
        }
        _coroutine = DoDelayedActivation();
        StartCoroutine(_coroutine);
    }

    IEnumerator DoDelayedActivation() {
        foreach(AnimatorConfigurator configurator in _animations) {
            if (configurator != null) {
                foreach (AnimatorParameter param in configurator.AnimatorParameters) {
                    switch(param.Type) {
                        case AnimatorParameterType.Bool:
                            bool boolean = bool.Parse(param.Value);
                            configurator.Animator.SetBool(param.Name, _activated ? boolean : !boolean);
                            break;
                        case AnimatorParameterType.Float: 
                            break;
                        case AnimatorParameterType.Int:
                            break;
                        case AnimatorParameterType.String:
                            break;
                        case AnimatorParameterType.Trigger:
                            break;
                    }
                }
            }
        }

        yield return new WaitForSeconds(_activated ? _activationDelay : _deativationDelay);

        SetActivationStates();

        if (_activationSounds != null && !_activated)
        {
            AudioClip clipToPlay = _activationSounds[1];

            if (_audioSource != null && clipToPlay)
            {
                _audioSource.clip = clipToPlay;
                _audioSource.volume = _activationSounds.volume;
                _audioSource.spatialBlend = _activationSounds.spatialBlend;
                _audioSource.priority = _activationSounds.priority;
                _audioSource.outputAudioMixerGroup = AudioManager.instance.GetAudioGroupFromATrackName(_activationSounds.audioGroup);
                _audioSource.Play();
            }
        }

        if (_objectActivators.Count > 0) {
            for (int i = 0; i < _objectActivators.Count; i++) {
                if (_objectActivators[i]) {
                    _objectActivators[i].SetActive(_activated);
                }
            }
        }

        if (_objectDeactivators.Count > 0)
        {
            for (int i = 0; i < _objectDeactivators.Count; i++)
            {
                if (_objectDeactivators[i])
                {
                    _objectDeactivators[i].SetActive(!_activated);
                }
            }
        }

        if (_materialControllers.Count > 0)
        {
            for (int i = 0; i < _materialControllers.Count; i++)
            {
                if (_materialControllers[i] != null)
                {
                    _materialControllers[i].Activate(_activated);
                }
            }
        }

    }

}
