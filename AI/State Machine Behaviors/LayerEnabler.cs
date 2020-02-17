using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerEnabler : AIStateMachineLink
{
    public bool OnEnter = false;
    public bool OnExit = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            _stateMachine.SetLayerActive(animator.GetLayerName(layerIndex), OnEnter);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            _stateMachine.SetLayerActive(animator.GetLayerName(layerIndex), OnExit);
        }
    }
}
