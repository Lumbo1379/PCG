using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErrorScript : MonoBehaviour
{
    void Start()
    {
        var rb = GetComponent<Rigidbody>();
        var v = rb.velocity;  // Will cause an error when rigidBody component is not present
    }
}
