using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace DefaultNamespace
{
    [RequireComponent(typeof(Rigidbody))]
    public class RigidBodyReport : MonoBehaviour
    {
        Rigidbody rb;

        float s;
        
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            //rb.velocity = Vector3.down*100;
        }

        void FixedUpdate()
        {
            Debug.Log(rb.velocity.magnitude);
            //Debug.Log((rb.velocity.magnitude-s)/Time.fixedDeltaTime);
            //Debug.Log((rb.velocity.y-s)/Time.fixedDeltaTime);
            //s = rb.velocity.y;
            //s = rb.velocity.magnitude;
        }
    }
}