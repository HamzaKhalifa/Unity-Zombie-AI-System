using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIBoneControlType { Animated, Ragdoll, RagdollToAnim }
public enum AIScreamPosition { Entity, Player }

public class BodyPartSnapshot {
    public Transform transform;
    public Vector3 position;
    public Quaternion rotation;
}

public class AIZombieStateMachine : AIStateMachine
{
    [SerializeField] [Range(10.0f, 360.0f)] float _fov = 50.0f;
    [SerializeField] [Range(0f, 1.0f)] float _sight = 0.5f;
    [SerializeField] [Range(0f, 1.0f)] float _hearing = 1.0f;
    [SerializeField] [Range(0f, 1.0f)] float _aggression = 0.5f;
    [SerializeField] [Range(0.0f, 100.0f)] int _health = 100;
    [SerializeField] [Range(0.0f, 100.0f)] int _lowerBodyDamage = 0;
    [SerializeField] [Range(0.0f, 100.0f)] int _upperBodyDamage = 0;
    [SerializeField] [Range(0, 100)] int _upperBodyThreshold = 30;
    [SerializeField] [Range(0, 100)] int _limpThreshold = 30;
    [SerializeField] [Range(0, 100)] int _crawlThreshold = 90;
    [SerializeField] [Range(0f, 1.0f)] float _intelligence = 0.5f;
    [SerializeField] [Range(0f, 1.0f)] float _satisfaction = 1.0f;
    [SerializeField] float _replenishRate = .5f;
    [SerializeField] float _depletionRate = .1f;
    [SerializeField] float _reanimationBlendTime = 1.5f;
    [SerializeField] float _reanimationWaitTime = 3f;
    [SerializeField] LayerMask _geometryLayers = 0;
    [SerializeField] [Range(0f, 1f)] float _screamChance = 1f;
    [SerializeField] [Range(0f, 1f)] float _screamRadius = 20f;
    [SerializeField] AIScreamPosition _screamPosition = AIScreamPosition.Entity;
    [SerializeField] AISoundEmitter _screamPrefab = null;
    [SerializeField] AudioCollection _ragdollColletion = null;

    int _seeking = 0;
    bool _feeding = false;
    bool _crawling = false;
    int _attackType = 0;
    float _speed = 0;
    float _isScreaming = 0f;
    float _nextRagdollSoundTime = 0f;

    // Ragdoll stuff
    AIBoneControlType _boneControlType = AIBoneControlType.Animated;
    List<BodyPartSnapshot> _bodyPartSnapshots = new List<BodyPartSnapshot>();
    float _ragdollEndTime = float.MinValue;
    Vector3 _ragdollHipPosition = Vector3.zero;
    Vector3 _ragdollFeetPosition = Vector3.zero;
    Vector3 _ragdollHeadPosition = Vector3.zero;
    IEnumerator _reanimationCoroutine = null;
    float _mecanimTransitionTime = .1f;

    // Hashes
    int _speedHash = Animator.StringToHash("Vertical");
    int _feedingHash = Animator.StringToHash("Feeding");
    int _seekingHash = Animator.StringToHash("Seeking");
    int _attackHash = Animator.StringToHash("Attack");
    int _crawlingHash = Animator.StringToHash("Crawling");
    int _screamHash = Animator.StringToHash("Scream");
    int _screamingHash = Animator.StringToHash("Screaming");
    int _hitTriggerHash = Animator.StringToHash("Hit");
    int _hitTypeHash = Animator.StringToHash("HitType");
    int _lowerBodyDamageHash = Animator.StringToHash("Lower Body Damage");
    int _upperBodyDamageHash = Animator.StringToHash("Upper Body Damage");
    int _reanimateFromBackHash = Animator.StringToHash("Reanimate From Back");
    int _reanimateFromFrontHash = Animator.StringToHash("Reanimate From Front");
    int _stateHash = Animator.StringToHash("State");

