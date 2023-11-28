using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    CapsuleCollider col;

    // Start is called before the first frame update
    void Start()
    {
        col = this.GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        var move_vec = Vector3.forward;
        
        if (Physics.CapsuleCast(transform.position - Vector3.up * (col.height - col.radius * 2) * 0.5f, transform.position +  Vector3.up * (col.height - col.radius * 2) * 0.5f, col.radius, move_vec, out var hit)
            && hit.distance < Time.deltaTime * 3)
        {

            Debug.DrawRay(hit.point, hit.normal, Color.red);

            var v_right = Vector3.Cross(Vector3.up, move_vec).normalized;
            
            var v = Vector3.ProjectOnPlane(hit.normal, v_right).normalized;

            move_vec = Vector3.ProjectOnPlane(Vector3.forward, v).normalized;
            
            Debug.DrawRay(hit.point, Vector3.ProjectOnPlane(Vector3.forward, v), Color.blue);
        }

        if(Input.GetKey(KeyCode.UpArrow))
            transform.position += move_vec * Time.deltaTime * 3;


        if (Physics.CapsuleCast(transform.position - Vector3.up * (col.height - col.radius * 2) * 0.5f, transform.position + Vector3.up * (col.height - col.radius * 2) * 0.5f, col.radius, Vector3.down, out var hit_slide))
        {
            Debug.DrawRay(hit_slide.point, hit_slide.normal, Color.red);
            
            Debug.DrawRay(hit_slide.point, Vector3.ProjectOnPlane(Vector3.down, hit_slide.normal).normalized, Color.blue);
        }
    }
}
