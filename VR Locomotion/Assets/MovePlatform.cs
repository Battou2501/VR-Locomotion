using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class MovePlatform : MonoBehaviour
    {
        public Vector3 moveVector;

        void FixedUpdate()
        {
            transform.position += moveVector * Time.fixedDeltaTime * 1000;
            Physics.SyncTransforms();
        }
    }
}