    // Layer indexes
    int _lowerBodyDamageLayer = -1;
    int _upperBodyDamageLayer = -1;

    // The first ones only have getters because the child states are never gonnas set these directly.
    public float replenishRate { get { return _replenishRate; } }
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
    public float speed
    {
        get
        {
            return _speed;
        }
        set
        {
            _speed = value;
        }
    }
    public bool isCrawling { get { return _lowerBodyDamage >= _crawlThreshold; } }
    public bool isLimping { get { return (_lowerBodyDamage >= _limpThreshold && _lowerBodyDamage < _crawlThreshold); } }
    public bool isScreaming { get { return _isScreaming > 0.1f; } }
    public float screamChance { get { return _screamChance; }}

    public bool Scream() {
        if (isScreaming) return true;
        if (_animator == null || IsLayerActive("Cinematic") || _screamPrefab == null) return false;

        _animator.SetTrigger(_screamHash);
        Vector3 spawnPosition = _screamPosition == AIScreamPosition.Entity ? transform.position : VisualThreat.position;
        AISoundEmitter screamEmitter = Instantiate(_screamPrefab, spawnPosition, Quaternion.identity) as AISoundEmitter;

        if (screamEmitter != null) {
            screamEmitter.SetRadius(_screamRadius);
        }

        return true;
    }

    protected override void Start()
    {
        base.Start();

        if (_animator != null) {
            _lowerBodyDamageLayer = _animator.GetLayerIndex("Lower Body Damage");
            _upperBodyDamageLayer = _animator.GetLayerIndex("Upper Body Damage");
        }
            

        // Create body part snapshot List
        if (_rootBone != null) {
            Transform[] transforms = _rootBone.GetComponentsInChildren<Transform>();
            foreach(Transform trans in transforms) {
                BodyPartSnapshot snapshot = new BodyPartSnapshot();
                snapshot.transform = trans;
                _bodyPartSnapshots.Add(snapshot);
            }
        }

        UpdateAnimatorDamage();
    }

    protected override void Update()
    {
        base.Update();

        if (_animator != null)
        {
            _animator.SetFloat(_speedHash, _speed);
            _animator.SetBool(_feedingHash, _feeding);
            _animator.SetInteger(_seekingHash, _seeking);
            _animator.SetInteger(_attackHash, _attackType);
            _animator.SetBool(_crawlingHash, isCrawling);
            _animator.SetInteger(_stateHash, (int)_currentStateType);

            _isScreaming = IsLayerActive("Cinematic") ? 0f : _animator.GetFloat(_screamingHash);
        }

        _satisfaction = Mathf.Max(0, _satisfaction - (_depletionRate * Time.deltaTime * Mathf.Pow(_speed, 3)) / 100);
    }

