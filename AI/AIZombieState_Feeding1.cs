using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Feeding1 : AIZombieState
{
    [SerializeField] Transform _bloodParticleMount = null;
    [SerializeField] [Range(0.01f, 1f)] float _bloodParticlesBurstTime = 0.1f;
    [SerializeField] [Range(1, 100)] int _bloodParticlesBurstAmount = 10;

    int _eatingStateHash = Animator.StringToHash("Feeding State");
    int _crawlEatingStateHash = Animator.StringToHash("Crawl Eating State");
    int _eatingLayerIndex = -1;
    float _timer = 0f;

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

        _timer = 0f;
    }

    public override AIStateType OnUpdate()
    {
        _timer += Time.deltaTime;

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

        int currentAnimatorHash = _zombieStateMachine.animator.GetCurrentAnimatorStateInfo(_eatingLayerIndex).shortNameHash;
        if (currentAnimatorHash == _eatingStateHash || currentAnimatorHash == _crawlEatingStateHash) {
            _zombieStateMachine.satistfaction = Mathf.Min(_zombieStateMachine.satistfaction + Time.deltaTime * _zombieStateMachine.replenishRate / 100, 1f);
            if (GameSceneManager.instance && GameSceneManager.instance.bloodParticles && _bloodParticleMount) {
                if (_timer >= _bloodParticlesBurstTime) {
                    ParticleSystem system = GameSceneManager.instance.bloodParticles;
                    system.transform.position = _bloodParticleMount.position;
                    system.transform.rotation = _bloodParticleMount.rotation;

                    system.simulationSpace = ParticleSystemSimulationSpace.World;
                    system.Emit(_bloodParticlesBurstAmount);
                    _timer = 0f;

                }
            }
        }

        Vector3 headToTarget = _zombieStateMachine.targetPosition - _zombieStateMachine.animator.GetBoneTransform(HumanBodyBones.Head).position;
        _zombieStateMachine.transform.position = Vector3.Lerp(
                                                            _zombieStateMachine.transform.position,
                                                            _zombieStateMachine.transform.position + headToTarget,
                                                            Time.deltaTime);

        return AIStateType.Feeding;
    }

    public override void OnExitState() {
        if (_zombieStateMachine != null)
            _zombieStateMachine.feeding = false;
    }
}
