using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    
    public float gravity = 9.8f;

    public float terminalVelocityFall;
    public float maxClimbAngle;
    public float maxStepHeight;
    public float minStepDepth;
    public float slideDeceleration;
    public float moveCastBackStepDistance;
    public float obstacleSeparationDistance;

    public float moveSpeed;
    
    CapsuleCollider col;

    float fall_speed;
    float slide_speed;

    float climb_angle_dot;

    Vector3 slide_vector;

    Transform transform_local;
    float collider_radius;
    float collider_radius_x2;
    float collider_height;
    
    Vector3 surface_normal;
    Vector3 move_vec;
    
    bool is_grounded;

    bool is_climbing;
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
        move_vec = Vector3.zero;

        if(Keyboard.current.upArrowKey.isPressed)
            move_vec = Vector3.forward;
        
        if(Keyboard.current.downArrowKey.isPressed)
            move_vec = Vector3.back;
        
        surface_normal = Vector3.up;
        //var obstacle_normal = Vector3.up;

        handle_gravity();

        is_climbing = false;
        
        //MOVEMENT
        //--------------------------------------------------------------------------------------------------------------

        if(move_vec.sqrMagnitude< 0.01f) return;

        var move_dist_left = Time.deltaTime * moveSpeed;
        var current_move_vec = move_vec;
        var current_surface_normal = surface_normal;
        var is_avoiding_obstacle = false;
        var iterations = 0;
        var total_moved = 0f;
        var move_dot_mult = 1f;
        while (move_dist_left>Mathf.Epsilon && iterations<5)
        {
            if (is_avoiding_obstacle)
                iterations += 1;
            else
            {
                total_moved = 0;
                iterations = 0;
            }

            var can_move = check_movement(is_avoiding_obstacle, move_vec, current_move_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal, out var will_avoid_obstacle, out var obstacle_dot);
            
            if(!can_move) break;

            if(iterations == 2 && total_moved < Mathf.Epsilon) break;
            
            Debug.Log("DOT: " + obstacle_dot + "   MOVE: " + move_dist);
            
            transform_local.position += move_dist * current_move_vec * move_dot_mult;
            move_dot_mult = obstacle_dot;
            current_surface_normal = new_surface_normal;
            current_move_vec = new_move_vec;
            move_dist_left -= move_dist;
            is_avoiding_obstacle = will_avoid_obstacle;
            
            if (is_avoiding_obstacle)
                total_moved += move_dist;
        }
        
        //Debug.Log("Iterations: " + iterations);

        //--------------------------------------------------------------------------------------------------------------
    }


    void handle_gravity()
    {
        //GRAVITY
        //--------------------------------------------------------------------------------------------------------------
        
        var ground_height_check_val = collider_height - col.radius * 2;
        
        var ground_check = Physics.SphereCast(
                               transform.position + (collider_height - collider_radius_x2) * 0.5f * Vector3.up,
                               collider_radius,
                               Vector3.down,
                               out var hit_ground) 
                           && hit_ground.distance <= ground_height_check_val + obstacleSeparationDistance + 0.05f;
        
        
        
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
            if (is_climbing || Vector3.Dot(Vector3.up, hit_ground.normal) >= climb_angle_dot)
            {
                if (!is_climbing && slide_speed > 0.1f)
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
            
            if(hit_ground.distance < ground_height_check_val + obstacleSeparationDistance)
                transform.position += (hit_ground.distance - ground_height_check_val - obstacleSeparationDistance) * Vector3.down;
        }
        //In the air
        else
        {
            
            Debug.Log("AIR");
            
            if (is_grounded)
            {
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
    }
    
    
    bool check_movement(bool is_obstacle_avoidance, Vector3 initial_move_vec, Vector3 current_move_vec, Vector3 surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal, out bool will_avoid_obstacle, out float obstacle_dot)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = surface_normal;
        will_avoid_obstacle = false;
        obstacle_dot = 1;

        var current_position = transform_local.position;
        
        var move_hit_check = Physics.CapsuleCast(
            current_position - (collider_height - collider_radius_x2) * 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec,
            current_position + (collider_height - collider_radius_x2) * 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec,
            collider_radius,
            current_move_vec,
            out var hit_move);

        var hit_dist = hit_move.distance;
 
        if (!move_hit_check || hit_dist > Time.deltaTime * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance) return true;

        var hit_norm = hit_move.normal;

        var is_obstacle = Vector3.Dot(Vector3.up, hit_norm) < climb_angle_dot && !step_check() ;
        
        var move_vec_right = Vector3.Cross(Vector3.up, current_move_vec).normalized;
        
        if (is_obstacle)
        {
            //Debug.Log("Obstacle  " + current_move_vec);
            //Debug.DrawRay(hit_move.point, Vector3.up, Color.magenta);
            
            var obstacle_move_sign = Mathf.Sign(Vector3.Dot(hit_norm, move_vec_right));
            var obstacle_move_vec = Vector3.Cross(hit_norm, surface_normal).normalized * obstacle_move_sign;
            //var obstacle_move_vec = Vector3.Cross(hit_norm, Vector3.up).normalized * obstacle_move_sign;

            Debug.Log(hit_norm);
            Debug.DrawRay(hit_move.point, hit_norm * 10, Color.magenta);
            
            var dot = Vector3.Dot(obstacle_move_vec, initial_move_vec);
            obstacle_dot = dot;
            
            //Debug.Log("DOT: " + obstacle_dot);
            
            if (dot <= 0) return false;
            
            will_avoid_obstacle = true;

            new_move_vec = obstacle_move_vec;
            
            //if (hit_dist - 0.02f <= 0.01f)
            if (hit_dist <= moveCastBackStepDistance + obstacleSeparationDistance)
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
            
            //Debug.DrawRay(current_position, new_move_vec, Color.red);
        }
        
        move_dist = hit_dist - (moveCastBackStepDistance + obstacleSeparationDistance);


        return true;

        bool step_check()
        {
            var step_hit_check = Physics.CapsuleCast(
                current_position - (collider_height - collider_radius_x2) * 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec + maxStepHeight * Vector3.up,
                current_position + (collider_height - collider_radius_x2) * 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec,
                collider_radius,
                current_move_vec,
                out var hit_step);
            
            
            //Debug.DrawRay(hit_step.point, hit_step.normal * 10, Color.green);
            Debug.DrawRay(hit_step.point, (current_move_vec * (hit_step.distance - moveCastBackStepDistance) + Vector3.up * maxStepHeight).normalized * -10, Color.green);
            //Debug.DrawRay(hit_step.point, (current_move_vec * (hit_step.distance - (moveCastBackStepDistance + obstacleSeparationDistance)) + Vector3.up * maxStepHeight).normalized, Color.green);
            //Debug.Log(Vector3.Dot(Vector3.up,(current_move_vec * (hit_step.distance - (moveCastBackStepDistance + obstacleSeparationDistance)) + Vector3.up * maxStepHeight).normalized));

            //Debug.Log(Vector3.Dot(Vector3.up, hit_step.normal));
            //Debug.Log(hit_step.normal);
            
            if (!step_hit_check
                //|| hit_step.distance >= minStepDepth + moveCastBackStepDistance + obstacleSeparationDistance //Time.deltaTime * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance 
                || Vector3.Dot(Vector3.up, hit_step.normal) >= climb_angle_dot
                || Vector3.Dot(Vector3.up, (current_move_vec * (hit_step.distance - (moveCastBackStepDistance + obstacleSeparationDistance)) + Vector3.up * maxStepHeight).normalized) <= climb_angle_dot
                //|| Vector3.Dot(Vector3.up, (current_move_vec * (hit_step.distance - moveCastBackStepDistance) + Vector3.up * maxStepHeight).normalized) <= climb_angle_dot
                //|| hit_step.distance>=Time.deltaTime * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance && Vector3.Dot(Vector3.up, hit_step.normal) >= climb_angle_dot
                )
            {
                is_climbing = true;
                return true;
            }

            hit_norm = hit_step.normal;
            
            return false;

        }
    }
}