    protected virtual void LateUpdate()
    {
        if (_boneControlType == AIBoneControlType.RagdollToAnim) {
            // The first time the late update function is gonna be called, the animator won't be playing the right animation after we set the triggers in the previous frame
            // So we wait for a period of time since the last time the zombie was ragdolled: 
            if (Time.time <= _ragdollEndTime + _mecanimTransitionTime) {
                // The ragdoll hip may be in a different position from that understood and set by the animator.
                Vector3 animatedToRagdoll = _ragdollHipPosition - _rootBone.position;
                // Sliding our transform along the previous vector to the location of the ragdoll hip.
                // We this, we only get the right x and z position of the character.
                Vector3 newRootPosition = transform.position + animatedToRagdoll;

                RaycastHit[] hits = Physics.RaycastAll(newRootPosition, Vector3.down, float.MaxValue, _geometryLayers);
                //newRootPosition.y = float.MinValue;
                foreach(RaycastHit hit in hits) {
                    if (!hit.transform.IsChildOf(transform)) {
                        //newRootPosition.y = Mathf.Max(newRootPosition.y, hit.point.y);
                    }
                }

                NavMeshHit navMeshHit;
                Vector3 baseOffset = Vector3.zero;
                if (_navAgent) baseOffset.y = _navAgent.baseOffset;
                if (NavMesh.SamplePosition(newRootPosition, out navMeshHit, 2f, NavMesh.AllAreas))
                {
                    transform.position = navMeshHit.position + baseOffset;
                } else {
                    transform.position = newRootPosition + baseOffset;
                }

                Vector3 ragdollDirection = _ragdollHeadPosition - _ragdollFeetPosition;
                ragdollDirection.y = 0;

                Vector3 meanFeetPosition = .5f * (_animator.GetBoneTransform(HumanBodyBones.RightFoot).position + _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);
                Vector3 animatedDirection = _animator.GetBoneTransform(HumanBodyBones.Head).position - meanFeetPosition;
                animatedDirection.y = 0f;

                transform.rotation *= Quaternion.FromToRotation(animatedDirection.normalized, ragdollDirection.normalized);
            }

            float blendAmount = Mathf.Clamp01((Time.time - _ragdollEndTime - _mecanimTransitionTime) / _reanimationBlendTime);

            // Calculate blended bone positions by interpolating between ragdoll bone snapshots and animated bone positions 
            foreach(BodyPartSnapshot snapshot in _bodyPartSnapshots) {
                if (snapshot.transform == _rootBone) {
                    snapshot.transform.position = Vector3.Lerp(snapshot.position, snapshot.transform.position, blendAmount);
                }
                // All other child bones are gonna automatically move with the parent bone. 
                // They are just gonna be rotated by the animator
                snapshot.transform.rotation = Quaternion.Slerp(snapshot.rotation, snapshot.transform.rotation, blendAmount);
            }

            // Conditional to exit reanimation code
            if (blendAmount >= 1) {
                _boneControlType = AIBoneControlType.Animated;
                if (_navAgent) _navAgent.enabled = true;
                if (_collider) _collider.enabled = true;

                AIState newState = null;
                if (_states.TryGetValue(AIStateType.Alerted, out newState)) {
                    if (_currentState != null) _currentState.OnExitState();

                    newState.OnEnterState();
                    _currentState = newState;
                    _currentStateType = AIStateType.Alerted;
                }
            }
        }

    }

    public void UpdateAnimatorDamage() {
        if (_animator != null) {
            if (_lowerBodyDamageLayer != -1) {
                int lowerBodyDamageLayerValue = _lowerBodyDamage >= _limpThreshold && _lowerBodyDamage < _crawlThreshold ? 1 : 0;
                _animator.SetLayerWeight(_lowerBodyDamageLayer, lowerBodyDamageLayerValue);
            }
            if (_upperBodyDamageLayer != -1)
            {
                    _animator.SetLayerWeight(_upperBodyDamageLayer, _upperBodyDamage >= _upperBodyThreshold? 1 : 0);
            }

            _animator.SetBool(_crawlingHash, isCrawling);
            _animator.SetInteger(_lowerBodyDamageHash, _lowerBodyDamage);
            _animator.SetInteger(_upperBodyDamageHash, _upperBodyDamage);

            if (_lowerBodyDamage < _limpThreshold && _lowerBodyDamage < _crawlThreshold) SetLayerActive("Lower Body Damage", true);
            else SetLayerActive("Lower Body Damage", false);

            if (_upperBodyDamage < _upperBodyThreshold && _lowerBodyDamage < _crawlThreshold) SetLayerActive("Upper Body Damage", true);
            else SetLayerActive("Upper Body Damage", false);
        }
    }

