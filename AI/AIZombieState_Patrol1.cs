using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIZombieState_Patrol1 : AIZombieState
{
    // Inspector assigned
    [SerializeField][Range(0f, 3f)] float _speed = 0;
    [SerializeField] float _turnOnSpotThreshold = 5f;
    [SerializeField] float _slerpSpeed = 5f;

    public override AIStateType GetStateType() {
        return AIStateType.Patrol;
    }

    public override void OnEnterState() {
        Debug.Log("Entered Patrolling");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;
        
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        // _zombieStateMachine.crawling = false;
        _zombieStateMachine.attackType = 0;

        // Set Destination
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));

        _zombieStateMachine.navAgent.isStopped = false;
    }

    public override AIStateType OnUpdate() {
        if (_zombieStateMachine == null) return AIStateType.Idle;

        // Stay in Idle until there is a player visual threat. 
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player) {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light) {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio) {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food) {
            if (1 - _zombieStateMachine.satistfaction > (_zombieStateMachine.VisualThreat.distance / _zombieStateMachine.sensorRadius)) {
                _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }
        }

        if (_zombieStateMachine.navAgent.pathPending) {

            _zombieStateMachine.speed = 0;
            return AIStateType.Patrol;
        } else {
            
            _zombieStateMachine.speed = _speed;
        }

        float angle = Vector3.Angle(_zombieStateMachine.transform.forward, _zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position);
        if (angle > _turnOnSpotThreshold && !_zombieStateMachine.isCrawling) {
            return AIStateType.Alerted;
        }

        if (!_zombieStateMachine.useRootRotation) {
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        if (_zombieStateMachine.navAgent.isPathStale 
            || !_zombieStateMachine.navAgent.hasPath
            || _zombieStateMachine.navAgent.pathStatus != NavMeshPathStatus.PathComplete) {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
        }
        

        return AIStateType.Patrol;
    }

    public override void OnDestinationReach(bool isReached) {

        if (_zombieStateMachine == null || !isReached) return;

        if (_zombieStateMachine.targetType == AITargetType.Waypoint) {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
        }
    }
}
