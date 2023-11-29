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

    Transform transform_local;
    float collider_radius;
    float collider_radius_x2;
    float collider_height;
    
    bool is_grounded;
    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<CapsuleCollider>();
        climb_angle_dot = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        transform_local = transform;
        collider_radius = col.radius;
        collider_radius_x2 = collider_radius * 2;
        collider_height = col.height;
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
        //var obstacle_normal = Vector3.up;

        var ground_height_check_val = collider_height - col.radius * 2;
        
        
        
        //GRAVITY
        //--------------------------------------------------------------------------------------------------------------
        
        var ground_check = Physics.SphereCast(
                               transform.position + (collider_height - collider_radius_x2) * 0.5f * Vector3.up,
                               collider_radius,
                               Vector3.down,
                               out var hit_ground) 
                           && hit_ground.distance <= ground_height_check_val + 0.01f;
        
        
        
        //On the ground
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
                if (slide_speed > 0.1f)
                {
                    slide_speed -= Time.deltaTime * slideDeceleration;
                    slide_speed = Mathf.Max(0, slide_speed);
                    
                    transform.position += Time.deltaTime * slide_speed * slide_vector;
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
                transform.position += (hit_ground.distance - ground_height_check_val) * Vector3.down;
        }
        //In the air
        else
        {
            if (is_grounded)
            {
                Debug.Log(hit_ground.distance + "    prev: " + prev);
                fall_speed = slide_speed;
                is_grounded = false;
            }

            if(fall_speed < terminalVelocityFall)
                fall_speed += Time.deltaTime * gravity;
            
            transform.position += Time.deltaTime * slide_speed * slide_vector;
            transform.position += (hit_ground.collider == null ? Time.deltaTime * fall_speed :  Mathf.Min(hit_ground.distance-ground_height_check_val, Time.deltaTime * fall_speed)) * Vector3.down;

            slide_speed = Mathf.Max(0, slide_speed * (1f - Time.deltaTime));
        }
        
        //--------------------------------------------------------------------------------------------------------------
        
        prev = hit_ground.distance;
        Debug.DrawRay(transform.position, surface_normal, Color.red);

        //MOVEMENT
        //--------------------------------------------------------------------------------------------------------------

        //if (move_vec.sqrMagnitude > 0.01f && Physics.CapsuleCast(
        //        transform.position - (collider_height - collider_radius_x2) * 0.5f * Vector3.up - 0.02f * move_vec, 
        //        transform.position +  (collider_height - collider_radius_x2) * 0.5f * Vector3.up - 0.02f * move_vec, 
        //        col.radius-0.0f, 
        //        move_vec, 
        //        out var hit))
        //{
        //    if (hit.distance < (Time.deltaTime * moveSpeed + 0.05f) && Vector3.Dot(Vector3.up, hit.normal) < climb_angle_dot /* && ADD STEP OVER CHECK */)
        //    {
        //        var move_vec_right = Vector3.Cross(Vector3.up, move_vec).normalized;
        //        var obstacle_normal = hit.normal;
        //        var obstacle_move_vec = Mathf.Sign(Vector3.Dot(obstacle_normal, move_vec_right)) * Vector3.Cross(obstacle_normal, surface_normal).normalized;
        //        
        //        move_vec = obstacle_move_vec;
        //        Debug.DrawRay(transform.position, obstacle_move_vec, Color.yellow);
        //    }
        //}

        if(move_vec.sqrMagnitude< 0.01f) return;

        var move_dist_left = Time.deltaTime * moveSpeed;
        var current_move_vec = move_vec;
        var current_surface_normal = surface_normal;
        var is_avoiding_obstacle = false;
        var iterations = 0;
        while (move_dist_left>Mathf.Epsilon && iterations<5)
        {
            iterations += 1;

            var can_move = check_movement(is_avoiding_obstacle, move_vec, current_move_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal, out var will_avoid_obstacle);

            if(!can_move) break;

            transform_local.position += move_dist * current_move_vec;
            current_surface_normal = new_surface_normal;
            current_move_vec = new_move_vec;
            move_dist_left -= move_dist;
            is_avoiding_obstacle = will_avoid_obstacle;
        }

        //--------------------------------------------------------------------------------------------------------------
    }

    bool check_movement(bool is_obstacle_avoidance, Vector3 initial_move_vec, Vector3 current_move_vec, Vector3 surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal, out bool will_avoid_obstacle)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = surface_normal;
        will_avoid_obstacle = false;

        var current_position = transform_local.position;
        
        var move_hit_check = Physics.CapsuleCast(
            current_position - (collider_height - collider_radius_x2) * 0.5f * Vector3.up - 0.02f * current_move_vec,
            current_position + (collider_height - collider_radius_x2) * 0.5f * Vector3.up - 0.02f * current_move_vec,
            collider_radius,
            current_move_vec,
            out var hit_move);

        var hit_dist = hit_move.distance;
 
        if (!move_hit_check || hit_dist > Time.deltaTime * left_move_dist + 0.05f) return true;

        //if (!is_first_check && hit_dist - 0.02f <= 0.01f) return false;

        var hit_norm = hit_move.normal;

        var is_obstacle = Vector3.Dot(Vector3.up, hit_norm) < climb_angle_dot /* && !(ADD STEP OVER CHECK) */ ;
        
        var move_vec_right = Vector3.Cross(Vector3.up, current_move_vec).normalized;
        
        if (is_obstacle)
        {
            Debug.Log("Obstacle");
            
            var obstacle_move_sign = Mathf.Sign(Vector3.Dot(hit_norm, move_vec_right));
            var obstacle_move_vec = Vector3.Cross(hit_norm, surface_normal).normalized * obstacle_move_sign;

            var dot = Vector3.Dot(obstacle_move_vec, initial_move_vec);

            if (dot <= 0) return false;
            
            will_avoid_obstacle = true;
            
            if (is_obstacle_avoidance && hit_dist - 0.02f <= 0.01f) return false;
            
            new_move_vec = obstacle_move_vec;
            
            if (!is_obstacle_avoidance && hit_dist - 0.02f <= 0.01f)
            {
                move_dist = 0;
                return true;
            }
        }
        else
        {
            new_surface_normal = hit_norm;

            var v = Vector3.ProjectOnPlane(new_surface_normal, move_vec_right).normalized;

            new_move_vec = Vector3.ProjectOnPlane(current_move_vec, v).normalized;
        }
        
        move_dist = hit_dist - 0.02f;


        return true;
    }
}
