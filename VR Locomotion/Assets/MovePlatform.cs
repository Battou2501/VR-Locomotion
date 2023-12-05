using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class MovePlatform : MonoBehaviour
    {
        public bool move;
        public bool roll;
        
        public Vector3 moveVector;
        public float acceleration;
        public float maxSpeed;
        float speed;
        float angle;
        void OnEnable()
        {
            speed = 0;
            angle = Mathf.PI*0.5f;
        }

        void FixedUpdate()
        {
            if (speed < maxSpeed)
                speed += Time.deltaTime * acceleration;


            angle += Time.fixedDeltaTime;

            if (angle > Mathf.PI * 2)
                angle = 0;
            
            if(move)
                transform.position += moveVector * Time.fixedDeltaTime * speed;

            if(roll)
                transform.rotation = Quaternion.identity * Quaternion.AngleAxis(Mathf.Cos(angle)*5f, Vector3.right);
        }
    }
}