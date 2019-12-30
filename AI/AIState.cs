using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIState : MonoBehaviour
{
    protected AIStateMachine _stateMachine;
    public virtual void SetStateMachine(AIStateMachine machine) {
        _stateMachine = machine;
    }

    // Default handlers
    public virtual void OnEnterState() {}
    public virtual void OnExitState() {}
    public void OnAnimatorUpdated() {
        if (_stateMachine.useRootPosition) {
            _stateMachine.navAgent.velocity = _stateMachine.animator.deltaPosition / Time.deltaTime;
        }

        if (_stateMachine.useRootRotation) {
            _stateMachine.transform.rotation = _stateMachine.animator.rootRotation;
        }
    }
    public virtual void OnAnimatorIKUpdated(int layerIndex) {}
    public virtual void OnTriggerEvent(AITriggerEventType eventType, Collider other) {}
    public virtual void OnDestinationReach(bool isReached) {}

    public abstract AIStateType GetStateType();
    public abstract AIStateType OnUpdate();

    public static void ConvertSphereColliderToWorldSpace(SphereCollider col, out Vector3 pos, out float radius) {
        // Default values
        pos = Vector3.zero;
        radius = 0.0f;
        
        if (col == null) return;

        pos = col.transform.position;
        pos.x += col.center.x * col.transform.lossyScale.x;
        pos.y += col.center.y * col.transform.lossyScale.y;
        pos.z += col.center.z * col.transform.lossyScale.z;

        radius = Mathf.Max(col.radius * col.transform.lossyScale.x, 
                                    col.radius * col.transform.lossyScale.y);

        radius = Mathf.Max(radius, col.radius * col.transform.lossyScale.z);
    }

    // Return the angle between two vectors in degrees
    public static float FindSignedAngle(Vector3 fromVector, Vector3 toVector) {
        if (fromVector == toVector) return 0f;

        float angle = Vector3.Angle(fromVector, toVector);
        Vector3 cross = Vector3.Cross(fromVector, toVector);
        angle *= Mathf.Sign(cross.y);

        return angle;
    }
}
