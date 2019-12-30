using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Feeding1 : AIZombieState
{
    [SerializeField] float _slerpSpeed = 5f;

    int _eatingStateHash = Animator.StringToHash("Feeding State");
    int _eatingLayerIndex = -1;

    public override AIStateType GetStateType() {
        return AIStateType.Feeding;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entered Feeding State");
        base.OnEnterState();
        if (_zombieStateMachine == null) return;

        // Get the layer index
        if (_eatingLayerIndex == -1) {
            _eatingLayerIndex = _zombieStateMachine.animator.GetLayerIndex("Cinematic");
        }

        _zombieStateMachine.feeding = true;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.attackType = 0;
    }

    public override AIStateType OnUpdate()
    {
        if (_zombieStateMachine.satistfaction > .9f) {
            _zombieStateMachine.GetWaypointPosition(false);
            return AIStateType.Alerted;
        }

        // Stay in Idle until there is a player visual threat. 
        if (_zombieStateMachine.VisualThreat.type != AITargetType.None && _zombieStateMachine.VisualThreat.type != AITargetType.Visual_Food)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Audio)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        if (_zombieStateMachine.animator.GetCurrentAnimatorStateInfo(_eatingLayerIndex).shortNameHash == _eatingStateHash) {
            _zombieStateMachine.satistfaction = Mathf.Min(_zombieStateMachine.satistfaction + Time.deltaTime * _zombieStateMachine.replenishRate / 100, 1f);
        }

        if (!_zombieStateMachine.useRootRotation) {
            Vector3 targetPosition = _zombieStateMachine.targetPosition;
            Quaternion newRot = Quaternion.LookRotation(targetPosition - _zombieStateMachine.transform.position);

            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, _slerpSpeed * Time.deltaTime);
        }

        return AIStateType.Feeding;
    }

    public override void OnExitState() {
        if (_zombieStateMachine != null)
            _zombieStateMachine.feeding = false;
    }
}
