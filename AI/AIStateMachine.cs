using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIStateType { None, Idle, Alerted, Patrol, Attack, Feeding, Pursuit, Dead }
public enum AITargetType { None, Waypoint, Visual_Player, Visual_Light, Visual_Food, Audio }
public enum AITriggerEventType { Enter, Stay, Exit }

public struct AITarget
{
    AITargetType _type { get; set; }
    Collider _collider { get; set; }
    Vector3 _position { get; set; }
    float _distance { get; set; }
    float _time { get; set; }

    public Vector3 position { get { return _position; } }
    public AITargetType type { get { return _type; } }
    public Collider collider { get { return _collider; } }
    public float distance { get { return _distance; } set{ _distance = value; } }

    public void Set(AITargetType t, Collider c, Vector3 p, float d) {
        _type = t;
        _collider = c;
        _position = p;
        _distance = d;
        _time = Time.time;
    }

    public void Clear() {
        _type = AITargetType.None;
        _collider = null;
        _position = Vector3.zero;
        _time = 0f;
        _distance = Mathf.Infinity;
    }
}

public abstract class AIStateMachine : MonoBehaviour
{
    public AITarget VisualThreat = new AITarget();
    public AITarget AudioThreat = new AITarget();

    // Protected
    protected AIState _currentState = null;
    protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>();
    protected AITarget _target = new AITarget();
    // When requesting to use root position or root rotation by the state machines
    protected int _rootPositionRefCount = 0;
    protected int _rootRotationRefCount = 0;
    protected bool _isTargetReached = false;

    [SerializeField] protected AIStateType _currentStateType = AIStateType.Idle;
    [SerializeField] protected SphereCollider _targetTrigger = null;
    [SerializeField] protected SphereCollider _sensorTrigger = null;
    [SerializeField] protected AIWaypointNetwork _waypointNetwork = null;
    [SerializeField] protected bool _randomPatrol = false;
    [SerializeField] protected int _currentWaypoint = -1;

    [SerializeField]
    [Range(0, 15)] 
    protected float _stoppingDistance = 1.0f;

    // Cache
    protected Animator _animator;
    protected NavMeshAgent _navAgent;
    protected Collider _collider;
    protected Transform _transform;

    // Public properties
    public bool isTargetReached { get { return _isTargetReached; }}
    public bool inMeleeRange { get; set; }
    public Animator animator { get { return _animator; } }
    public NavMeshAgent navAgent { get { return _navAgent; } }
    public Vector3 sensorPosition {
        get {
            if (_sensorTrigger == null) return Vector3.zero;
            Vector3 point = _sensorTrigger.transform.position;
            point.x += _sensorTrigger.center.x * _sensorTrigger.transform.lossyScale.x;
            point.y += _sensorTrigger.center.y * _sensorTrigger.transform.lossyScale.y;
            point.z += _sensorTrigger.center.z * _sensorTrigger.transform.lossyScale.z;
            return point;
        }
    }

    public float sensorRadius {
        get {
            if (_sensorTrigger == null) return 0.0f;
            float radius = Mathf.Max(_sensorTrigger.radius * _sensorTrigger.transform.lossyScale.x, 
                                     _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.y);

            return Mathf.Max(radius, _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.z);
        }
    }

    public bool useRootPosition { get { return _rootPositionRefCount > 0; } }
    public bool useRootRotation { get { return _rootRotationRefCount > 0; } }
    public AITargetType targetType { get { return _target.type; } }
    public Vector3 targetPosition
    {
        get
        {
            return _target.position;
        }
    }
    public int targetColliderID {
        get {
            if (_target.collider != null) {
                return _target.collider.GetInstanceID();
            } else {
                return -1;
            }
        }
    }

    protected virtual void Awake() {
        _transform = transform;
        _animator = GetComponent<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();

        if (GameSceneManager.instance != null) {
            if (_collider) GameSceneManager.instance.RegisterAIStateMachine(_collider.GetInstanceID(), this);
            if (_sensorTrigger) GameSceneManager.instance.RegisterAIStateMachine(_sensorTrigger.GetInstanceID(), this);
        }

    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        if (_sensorTrigger != null) {
            AISensor script = _sensorTrigger.GetComponent<AISensor>();
            if (script) {
                script.parentStateMachine = this;
            }
        }

        // Fetch all states on this gameobject
        AIState[] states = GetComponents<AIState>();

        //Loop through all states and add them to the state dictionary
        foreach(AIState state in states) {
            if (state != null && !_states.ContainsKey(state.GetStateType()))
            {
                // Add this state to the state dictionary
                _states[state.GetStateType()] = state;
                // For each state found in the gameobject, we setup the state's state machine.
                state.SetStateMachine(this);
            }
        }

        // Initialize current state
        if (_states.ContainsKey(_currentStateType)) {
            _currentState = _states[_currentStateType];
            _currentState.OnEnterState();
        } else {
            _currentState = null;
        }

        if (_animator) {
            AIStateMachineLink[] _scripts = _animator.GetBehaviours<AIStateMachineLink>();
            foreach (AIStateMachineLink script in _scripts)
            {
                script.stateMachine = this;
            }
        }
    }

