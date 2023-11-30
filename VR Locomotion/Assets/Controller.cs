using System;
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

    float climb_angle_dot;

    Vector3 slide_vector;
    Vector3 slide_speed_vector;

    Transform transform_local;
    float collider_radius;
    float collider_radius_x2;
    float collider_height;
    
    Vector3 surface_normal;
    Vector3 move_vec;

    Vector3 vec_up;
    Vector3 vec_down;
    Vector3 vec_zero;
    Vector3 vec_forward;
    Vector3 vec_back;
    Vector3 vec_left;
    Vector3 vec_right;

    float delta_time;
    
    bool is_grounded;

    bool is_climbing;

    bool is_jumping;
    
    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<CapsuleCollider>();
        climb_angle_dot = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        transform_local = transform;
        collider_radius = col.radius;
        collider_radius_x2 = collider_radius * 2;
        collider_height = col.height;
        vec_up = Vector3.up;
        vec_down = Vector3.down;
        vec_zero = Vector3.zero;
        vec_forward = Vector3.forward;
        vec_back = Vector3.back;
        vec_left = Vector3.left;
        vec_right = Vector3.right;
        Destroy(col);
        
    }

    // Update is called once per frame
    void Update()
    {
        delta_time = Time.deltaTime;
        
        //Debug.DrawRay(Vector3.zero + Vector3.forward*2, Vector3.ProjectOnPlane(new Vector3(1,2,1), vec_right), Color.green);
        //Debug.DrawRay(Vector3.zero + Vector3.forward*-2, Vector3.ProjectOnPlane(new Vector3(1,2,1).normalized, vec_right), Color.blue);
        
        move_vec = vec_zero;

        if(Keyboard.current.upArrowKey.isPressed)
            move_vec += vec_forward;
        
        if(Keyboard.current.downArrowKey.isPressed)
            move_vec += vec_back;
        
        if(Keyboard.current.leftArrowKey.isPressed)
            move_vec += vec_left;
        
        if(Keyboard.current.rightArrowKey.isPressed)
            move_vec += vec_right;
        
        if(move_vec.sqrMagnitude > 0.01f)
            move_vec.Normalize();
        
        surface_normal = vec_up;

        if (is_grounded && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            //is_grounded = false;
            is_jumping = true;
            fall_speed = -15f;
        }
        
        handle_gravity();

        handle_slide();
        
        //is_climbing = false;

        handle_movement();
    }


    void handle_gravity()
    {
        //GRAVITY
        //--------------------------------------------------------------------------------------------------------------
        
        var ground_height_check_val = collider_height - collider_radius_x2;

        var ground_collision_check = Physics.SphereCast(
            transform.position + (collider_height - collider_radius_x2) * 0.5f * vec_up,
            collider_radius,
            vec_down,
            out var hit_ground);
        
        var ground_check = ground_collision_check && hit_ground.distance <= ground_height_check_val + obstacleSeparationDistance + 0.10f;
        
        //On the ground
        if (ground_check && !is_jumping)
        {
            slide_vector = Vector3.ProjectOnPlane(vec_down, hit_ground.normal).normalized;
            
            if (!is_grounded)
            {
                //slide_speed = Mathf.Max(0,fall_speed);

                transform_local.position += vec_down * (hit_ground.distance - (ground_height_check_val + obstacleSeparationDistance));
                
                slide_speed_vector = Vector3.Project(vec_down * fall_speed, slide_vector);

                fall_speed = delta_time * gravity;
                is_grounded = true;
            }

            var up_ground_norm_dot = Vector3.Dot(vec_up, hit_ground.normal);
            
            //stand
            //if (is_climbing || up_ground_norm_dot >= climb_angle_dot)
            if (up_ground_norm_dot >= climb_angle_dot)
            {
                if (slide_speed_vector.sqrMagnitude > 0.01f)
                {
                    slide_speed_vector = Vector3.Lerp(slide_speed_vector, vec_zero, delta_time * slideDeceleration);// / Mathf.Exp(slide_speed_vector.magnitude/50));
                    //slide_speed_vector -= Mathf.Max(0, Mathf.Min(slide_speed_vector.magnitude, delta_time * slideDeceleration / (slide_speed_vector.magnitude/20) )) * slide_speed_vector.normalized;
                    //slide_speed_vector -= Mathf.Max(0, Mathf.Min(slide_speed_vector.magnitude, delta_time * slideDeceleration / Mathf.Exp(slide_speed_vector.magnitude/5) )) * slide_speed_vector.normalized;
                    
                    //var new_slide_speed_vector = slide_speed_vector + delta_time * gravity * slide_vector;
                    //
                    //if (new_slide_speed_vector.sqrMagnitude <= slide_speed_vector.sqrMagnitude)
                    //    slide_speed_vector = new_slide_speed_vector;
                    //else
                    //    slide_speed_vector -= Mathf.Min(slide_speed_vector.magnitude, delta_time * gravity) * slide_speed_vector.normalized;// / slide_speed_vector.magnitude;
                }
                else
                    slide_speed_vector = vec_zero;
                
                surface_normal = hit_ground.normal;
            }
            //slide
            else
            {
                //Debug.Log("SLIDE");

                var slide_terminal_velocity = terminalVelocityFall;// * (1f-up_ground_norm_dot);
                
                if (slide_speed_vector.sqrMagnitude < slide_terminal_velocity*slide_terminal_velocity)
                {
                    slide_speed_vector += delta_time * gravity * slide_vector;
                }
                else
                {
                    //slide_speed_vector -= delta_time * slideDeceleration * up_ground_norm_dot * slide_vector;
                    slide_speed_vector -= Mathf.Min(slide_speed_vector.magnitude, delta_time * slideDeceleration) * slide_speed_vector;
                }
                
                surface_normal = hit_ground.normal;
            }
            
            //Debug.DrawRay(transform_local.position, surface_normal*10, Color.blue);

            if (!(move_vec.sqrMagnitude > 0.01f)) return;
            
            var v_right = Vector3.Cross(vec_up, move_vec);//.normalized;//
            
            var v = Vector3.ProjectOnPlane(surface_normal, v_right);//.normalized;//
            
            move_vec = Vector3.ProjectOnPlane(move_vec, v).normalized;
            
            
        }
        //In the air
        else
        {
            
            //Debug.Log("AIR");
            
            if (is_grounded)
            {
                //fall_speed = -Math.Min(0,slide_speed_vector.y);
                is_grounded = false;
            }

            if (!ground_check && is_jumping)
                is_jumping = false;

            if (fall_speed < 0)
            {
                var head_collision_check = Physics.SphereCast(
                    transform.position + (collider_height - collider_radius_x2) * 0.5f * vec_down,
                    collider_radius,
                    vec_up,
                    out var hit_jump);
                
                var jump_check = head_collision_check && hit_jump.distance <= ground_height_check_val + obstacleSeparationDistance + 0.01f;

                if (jump_check)
                    fall_speed = 0;
            }
            
            
            if(fall_speed < terminalVelocityFall)
                fall_speed += delta_time * gravity;

            transform_local.position += (!ground_collision_check ? delta_time * fall_speed :  Mathf.Min(hit_ground.distance-ground_height_check_val - obstacleSeparationDistance, delta_time * fall_speed)) * vec_down;
            //Debug.DrawRay(transform_local.position, slide_vector*10, Color.red);
            slide_speed_vector = Vector3.Lerp(slide_speed_vector, vec_zero, delta_time * 1f);
            if (slide_speed_vector.sqrMagnitude < 0.01f)
                slide_speed_vector = vec_zero;

        }
        
        
        
        //--------------------------------------------------------------------------------------------------------------
    }


    void handle_slide()
    {
        if(slide_speed_vector.sqrMagnitude < 0.01f) return;

        var slide_magnitude = slide_speed_vector.magnitude;
        var move_dist_left = delta_time * slide_magnitude;
        var current_move_vec = slide_speed_vector.normalized;
        var current_surface_normal = surface_normal;
        var is_avoiding_obstacle = false;
        var iterations = 0;
        var avoid_iterations = 0;
        var total_moved = 0f;
        var move_dot_mult = 1f;
        
        while (move_dist_left>Mathf.Epsilon && iterations<5)
        {
            iterations += 1;
            
            if (is_avoiding_obstacle)
                avoid_iterations += 1;
            else
            {
                total_moved = 0;
                avoid_iterations = 0;
            }

            var can_move = check_movement2(move_vec, current_move_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal, out var will_avoid_obstacle, out var obstacle_dot);
            
            if(!can_move)
            {
                slide_speed_vector = slide_speed_vector.normalized * 100; 
                break;
            }

            if (avoid_iterations == 2 && total_moved < Mathf.Epsilon)
            {
                slide_speed_vector = vec_zero;
                break;
            }

            transform_local.position += move_dist * move_dot_mult * current_move_vec;
            move_dot_mult = obstacle_dot;
            current_surface_normal = new_surface_normal;
            current_move_vec = new_move_vec;
            move_dist_left -= move_dist;
            is_avoiding_obstacle = will_avoid_obstacle;
            
            if (is_avoiding_obstacle)
                total_moved += move_dist;
        }

        slide_speed_vector = current_move_vec * slide_magnitude;
        //slide_speed_vector = Vector3.Project(slide_speed_vector,current_move_vec);
    }

    void handle_movement()
    {
        //MOVEMENT
        //--------------------------------------------------------------------------------------------------------------

        if(move_vec.sqrMagnitude< 0.01f) return;

        //Debug.DrawRay(transform_local.position, move_vec*2, Color.green);
        
        var move_dist_left = delta_time * moveSpeed;
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

            var can_move = check_movement(move_vec, current_move_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal, out var will_avoid_obstacle, out var obstacle_dot);
            
            if(!can_move) break;

            if(iterations == 2 && total_moved < Mathf.Epsilon) break;

            transform_local.position += move_dist * move_dot_mult * current_move_vec;
            move_dot_mult = obstacle_dot;
            current_surface_normal = new_surface_normal;
            current_move_vec = new_move_vec;
            move_dist_left -= move_dist;
            is_avoiding_obstacle = will_avoid_obstacle;
            
            if (is_avoiding_obstacle)
                total_moved += move_dist;
        }

        //--------------------------------------------------------------------------------------------------------------
    }

    bool check_movement(Vector3 initial_move_vec, Vector3 current_move_vec, Vector3 surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal, out bool will_avoid_obstacle, out float obstacle_dot)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = surface_normal;
        will_avoid_obstacle = false;
        obstacle_dot = 1;

        var current_position = transform_local.position;
        var capsule_points_offset1 = -(collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_move_vec;
        var capsule_points_offset2 = (collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_move_vec;
        
        var move_hit_check = Physics.CapsuleCast(
            //current_position - 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec,
            //current_position + 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec,
            current_position + capsule_points_offset1,
            current_position + capsule_points_offset2,
            collider_radius,
            current_move_vec,
            out var hit_move);

        var hit_dist = hit_move.distance;
 
        //if (!move_hit_check || hit_dist > delta_time * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance) return true;
        if (!move_hit_check || hit_dist > left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance) return true;

        var hit_norm = hit_move.normal;

        var is_obstacle = Vector3.Dot(vec_up, hit_norm) < climb_angle_dot && !step_check() ;
        
        var move_vec_right = Vector3.Cross(vec_up, current_move_vec);//.normalized;//
        
        //DebugExtension.DebugCapsule(
        //    current_position - Vector3.up - moveCastBackStepDistance * current_move_vec + 0.15f * current_move_vec,
        //    current_position + Vector3.up - moveCastBackStepDistance * current_move_vec + 0.15f * current_move_vec,
        //    Color.blue,
        //    collider_radius);
        
        if (is_obstacle)
        {
            var obstacle_move_sign = Mathf.Sign(Vector3.Dot(hit_norm, move_vec_right));
            var obstacle_move_vec = obstacle_move_sign * Vector3.Cross(hit_norm, surface_normal).normalized;

            //Debug.Log(hit_norm);
            //Debug.DrawRay(hit_move.point, hit_norm * 10, Color.magenta);
            
            var dot = Vector3.Dot(obstacle_move_vec, initial_move_vec);
            obstacle_dot = dot;

            if (dot <= 0) return false;
            
            will_avoid_obstacle = true;

            new_move_vec = obstacle_move_vec;
            
            if (hit_dist <= moveCastBackStepDistance + obstacleSeparationDistance)
            {
                move_dist = 0;
                return true;
            }
        }
        else
        {
            new_surface_normal = hit_norm;

            var v = Vector3.ProjectOnPlane(new_surface_normal, move_vec_right);//.normalized;//

            new_move_vec = Vector3.ProjectOnPlane(current_move_vec, v).normalized;

            //Debug.DrawRay(hit_move.point, Vector3.up, Color.red);
        }
        
        move_dist = hit_dist - (moveCastBackStepDistance + obstacleSeparationDistance);


        return true;

        bool step_check()
        {
            var step_hit_check = Physics.CapsuleCast(
                current_position + capsule_points_offset1 + maxStepHeight * vec_up,
                current_position + capsule_points_offset2,
                collider_radius,
                current_move_vec,
                out var hit_step);

            
            if (!step_hit_check
                //|| hit_step.distance >= minStepDepth + moveCastBackStepDistance + obstacleSeparationDistance //delta_time * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance 
                || Vector3.Dot(vec_up, hit_step.normal) >= climb_angle_dot
                || Vector3.Dot(vec_up, ((hit_step.distance - (moveCastBackStepDistance + obstacleSeparationDistance)) * current_move_vec + maxStepHeight * vec_up).normalized) <= climb_angle_dot
                //&& Vector3.Dot(vec_up, (hit_step.point-hit_move.point).normalized) <= climb_angle_dot
                //|| Vector3.Dot(vec_up, (current_move_vec * (hit_step.distance - moveCastBackStepDistance) + vec_up * maxStepHeight).normalized) <= climb_angle_dot
                //|| hit_step.distance>=delta_time * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance && Vector3.Dot(vec_up, hit_step.normal) >= climb_angle_dot
               )
            {
                //is_climbing = true;
                return true;
            }

            hit_norm = hit_step.normal;
            
            return false;

        }
    }
    
    bool check_movement2(Vector3 initial_move_vec, Vector3 current_move_vec, Vector3 surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal, out bool will_avoid_obstacle, out float obstacle_dot)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = surface_normal;
        will_avoid_obstacle = false;
        obstacle_dot = 1;

        var current_position = transform_local.position;
        var capsule_points_offset1 = -(collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_move_vec;
        var capsule_points_offset2 = (collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_move_vec;
        
        var move_hit_check = Physics.CapsuleCast(
            //current_position - 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec,
            //current_position + 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec,
            current_position + capsule_points_offset1,
            current_position + capsule_points_offset2,
            collider_radius,
            current_move_vec,
            out var hit_move);

        var hit_dist = hit_move.distance;

        //if (left_move_dist > 0.38f)
        //{
        //    
        //}
        
        //Debug.Log(hit_dist +"   " + delta_time * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance);
        
        //if (!move_hit_check || hit_dist > delta_time * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance) return true;
        if (!move_hit_check || hit_dist > left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance) return true;

        var hit_norm = hit_move.normal;

        //var is_obstacle = Vector3.Dot(vec_up, hit_norm) < climb_angle_dot;// && !step_check() ;
        
        var move_vec_right = Vector3.Cross(vec_up, current_move_vec);//.normalized;//
        
        //DebugExtension.DebugCapsule(
        //    current_position - Vector3.up - moveCastBackStepDistance * current_move_vec + 0.15f * current_move_vec,
        //    current_position + Vector3.up - moveCastBackStepDistance * current_move_vec + 0.15f * current_move_vec,
        //    Color.blue,
        //    collider_radius);
        
        //if (is_obstacle)
        //{
        //    var obstacle_move_sign = Mathf.Sign(Vector3.Dot(hit_norm, move_vec_right));
        //    var obstacle_move_vec = obstacle_move_sign * Vector3.Cross(hit_norm, surface_normal).normalized;
        //
        //    //Debug.Log(hit_norm);
        //    //Debug.DrawRay(hit_move.point, hit_norm * 10, Color.magenta);
        //    
        //    var dot = Vector3.Dot(obstacle_move_vec, initial_move_vec);
        //    obstacle_dot = dot;
        //
        //    if (dot <= 0) return false;
        //    
        //    will_avoid_obstacle = true;
        //
        //    new_move_vec = obstacle_move_vec;
        //    
        //    if (hit_dist <= moveCastBackStepDistance + obstacleSeparationDistance)
        //    {
        //        move_dist = 0;
        //        return true;
        //    }
        //}
        //else
        {
            //Debug.Log("HIT");
            new_surface_normal = hit_norm;

            var v = Vector3.ProjectOnPlane(new_surface_normal, move_vec_right);//.normalized;//

            new_move_vec = Vector3.ProjectOnPlane(current_move_vec, v).normalized;

            //Debug.DrawRay(hit_move.point, Vector3.up, Color.red);
            //Debug.DrawRay(hit_move.point, new_move_vec, Color.red);
        }
        
        move_dist = Mathf.Max(0, hit_dist - (moveCastBackStepDistance + obstacleSeparationDistance));


        return true;
        //
        //bool step_check()
        //{
        //    var step_hit_check = Physics.CapsuleCast(
        //        current_position + capsule_points_offset1 + maxStepHeight * vec_up,
        //        current_position + capsule_points_offset2,
        //        collider_radius,
        //        current_move_vec,
        //        out var hit_step);
        //
        //    
        //    if (!step_hit_check
        //        //|| hit_step.distance >= minStepDepth + moveCastBackStepDistance + obstacleSeparationDistance //delta_time * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance 
        //        || Vector3.Dot(vec_up, hit_step.normal) >= climb_angle_dot
        //        || Vector3.Dot(vec_up, ((hit_step.distance - (moveCastBackStepDistance + obstacleSeparationDistance)) * current_move_vec + maxStepHeight * vec_up).normalized) <= climb_angle_dot
        //        //&& Vector3.Dot(vec_up, (hit_step.point-hit_move.point).normalized) <= climb_angle_dot
        //        //|| Vector3.Dot(vec_up, (current_move_vec * (hit_step.distance - moveCastBackStepDistance) + vec_up * maxStepHeight).normalized) <= climb_angle_dot
        //        //|| hit_step.distance>=delta_time * left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance && Vector3.Dot(vec_up, hit_step.normal) >= climb_angle_dot
        //       )
        //    {
        //        //is_climbing = true;
        //        return true;
        //    }
        //
        //    hit_norm = hit_step.normal;
        //    
        //    return false;
        //
        //}
    }
}
