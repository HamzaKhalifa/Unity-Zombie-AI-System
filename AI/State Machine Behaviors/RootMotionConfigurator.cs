using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionConfigurator : AIStateMachineLink
{
    [SerializeField] int _rootPosition = 0;
    [SerializeField] int _rootRotation = 0;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if (_stateMachine) {
            _stateMachine.AddRootMotionRequest(_rootPosition, _rootRotation);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if (_stateMachine) {
            _stateMachine.AddRootMotionRequest(-_rootPosition, -_rootRotation);
        }
    }
}