    protected virtual void Update() {
        if (_currentState == null) return;

        // Try updating the current state and see what the next state it returns
        AIStateType newStateType = _currentState.OnUpdate();
        // If the returned state is different from the current one, then we transition to it. 
        if (newStateType != _currentStateType) {
            AIState newState = null;
            if (_states.TryGetValue(newStateType, out newState)) {
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            } else if(_states.TryGetValue(AIStateType.Idle, out newState)) {
                // If we can't find the state, we got back to IDlEing 
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }

            // Update the next state type.
            _currentStateType = newStateType;
        }
    }

    protected virtual void FixedUpdate() {
        // Clear both visual and audio threats to keep things updated
        VisualThreat.Clear();
        AudioThreat.Clear();

        // Update the distance to the target
        if (_target.type != AITargetType.None) {
            _target.distance = Vector3.Distance(transform.position, _target.position);
        }

        _isTargetReached = false;
    }

    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d) {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null) {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d, float s)
    {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = s;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }
    
    public void SetTarget(AITarget t) {
        _target = t;

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }
    
    // Clearing the target
    public void ClearTarget() {
        _target.Clear();
        // When we clear the target, we clear the target trigger too
        if (_targetTrigger.enabled) {
            _targetTrigger.enabled = false;
        }
    }

    // Lets the AI agent know when it enters the sphere of influence of a waypoint or a player's last sightest position
    protected virtual void OnTriggerEnter(Collider other) {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = true;

        // Notify child state
        if (_currentState)
            _currentState.OnDestinationReach(true);
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = true;
    }

    // When the AI agent leaves a waypoint or a player's last sightest position
    protected virtual void OnTriggerExit(Collider other) {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = false;

        // Notify child state
        if (_currentState)
            _currentState.OnDestinationReach(false);
    }

    public virtual void OnTriggerEvent(AITriggerEventType type, Collider other) {
        if (_currentState != null)
            _currentState.OnTriggerEvent(type, other);
    }

    protected virtual void OnAnimatorMove()
    {
        if (_currentState != null) {
            _currentState.OnAnimatorUpdated();
        }
    }

    protected virtual void OnAnimatorIK(int layerIndex)
    {
        if (_currentState != null) {
            _currentState.OnAnimatorIKUpdated(layerIndex);
        }
    }

    // Set the NavAgent should update position and rotation. 
    public void NavAgentControl (bool updatePosition, bool updateRotation) {
        if (_navAgent) {
            _navAgent.updatePosition = updatePosition;
            _navAgent.updateRotation = updateRotation;
        }
    }

    // This should be called by the animation state machine
    public void AddRootMotionRequest(int rootPosition, int rootRotation) {
        _rootPositionRefCount += rootPosition;
        _rootRotationRefCount += rootRotation;
    }

    public Vector3 GetWaypointPosition(bool increment) {
        if (_currentWaypoint == -1) {
            if (_randomPatrol)
                _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
            else
                _currentWaypoint = 0;
        } else if (increment)
                NextWaypoint();

        if (_waypointNetwork.Waypoints[_currentWaypoint] != null)
        {
            Transform newWaypoint = _waypointNetwork.Waypoints[_currentWaypoint];
            SetTarget(AITargetType.Waypoint, null, newWaypoint.position, Vector3.Distance(newWaypoint.position, transform.position));
            return newWaypoint.position;
        }

        return Vector3.zero;
    }

    void NextWaypoint()
    {
        if (_randomPatrol && _waypointNetwork.Waypoints.Count > 1)
        {
            int oldWaypoint = _currentWaypoint;
            while (_currentWaypoint == oldWaypoint)
            {

                _currentWaypoint = Random.Range(0, _waypointNetwork.Waypoints.Count);
            }
        }
        else _currentWaypoint = _currentWaypoint == _waypointNetwork.Waypoints.Count - 1 ? 0 : _currentWaypoint + 1;
    }
}