    public override void TakeDamage(Vector3 position,
                           Vector3 force,
                           int damage,
                           // The body part that got hit
                           Rigidbody bodyPart,
                           // To tell the damage system wchich player hit the zombie
                           CharacterManager characterManager,
                           int hitDirection = 0)
    {
        base.TakeDamage(position, force, damage, bodyPart, characterManager, hitDirection);

        if (GameSceneManager.instance != null && GameSceneManager.instance.bloodParticles != null)
        {
            ParticleSystem system = GameSceneManager.instance.bloodParticles;
            system.transform.position = position;
            system.simulationSpace = ParticleSystemSimulationSpace.World;
            system.Emit(60);
        }

        float hitStrength = force.magnitude;
        float prevHealth = _health;

        if (_boneControlType == AIBoneControlType.Ragdoll) {
            if (bodyPart != null) {

                if (Time.time > _nextRagdollSoundTime && _ragdollColletion != null && _health > 0) {
                    AudioClip clip = _ragdollColletion[1];
                    if (clip) {
                        _nextRagdollSoundTime = Time.time + clip.length;
                        AudioManager.instance.PlayOneShotSound(_ragdollColletion.audioGroup,
                                                               clip,
                                                               position,
                                                               _ragdollColletion.volume,
                                                               _ragdollColletion.spatialBlend,
                                                               _ragdollColletion.priority);
                    }
                }

                if (hitStrength > 1)
                    bodyPart.AddForce(force, ForceMode.Impulse);
                if (bodyPart.CompareTag("Head")) {
                    _health = Mathf.Max(_health - damage, 0);
                } else if (bodyPart.CompareTag("Upper Body")) {
                    _upperBodyDamage += damage;
                } else if (bodyPart.CompareTag("Lower Body"))
                {
                    _lowerBodyDamage += damage;
                }

                UpdateAnimatorDamage();

                if (_health > 0) {
                    // Reanimate zombie
                    if (_reanimationCoroutine != null) StopCoroutine(_reanimationCoroutine);

                    _reanimationCoroutine = Reanimate();
                    StartCoroutine(_reanimationCoroutine);
                }
            }
            return;
        }

        // Get local space position of attacker
        Vector3 attackerLocPos = transform.InverseTransformPoint(characterManager.transform.position);
        // Get local space position of hit
        Vector3 hitLocPos = transform.InverseTransformPoint(position);

        // We ragdoll if the hit strength is superior to 1
        bool shouldRagdoll = hitStrength > 1f;

        if (bodyPart != null)
        {
            if (bodyPart.CompareTag("Head"))
            {
                _health = Mathf.Max(_health - damage, 0);
                if (_health == 0)
                {
                    // We ragdoll if the zombie is dead: health = 0
                    shouldRagdoll = true;
                }
            }
            else if (bodyPart.CompareTag("Upper Body"))
            {
                _upperBodyDamage += damage;
                UpdateAnimatorDamage();
            }
            else if (bodyPart.CompareTag("Lower Body"))
            {
                _lowerBodyDamage += damage;
                UpdateAnimatorDamage();
                // When we do a leg shot, the zombie will always ragdoll
                shouldRagdoll = true;
            }
        }

        // We ragdoll if we are playing a cinematic (eating for example), 
        // if we are getting attacked from behind, 
        // if we are not in the animated state (meaning that we are trying to reanimate),
        // if we are crawling
        if (_boneControlType != AIBoneControlType.Animated || isCrawling || IsLayerActive("Cinematic") || attackerLocPos.z < 0) {
            shouldRagdoll = true;
        }

        if (!shouldRagdoll) {
            // We try to play one of hit animations
            float angle = 0f;
            if (hitDirection == 0) {
                Vector3 vecToHit = (position - transform.position).normalized;
                angle = AIState.FindSignedAngle(vecToHit, transform.forward);
            }
            int hitType = 0;
            if (bodyPart.CompareTag("Head")) {
                if (angle < -10 || hitDirection == -1)
                {
                    hitType = 1;
                }
                else if (angle > 10 || hitDirection == 1)
                {
                    hitType = 3;
                }
                else hitType = 2;
            } else if (bodyPart.gameObject.CompareTag("Upper Body")) {
                if (angle < -20 || hitDirection == -1)
                {
                    hitType = 4;
                }
                else if (angle > 20 || hitDirection == 1)
                {
                    hitType = 6;
                }
                else hitType = 5;
            }
            if (_animator) {
                _animator.SetInteger(_hitTypeHash, hitType);
                _animator.SetTrigger(_hitTriggerHash);
            }

        } else {
            // We are ragdolling

            // We exit the current state when we are ragdolling to avoid any moving or other actions.
            if (_currentState) {
                _currentState.OnExitState();
                _currentState = null;
                _currentStateType = AIStateType.None;
            }

            if (_navAgent) _navAgent.enabled = false;

            if (_animator) _animator.enabled = false;

            if (_collider) _collider.enabled = false;

            // Mute audio while ragdoll is happening
            if (_layeredAudioSource != null) {
                _layeredAudioSource.Mute(true);
            }

            if (Time.time > _nextRagdollSoundTime && _ragdollColletion != null && prevHealth > 0) {
                AudioClip clip = _ragdollColletion[0];
                if (clip != null)
                {
                    _nextRagdollSoundTime = Time.time + clip.length;
                    AudioManager.instance.PlayOneShotSound(_ragdollColletion.audioGroup,
                                                          clip,
                                                           position,
                                                           _ragdollColletion.volume,
                                                           _ragdollColletion.spatialBlend,
                                                           _ragdollColletion.priority);
                }
            }

            inMeleeRange = false;

            // Transform all the rigid bodies to non kinematic so that they could start applying their physics
            foreach(Rigidbody body in _bodyParts) {
                if (body != null) {
                    body.isKinematic = false;
                }
            }

            // Add force to the part that got hit in the hit direction
            if (hitStrength > 1f) {
                bodyPart.AddForce(force, ForceMode.Impulse);
            }

            _boneControlType = AIBoneControlType.Ragdoll;

            if (_health > 0) {
                // Reanimate zombie
                if (_reanimationCoroutine != null) StopCoroutine(_reanimationCoroutine);

                _reanimationCoroutine = Reanimate();
                StartCoroutine(_reanimationCoroutine);
            }
        }
    }

