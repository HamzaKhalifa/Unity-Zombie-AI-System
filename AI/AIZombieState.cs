using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIZombieState : AIState
{
    // Private 
    protected int _playerLayerMask = -1;
    protected int _visualLayerMask = -1;
    protected int _bodyPartLayer = -1;

    protected AIZombieStateMachine _zombieStateMachine;

    public void Awake()
    {
        _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Default");
        _bodyPartLayer = LayerMask.NameToLayer("AI Body Part");
        //_visualLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Visual Aggravator", "Default");
        _visualLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Visual Aggravator") + 1;
    }

    public override void SetStateMachine(AIStateMachine machine) {
        if (machine.GetType() == typeof(AIZombieStateMachine)) {
            base.SetStateMachine(machine);
            _zombieStateMachine = (AIZombieStateMachine) machine;
        }
    }

    public override void OnTriggerEvent(AITriggerEventType eventType, Collider other) {
        if (_zombieStateMachine == null)
            return;

        if (eventType != AITriggerEventType.Exit) {
            AITargetType curType = _zombieStateMachine.VisualThreat.type;

            if (other.CompareTag("Player")) {
                float distance = Vector3.Distance(_zombieStateMachine.sensorPosition, other.transform.position);
                if (curType != AITargetType.Visual_Player ||
                    curType == AITargetType.Visual_Player && distance < _zombieStateMachine.VisualThreat.distance) {
                    RaycastHit hitInfo;
                    if (ColliderIsVisible(other, out hitInfo, _playerLayerMask)) {
                        _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Player, other, other.transform.position, distance);
                    }
                }
            } else if (other.CompareTag("Flashlight") && curType != AITargetType.Visual_Player) {
                BoxCollider flashLightTrigger = (BoxCollider)other;
                float distanceToThreat = Vector3.Distance(_zombieStateMachine.sensorPosition, flashLightTrigger.transform.position);
                float zSize = flashLightTrigger.size.z * flashLightTrigger.transform.lossyScale.z;
                float aggrFactor = distanceToThreat / zSize;

                if (aggrFactor <= _zombieStateMachine.sight && aggrFactor <= _zombieStateMachine.intelligence) {
                    _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Light, other, other.transform.position, distanceToThreat);
                }
            } else if (other.CompareTag("AI Sound Emitter")) {
                SphereCollider soundTrigger = (SphereCollider) other;
                Vector3 agentSensorPosition = _zombieStateMachine.sensorPosition;
                Vector3 soundPos;
                float soundRadius;
                AIState.ConvertSphereColliderToWorldSpace(soundTrigger, out soundPos, out soundRadius);
                
                float distanceToThreat = Vector3.Distance(soundPos, agentSensorPosition);
                float distanceFactor = distanceToThreat / soundRadius;
                distanceFactor += distanceFactor * (1 - _zombieStateMachine.hearing);

                // Too far way
                if (distanceFactor > 1) return;

                if (distanceToThreat < _zombieStateMachine.AudioThreat.distance) {
                    _zombieStateMachine.AudioThreat.Set(AITargetType.Audio, soundTrigger, soundPos, distanceToThreat);
                }
            } else if (other.CompareTag("AI Food") && curType != AITargetType.Visual_Player 
                && curType != AITargetType.Visual_Light 
                && _zombieStateMachine.AudioThreat.type == AITargetType.None
                && _zombieStateMachine.satistfaction <= 0.9f) {
                    float distanceToThreat = Vector3.Distance(_zombieStateMachine.sensorPosition, other.transform.position);
                  
                    if (distanceToThreat < _zombieStateMachine.VisualThreat.distance) {
                        RaycastHit hitInfo;
                        if (ColliderIsVisible(other, out hitInfo, _visualLayerMask)) {
                            Debug.Log("Setting food as threat");
                            _zombieStateMachine.VisualThreat.Set(AITargetType.Visual_Food, other, other.transform.position, distanceToThreat);
                        }
                    }
            }
        }
    }

    protected virtual bool ColliderIsVisible(Collider other, out RaycastHit hitInfo, int layerMask = -1) {
        hitInfo = new RaycastHit();

        if (_zombieStateMachine == null)
            return false;

        Vector3 head = _zombieStateMachine.sensorPosition;
        Vector3 direction = other.transform.position - head;
        float angle = Vector3.Angle(direction, transform.forward);

        if (angle > _zombieStateMachine.fov * .5f) {
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(head, direction.normalized, _zombieStateMachine.sensorRadius * _zombieStateMachine.sight, layerMask);

        float closestColliderDistance = float.MaxValue;
        Collider closestCollider = null;

        foreach(RaycastHit hit in hits) {
            if (hit.transform.gameObject.layer == _bodyPartLayer) {

                if (_stateMachine != GameSceneManager.instance.GetAIStateMachine(hit.rigidbody.GetInstanceID())) {
                    closestColliderDistance = hit.distance;
                    closestCollider = hit.collider;
                    hitInfo = hit;
                }
            } else {
                closestColliderDistance = hit.distance;
                closestCollider = hit.collider;
                hitInfo = hit;
            }
        }

        if (closestCollider && closestCollider.gameObject == other.gameObject) 
            return true;

        return false;
    }

    public override AIStateType GetStateType()
    {
        throw new System.NotImplementedException();
    }

    public override AIStateType OnUpdate()
    {
        throw new System.NotImplementedException();
    }

    private void Start() {

    }
}
