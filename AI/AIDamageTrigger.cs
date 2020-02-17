using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDamageTrigger : MonoBehaviour
{
    [SerializeField] string _parameter = "RightHand";
    [SerializeField] int _bloodParticleBurstAmount = 10;
    [SerializeField] float _damageAmount = 50f;
    [SerializeField] bool _doDamageSound = true;
    [SerializeField] bool _doPainSound = true;

    // Private
    AIStateMachine _stateMachine = null;
    Animator _animator = null;
    int _parameterHash = -1;
    GameSceneManager _gameSceneManager = null;
    bool _firstContact = false;
    float _resetFirstContactTimer = 0f;
    float _resetFirstContactDelay = 3f;

    void Start()
    {
        _stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();
        if (_stateMachine != null) _animator = _stateMachine.animator;

        _parameterHash = Animator.StringToHash(_parameter);
        _gameSceneManager = GameSceneManager.instance;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_animator)
            return;

        if (other.gameObject.CompareTag("Player") && _animator.GetFloat(_parameterHash) > .9f)
        {
            _firstContact = true;
        }
    }

    void Update()
    {
        if (!_firstContact) {
            _resetFirstContactTimer += Time.deltaTime;
            if (_resetFirstContactTimer >= _resetFirstContactDelay)
            {
                _resetFirstContactTimer = 0f;
                _firstContact = true;
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!_animator) return;

        if (other.CompareTag("Player") && _animator.GetFloat(_parameter) > .9f) {
            if (_gameSceneManager && GameSceneManager.instance.bloodParticles) {
                ParticleSystem system = GameSceneManager.instance.bloodParticles;
                system.transform.position = transform.position;
                // To make sure the blood goes forward
                system.transform.rotation = Camera.main.transform.rotation;
                system.simulationSpace = ParticleSystemSimulationSpace.World;
                system.Emit(_bloodParticleBurstAmount);
            }

            if (_gameSceneManager != null) {
                PlayerInfo playerInfo = _gameSceneManager.GetPlayerInfo(other.GetInstanceID());
                CharacterManager characterManager = other.GetComponent<CharacterManager>();
                characterManager.TakeDamage(_damageAmount, _doDamageSound && _firstContact, _doPainSound);
            }

            _firstContact = false;
            _resetFirstContactTimer = 0f;
        }
    }
}
