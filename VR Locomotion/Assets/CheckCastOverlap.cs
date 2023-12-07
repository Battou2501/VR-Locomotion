using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class CheckCastOverlap : MonoBehaviour
    {
        bool is_set;
        bool is_ready;
        Vector3 point_A;
        Vector3 vector_B;
        float radius_C = 0.34f;

        void Update()
        {
            if (!is_set) set_experiment();

            if (!is_ready) return;

            var is_hit = Physics.SphereCast(
                point_A,
                radius_C,
                vector_B,
                out var hit);

            if (!is_hit) return;

            var overlap = Physics.OverlapSphere(point_A + vector_B * hit.distance, radius_C);

            Debug.DrawRay(point_A, vector_B * 10, Color.red);
            Debug.Log("Sphere cast detected a hit! " + "  Collider name: " + hit.collider.name + "   Is overlap sphere in same plase detected any colliders: " + (overlap is {Length: > 0}));

        }

        void set_experiment()
        {
            is_set = true;
            
            var is_hit = Physics.SphereCast(
                transform.position,
                radius_C,
                Vector3.forward, 
                out var hit);
            
            if(!is_hit) return;
            
            transform.position+=Vector3.forward*hit.distance;


            var v = Vector3.ProjectOnPlane(hit.normal, Vector3.right);

            vector_B = Vector3.ProjectOnPlane(Vector3.forward, v).normalized;

            point_A = transform.position - vector_B * 0.05f;
            
            is_ready = true;
        }
    }
}