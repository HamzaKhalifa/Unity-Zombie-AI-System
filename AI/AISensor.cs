using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISensor : MonoBehaviour
{
    AIStateMachine _parentStateMachine = null;

    public AIStateMachine parentStateMachine { set { _parentStateMachine = value; } }

    protected virtual void OnTriggerEnter(Collider col)
    {
        // Notify child state
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Enter, col);
    }

    protected virtual void OnTriggerStay(Collider col)
    {
        // Notify child state
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Stay, col);
    }

    protected virtual void OnTriggerExit(Collider col)
    {
        // Notify child state
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.Exit, col);
    }
}
