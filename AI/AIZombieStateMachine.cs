using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieStateMachine : AIStateMachine
{
    [SerializeField] [Range(10.0f, 360.0f)] float _fov = 50.0f;
    [SerializeField] [Range(0f, 1.0f)] float _sight = 0.5f;
    [SerializeField] [Range(0f, 1.0f)] float _hearing = 1.0f;
    [SerializeField] [Range(0f, 1.0f)] float _aggression = 0.5f;
    [SerializeField] [Range(0.0f, 100.0f)] int _health = 100;
    [SerializeField] [Range(0f, 1.0f)] float _intelligence = 0.5f;
    [SerializeField] [Range(0f, 1.0f)] float _satisfaction = 1.0f;
    [SerializeField] float _replenishRate = .5f;
    [SerializeField] float _depletionRate = .1f;

    int _seeking = 0;
    bool _feeding = false;
    bool _crawling = false;
    int _attackType = 0;
    float _speed = 0;

    // Hashes
    int _speedHash = Animator.StringToHash("Vertical");
    int _feedingHash = Animator.StringToHash("Feeding");
    int _seekingHash = Animator.StringToHash("Seeking");
    int _attackHash = Animator.StringToHash("Attack");

    // The first ones only have getters because the child states are never gonnas set these directly.
    public float replenishRate { get { return _replenishRate; }}
    public float fov { get { return _fov; } }
    public float sight { get { return _sight; } }
    public float hearing { get { return _hearing; } }
    public bool crawling { get { return _crawling; } }
    public float intelligence { get { return _intelligence; } }
    public float satistfaction { get { return _satisfaction; } set { _satisfaction = value; } }
    public float aggression { get { return _aggression; } set { _aggression = value; } }
    public int health { get { return health; } set { _health = value; } }
    public int attackType { get { return _attackType; } set { _attackType = value; } }
    public bool feeding { get { return _feeding; } set { _feeding = value; } }
    public int seeking { get { return _seeking; } set { _seeking = value; } }
    public float speed {
        get {
            return _speed;
        }
        set {
            _speed = value;
        }
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        if (_animator != null) {
            _animator.SetFloat(_speedHash, _speed);
            _animator.SetBool(_feedingHash, _feeding);
            _animator.SetInteger(_seekingHash, _seeking);
            _animator.SetInteger(_attackHash, _attackType);
        }
    }
}