    protected virtual IEnumerator Reanimate() {
        if (_boneControlType != AIBoneControlType.Ragdoll || _animator == null) {
            yield break;
        }

        yield return new WaitForSeconds(_reanimationWaitTime);
       

        _ragdollEndTime = Time.time;

        foreach(Rigidbody body in _bodyParts) {
            body.isKinematic = true;
        }

        _boneControlType = AIBoneControlType.RagdollToAnim;

        foreach(BodyPartSnapshot snapshot in _bodyPartSnapshots) {
            snapshot.position = snapshot.transform.position;
            snapshot.rotation = snapshot.transform.rotation;
        }

        // Record ragdolls head and feet position
        _ragdollHeadPosition = _animator.GetBoneTransform(HumanBodyBones.Head).position;
        _ragdollFeetPosition = (_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + _animator.GetBoneTransform(HumanBodyBones.RightFoot).position) * .5f;
        _ragdollHipPosition = _rootBone.position;

        _animator.enabled = true;

        if (_rootBone != null)
        {
            float forwardTest;
            switch (_rootBoneAlignment) {
                case AIBoneAlignmentType.ZAxis:
                    forwardTest = _rootBone.forward.y; break;
                case AIBoneAlignmentType.ZAxisInverted:
                    forwardTest = -_rootBone.forward.y; break;
                case AIBoneAlignmentType.YAxis:
                    forwardTest = _rootBone.up.y; break;
                case AIBoneAlignmentType.YAxisInverted:
                    forwardTest = -_rootBone.up.y; break;
                case AIBoneAlignmentType.XAxis:
                    forwardTest = _rootBone.right.y; break;
                case AIBoneAlignmentType.XAxisInverted:
                    forwardTest = -_rootBone.right.y; break;
                default: forwardTest = _rootBone.forward.y; break;
            }

            _animator.SetTrigger(forwardTest >= 0 ? _reanimateFromFrontHash : _reanimateFromBackHash);
        }
    }
}
