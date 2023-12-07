using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class ColliderMover : MonoBehaviour
    {
        public Rigidbody body;
        public Transform target;
        
        
        void FixedUpdate()
        {
            var vec = target.position - transform.position;
            var dist = vec.magnitude;
            var new_pos = transform.position + vec.normalized * Mathf.Min(dist, 10f * Time.fixedDeltaTime);
            body.MovePosition(new_pos);
        }
    }
}