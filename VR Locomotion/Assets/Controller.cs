using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    
    public float gravity = 9.8f;

    public float terminalVelocityFall;
    public float maxClimbAngle;
    public float slideDeceleration;

    public float moveSpeed;
    
    CapsuleCollider col;

    float fall_speed;
    float slide_speed;

    float climb_angle_dot;
    
    float prev;

    Vector3 slide_vector;
    
    bool is_grounded;
    // Start is called before the first frame update
    void Start()
    {
        col = this.GetComponent<CapsuleCollider>();
        climb_angle_dot = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
    }

    // Update is called once per frame
    void Update()
    {
        var move_vec = Vector3.zero;

        if(Keyboard.current.upArrowKey.isPressed)
            move_vec = Vector3.forward;
        
        if(Keyboard.current.downArrowKey.isPressed)
            move_vec = Vector3.back;
        
        var surface_normal = Vector3.up;
        var obstacle_normal = Vector3.up;
        //var fall_normal = Vector3.down;

        //var ground_check = Physics.CapsuleCast(
        //    transform.position - Vector3.up * (col.height - col.radius * 2) * 0.5f + Vector3.up * 0.09f,
        //    transform.position + Vector3.up * (col.height - col.radius * 2) * 0.5f,
        //    col.radius,
        //    Vector3.down,
        //    out var hit_ground) 
        //                   //&& hit_ground.distance <= Time.deltaTime * fall_speed + 0.1f  
        //                   && hit_ground.distance <= 0.1f  
        //                   //&& Vector3.Dot(Vector3.up, hit_ground.normal) >= 0.5f
        //                   ;

        var ground_height_check_val = col.height - col.radius * 2;
        
        var ground_check = Physics.SphereCast(
                               transform.position + Vector3.up * (col.height - col.radius * 2) * 0.5f,
                               col.radius,
                               Vector3.down,
                               out var hit_ground) 
                           //&& hit_ground.distance <= Time.deltaTime * fall_speed + 0.1f  
                           && hit_ground.distance <= ground_height_check_val + 0.01f
            //&& Vector3.Dot(Vector3.up, hit_ground.normal) >= 0.5f
            ;
        
        
        //Debug.Log(hit_ground.distance +"    prev: " + prev);
        
        
        
        if (ground_check)
        {
            slide_vector = Vector3.ProjectOnPlane(Vector3.down, hit_ground.normal).normalized;

            if (!is_grounded)
            {
                slide_speed = fall_speed;
                fall_speed = Time.deltaTime * gravity;
                is_grounded = true;
            }



            //stand
            if (Vector3.Dot(Vector3.up, hit_ground.normal) >= climb_angle_dot)
            {
                //if (slide_speed > Time.deltaTime * gravity + 0.1f)
                if (slide_speed > 0.1f)
                {
                    slide_speed -= Time.deltaTime * slideDeceleration;
                    slide_speed = Mathf.Max(0, slide_speed);
                    
                    transform.position += slide_vector * (Time.deltaTime * slide_speed);
                }
                else
                    slide_speed = 0;
                
                surface_normal = hit_ground.normal;
            }
            //slide
            else
            {
                var slide_terminal_velocity = terminalVelocityFall * (1f-Vector3.Dot(Vector3.up, hit_ground.normal));
                
                if(slide_speed < slide_terminal_velocity)
                    slide_speed += Time.deltaTime * gravity;
                else
                {
                    slide_speed -= Time.deltaTime * slideDeceleration * Vector3.Dot(Vector3.up, hit_ground.normal);
                    slide_speed = Mathf.Max(0, slide_speed);
                }
                
                transform.position += slide_vector * (Time.deltaTime * slide_speed);
            }
            
            if (move_vec.sqrMagnitude > 0.01f)
            {
                var v_right = Vector3.Cross(Vector3.up, move_vec).normalized;

                var v = Vector3.ProjectOnPlane(surface_normal, v_right).normalized;

                move_vec = Vector3.ProjectOnPlane(move_vec, v).normalized;
            }
            
            
            if(hit_ground.distance < ground_height_check_val)
                transform.position += Vector3.down * (hit_ground.distance - ground_height_check_val);
        }
        else
        {
            if (is_grounded)
            {
                Debug.Log(hit_ground.distance + "    prev: " + prev);
                fall_speed = slide_speed;
                is_grounded = false;
            }

            //if (hit_ground.distance <= Time.deltaTime * fall_speed + 0.1f && Vector3.Dot(Vector3.up, hit_ground.normal) < 0.5f)
            //    fall_normal = Vector3.ProjectOnPlane(fall_normal, hit_ground.normal).normalized;
            
            //prev = hit_ground.distance;
            if(fall_speed < terminalVelocityFall)
                fall_speed += Time.deltaTime * gravity;
            
            transform.position += slide_vector * (Time.deltaTime * slide_speed);
            transform.position += Vector3.down * (hit_ground.collider == null ? Time.deltaTime * fall_speed :  Mathf.Min(hit_ground.distance-ground_height_check_val, Time.deltaTime * fall_speed));// + 0.09f);
            //transform.position += Vector3.down *  (Time.deltaTime * fall_speed);// + 0.09f);

            slide_speed = Mathf.Max(0, slide_speed * (1f - Time.deltaTime));
        }
        
        prev = hit_ground.distance;
        Debug.DrawRay(transform.position, surface_normal, Color.red);

        //if(is_grounded)
        //    transform.position += slide_vector * (Time.deltaTime * slide_speed);
        
        //if (is_grounded)
        //{
        //    var slide_hited = Physics.CapsuleCast(
        //        transform.position - Vector3.up * (col.height - col.radius * 2) * 0.5f - slide_vector * 0.02f,
        //        transform.position + Vector3.up * (col.height - col.radius * 2) * 0.5f,
        //        col.radius,
        //        Vector3.down,
        //        out var hit_slide);
        //    
        //    transform.position += slide_vector * (!slide_hited ? Time.deltaTime * slide_speed : Mathf.Min(hit_slide.distance+0.01f, Time.deltaTime * slide_speed));
        //}

        if (move_vec.sqrMagnitude > 0.01f && Physics.CapsuleCast(
                transform.position - Vector3.up * (col.height - col.radius * 2) * 0.5f - move_vec * 0.02f, 
                transform.position +  Vector3.up * (col.height - col.radius * 2) * 0.5f - move_vec * 0.02f, 
                col.radius-0.0f, 
                move_vec, 
                out var hit))
        {
            //Debug.DrawRay(hit.point, hit.normal, Color.red);
            //Debug.DrawRay(transform.position, move_vec, Color.red);
        
            //var v_right = Vector3.Cross(Vector3.up, move_vec).normalized;
            //
            //var v = Vector3.ProjectOnPlane(hit.normal, v_right).normalized;
            //
            
            //Debug.Log(Vector3.Dot(Vector3.up, hit.normal));
            
            if (hit.distance < (Time.deltaTime * moveSpeed + 0.05f) && Vector3.Dot(Vector3.up, hit.normal) < climb_angle_dot /* && ADD STEP OVER CHECK */)
            {
                var move_vec_right = Vector3.Cross(Vector3.up, move_vec).normalized;
                obstacle_normal = hit.normal;
                var obstacle_move_vec = Vector3.Cross(obstacle_normal, surface_normal).normalized * Mathf.Sign(Vector3.Dot(obstacle_normal, move_vec_right));
                
                move_vec = obstacle_move_vec;
                Debug.DrawRay(transform.position, obstacle_move_vec, Color.yellow);
            }
            //    move_vec = Vector3.ProjectOnPlane(Vector3.forward, v).normalized;
            
            //Debug.DrawRay(hit.point, Vector3.ProjectOnPlane(Vector3.forward, v), Color.blue);
        }

        //if(Keyboard.current.upArrowKey.isPressed)
            transform.position += move_vec * Time.deltaTime * moveSpeed;

            //if(Keyboard.current.downArrowKey.isPressed)
        //    transform.position -= move_vec * Time.deltaTime * 3;


        //if (Physics.CapsuleCast(transform.position - Vector3.up * (col.height - col.radius * 2) * 0.5f, transform.position + Vector3.up * (col.height - col.radius * 2) * 0.5f, col.radius, Vector3.down, out var hit_slide))
        //{
        //    Debug.DrawRay(hit_slide.point, hit_slide.normal, Color.red);
        //    
        //    Debug.DrawRay(hit_slide.point, Vector3.ProjectOnPlane(Vector3.down, hit_slide.normal).normalized, Color.blue);
        //}
    }
}
