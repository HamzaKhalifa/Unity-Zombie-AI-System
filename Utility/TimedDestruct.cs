using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedDestruct : MonoBehaviour
{
    [SerializeField] float _time = 10f;

    private void Awake()
    {
        Invoke("DestroyObject", _time);
    }

    void DestroyObject() {
        Destroy(gameObject);
    }
}
