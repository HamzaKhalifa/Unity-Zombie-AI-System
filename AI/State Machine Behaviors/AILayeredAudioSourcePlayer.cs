using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AILayeredAudioSourcePlayer : AIStateMachineLink
{
    [SerializeField] AudioCollection _collection = null;
    [SerializeField] int _bank = 0;
    [SerializeField] bool _looping = false;
    [SerializeField] bool _stopOnExit = false;

    float _prevLayerWeight = 0f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if (_stateMachine == null) return;

        float layerWeight = animator.GetLayerWeight(layerIndex);

        if (_collection != null) {
            if (layerWeight.Equals(0) || layerWeight > .5f) {
                _stateMachine.PlayAudio(_collection, _bank, layerIndex, _looping);
            } else {
                _stateMachine.StopAudio(layerIndex);
            }
        }

        _prevLayerWeight = layerWeight;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if (_stateMachine == null) return;

        float layerWeight = animator.GetLayerWeight(layerIndex);

        if (!layerWeight.Equals(_prevLayerWeight) && _collection != null)
        {
            if (layerWeight > .5f)
            {
                _stateMachine.PlayAudio(_collection, _bank, layerIndex, _looping);
            }
            else
            {
                _stateMachine.StopAudio(layerIndex);
            }
        }

        _prevLayerWeight = layerWeight;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if (_stateMachine != null && _stopOnExit) {
            _stateMachine.StopAudio(layerIndex);
        }
    }

}
