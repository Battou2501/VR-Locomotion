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
        float delta_time;
        
        void OnEnable()
        {
            speed = 0;
            angle = Mathf.PI*0.5f;
        }

        void Update()
        //void FixedUpdate()
        {
            delta_time = Time.deltaTime;
            //delta_time = Time.fixedDeltaTime;
            
            if (speed < maxSpeed)
                speed += delta_time * acceleration;


            angle += delta_time;

            if (angle > Mathf.PI * 2)
                angle = 0;
            
            if(move)
                transform.position += moveVector * delta_time * speed;

            if(roll)
                transform.rotation = Quaternion.identity * Quaternion.AngleAxis(Mathf.Cos(angle)*5f, Vector3.right);
        }
    }
}