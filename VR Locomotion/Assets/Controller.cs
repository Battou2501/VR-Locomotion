using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class Controller : MonoBehaviour
{
    
    public float gravity = 9.8f;

    public LayerMask raycastMask;
    public float colliderRadius;
    public float colliderHeight;
    public float colliderMinHeight;
    public float terminalVelocityFall;
    public float terminalVelocitySlide;
    public float maxClimbAngle;
    public float maxStepHeight;
    public float minStepDepth;
    public float slideDeceleration;
    public float moveCastBackStepDistance;
    public float obstacleSeparationDistance;

    public float moveSpeed;
    public float jumpStrength;
    
    //CapsuleCollider col;

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

    float ground_height_check_val;
    
    float delta_time;
    
    bool is_grounded;

    bool is_climbing;

    bool is_jumping;
    
    // Start is called before the first frame update
    void Start()
    {
        //col = GetComponent<CapsuleCollider>();
        climb_angle_dot = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        transform_local = transform;
        //collider_radius = col.radius;
        collider_radius = colliderRadius;
        collider_radius_x2 = collider_radius * 2;
        //collider_height = col.height;
        collider_height = colliderHeight;
        vec_up = Vector3.up;
        vec_down = Vector3.down;
        vec_zero = Vector3.zero;
        vec_forward = Vector3.forward;
        vec_back = Vector3.back;
        vec_left = Vector3.left;
        vec_right = Vector3.right;
        //Destroy(col);

        var s = Stopwatch.StartNew();
        
        for (int i = 0; i < 1000000; i++)
        {
            var r = Random.onUnitSphere;

            //var c = Physics.Raycast(vec_zero, r, out var hit);
            //var c = Physics.SphereCast(vec_zero, 0.05f, r, out var hit);
            var c = Physics.CapsuleCast(vec_zero, vec_up, 0.05f, r, out var hit, raycastMask);
        }
        s.Stop();
        
        Debug.Log(s.ElapsedMilliseconds);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Physics.SyncTransforms();
        
        delta_time = Time.fixedDeltaTime;
        move_vec = vec_zero;
        
        ground_height_check_val = collider_height - collider_radius_x2;
        
        if(Keyboard.current.upArrowKey.isPressed)
            move_vec += vec_forward;
        
        if(Keyboard.current.downArrowKey.isPressed)
            move_vec += vec_back;
        
        if(Keyboard.current.leftArrowKey.isPressed)
            move_vec += vec_left;
        
        if(Keyboard.current.rightArrowKey.isPressed)
            move_vec += vec_right;
        
        if(Keyboard.current.eKey.isPressed)
            move_vec += vec_right+vec_forward;
        
        if(move_vec.sqrMagnitude > 0.01f)
            move_vec.Normalize();
        
        
        //handle_jump();

        handle_movement();
        
        handle_slide();
        
        handle_gravity();

        
        Physics.SyncTransforms();
    }

    void handle_jump()
    {
        if (!is_grounded || !Keyboard.current.spaceKey.wasPressedThisFrame) return;
        
        var head_collision_check = Physics.SphereCast(
            transform.position + (collider_height - collider_radius_x2) * 0.5f * vec_down,
            collider_radius,
            vec_up,
            out var hit_jump,
            raycastMask);
                
        var jump_check = head_collision_check && hit_jump.distance <= ground_height_check_val + obstacleSeparationDistance + 0.05f;

        if (jump_check) return;
        
        is_jumping = true;
        fall_speed = -jumpStrength;
    }
    

    void handle_gravity()
    {
        //GRAVITY
        //--------------------------------------------------------------------------------------------------------------

        surface_normal = vec_up;
        
        var ground_collision_check = Physics.SphereCast(
            transform.position + (collider_height - collider_radius_x2) * 0.5f * vec_up,
            collider_radius,// + 0.02f,
            vec_down,
            out var hit_ground,
            raycastMask);

        var ground_check = ground_collision_check && hit_ground.distance <= ground_height_check_val + obstacleSeparationDistance + 0.001f;// - 0.02f;// || is_climbing;

        //On the ground
        if (ground_check && !is_jumping)
        {
            slide_vector = Vector3.ProjectOnPlane(vec_down, hit_ground.normal).normalized;

            //if (!is_climbing)
            //{
                //transform_local.position = Vector3.Lerp(transform_local.position, transform_local.position + vec_down * (hit_ground.distance - (ground_height_check_val + obstacleSeparationDistance)), 5f * delta_time);
            //}

            
            if (hit_ground.distance <= ground_height_check_val + obstacleSeparationDistance + 0.0f)
            {
                transform_local.position += vec_down * (hit_ground.distance - (ground_height_check_val + obstacleSeparationDistance + 0.0f));
                //transform_local.position = Vector3.Lerp(transform_local.position, transform_local.position + vec_down * (hit_ground.distance - (ground_height_check_val + obstacleSeparationDistance)), Mathf.Min(1f,20f * delta_time));
            }
            
            if (!is_grounded)
            {
                //if(hit_ground.distance <= ground_height_check_val + obstacleSeparationDistance)
                //    transform_local.position += vec_down * (hit_ground.distance - (ground_height_check_val + obstacleSeparationDistance));
                
                slide_speed_vector = Vector3.Project(vec_down * fall_speed, slide_vector);

                fall_speed = 0;//delta_time * gravity;
                is_grounded = true;
            }

            var up_ground_norm_dot = Vector3.Dot(vec_up, hit_ground.normal);
            
            //stand
            if (up_ground_norm_dot >= climb_angle_dot || is_climbing)
            {
                //fall_speed = 0;
                if (slide_speed_vector.sqrMagnitude > 0.01f)
                {
                    slide_speed_vector = Vector3.Lerp(slide_speed_vector, vec_zero, delta_time * slideDeceleration);// / Mathf.Exp(slide_speed_vector.magnitude/50));
                    //slide_speed_vector -= Mathf.Max(0, Mathf.Min(slide_speed_vector.magnitude, delta_time * slideDeceleration / (slide_speed_vector.magnitude/20) )) * slide_speed_vector.normalized;
                    //slide_speed_vector -= Mathf.Max(0, Mathf.Min(slide_speed_vector.magnitude, delta_time * slideDeceleration / Mathf.Exp(slide_speed_vector.magnitude/5) )) * slide_speed_vector.normalized;
                }
                else
                    slide_speed_vector = vec_zero;
                
                surface_normal = hit_ground.normal;
            }
            //slide
            else
            {
                //var slide_terminal_velocity = terminalVelocityFall;// * (1f-up_ground_norm_dot);
                
                if (!is_climbing && slide_speed_vector.sqrMagnitude < terminalVelocitySlide*terminalVelocitySlide)
                {
                    slide_speed_vector += Mathf.Max(0,(up_ground_norm_dot-climb_angle_dot)/(1f - climb_angle_dot)) * delta_time * gravity * slide_vector;
                }
                else if (!is_climbing)
                {
                    //slide_speed_vector -= delta_time * slideDeceleration * up_ground_norm_dot * slide_vector;
                    slide_speed_vector -= Mathf.Min(slide_speed_vector.magnitude, delta_time * slideDeceleration) * slide_speed_vector;
                }
                else
                {
                    slide_speed_vector = vec_zero;
                }
                
                surface_normal = hit_ground.normal;
            }
        }
        //In the air
        else
        {
            //Debug.Log("AIR   "+ fall_speed);
            
            if (is_climbing)
            {
                fall_speed = 0;
                //fall_speed = gravity;
                return;
            }

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
                    out var hit_jump,
                    raycastMask);
                
                var jump_check = head_collision_check && hit_jump.distance <= ground_height_check_val + obstacleSeparationDistance + 0.01f;

                if (jump_check)
                {
                    fall_speed = 0;
                    is_jumping = false;
                }
            }

            var touch_ground = hit_ground.distance - ground_height_check_val - obstacleSeparationDistance < delta_time * fall_speed;
            
            if(!touch_ground && fall_speed < terminalVelocityFall)
                fall_speed += delta_time * gravity;

            transform_local.position += (!ground_collision_check ? delta_time * fall_speed :  Mathf.Min(hit_ground.distance-ground_height_check_val - obstacleSeparationDistance, delta_time * fall_speed)) * vec_down;

            //if (touch_ground)
            //    fall_speed = 0;
            
            slide_speed_vector = Vector3.Lerp(slide_speed_vector, vec_zero, delta_time * 1f);
            if (slide_speed_vector.sqrMagnitude < 0.01f)
                slide_speed_vector = vec_zero;

        }
        
        
        
        //--------------------------------------------------------------------------------------------------------------
    }
    
    void handle_slide()
    {
        if (is_climbing)
        {
            slide_speed_vector = vec_zero;
        }
        
        if(is_climbing || slide_speed_vector.sqrMagnitude < 0.01f) return;

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

        is_climbing = false;
        
        var v_right = Vector3.Cross(vec_up, move_vec);//.normalized;//
            
        var v = Vector3.ProjectOnPlane(surface_normal, v_right);//.normalized;//
            
        move_vec = Vector3.ProjectOnPlane(move_vec, v).normalized;
        
        var move_dist_left = delta_time * moveSpeed;
        var current_move_vec = move_vec;
        var current_surface_normal = surface_normal;
        var is_avoiding_obstacle = false;
        var iterations = 0;
        var iterations_obstacle = 0;
        var total_moved = 0f;
        var move_dot_mult = 1f;
        
        while (move_dist_left>Mathf.Epsilon && iterations<5)
        {
            iterations += 1;
            
            if (is_avoiding_obstacle)
                iterations_obstacle += 1;
            else
            {
                total_moved = 0;
                iterations_obstacle = 0;
            }

            var can_move = check_movement(iterations,move_vec, current_move_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal, out var will_avoid_obstacle, out var obstacle_dot);
            
            
            transform_local.position += move_dist * move_dot_mult * current_move_vec;
            
            if(!can_move) break;
            
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

    bool check_movement(int iter_num, Vector3 initial_move_vec, Vector3 current_move_vec, Vector3 surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal, out bool will_avoid_obstacle, out float obstacle_dot)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = surface_normal;
        will_avoid_obstacle = false;
        obstacle_dot = 1;
        
        var current_move_vec_flat = new Vector3(current_move_vec.x, 0, current_move_vec.z).normalized;

        var up_surface_dot = Vector3.Dot(vec_up, surface_normal);
        
        var current_iter_move_vec = iter_num > 1 || up_surface_dot < 0? current_move_vec : current_move_vec_flat;
        
        var current_position = transform_local.position;
        var capsule_points_offset1 = -(collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_iter_move_vec;
        var capsule_points_offset2 = (collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_iter_move_vec;

        var move_hit_check = Physics.CapsuleCast(
            current_position + capsule_points_offset1,
            current_position + capsule_points_offset2,
            collider_radius,
            current_iter_move_vec,
            out var hit_move,
            raycastMask);

        var move_ceiling_check = Physics.SphereCast(current_position + (collider_height - collider_radius_x2) * 0.5f * vec_up + moveCastBackStepDistance * vec_down + current_move_vec_flat * move_dist, collider_radius, vec_up, out var hit_ceiling);

        var ceiling_check = Vector3.Dot(vec_up, current_move_vec)<=0 || (!move_ceiling_check || hit_ceiling.distance > moveCastBackStepDistance + obstacleSeparationDistance * 2);
        var move_check = (!move_hit_check || hit_move.distance > left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance);
        
        var hit_dist = hit_move.distance;
        
        if (ceiling_check && move_check) return true;

        var hit_norm = !move_check ? hit_move.normal : surface_normal;
        hit_norm = !ceiling_check ? hit_ceiling.normal : hit_norm;

        hit_dist = !ceiling_check ? 0 : hit_dist;
        
        var move_vec_right = Vector3.Cross(vec_up, current_move_vec);//.normalized;//

        var move_hit_dot = Vector3.Dot(vec_up, hit_norm);
        
        var is_obstacle = move_hit_dot<0 || move_hit_dot < climb_angle_dot && !step_check2();// || head_check ;

        if (is_obstacle)
        {
            var obstacle_move_sign = Mathf.Sign(Vector3.Dot(hit_norm, move_vec_right));
            
            if (hit_norm.y < -0.999f)
            {
                obstacle_move_sign = Mathf.Sign(Vector3.Dot(hit_norm+surface_normal*0.1f, move_vec_right));
            }
            
            var obstacle_move_vec = obstacle_move_sign * Vector3.Cross(hit_norm, surface_normal).normalized;

            var dot = Vector3.Dot(obstacle_move_vec, initial_move_vec);
            obstacle_dot = dot;

            if (dot <= 0)
            {
                move_dist = !ceiling_check ? 0 : hit_move.distance - moveCastBackStepDistance - obstacleSeparationDistance;
                return false;
            }
            
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
            if (hit_norm.y < 0.0001f)
                new_move_vec = vec_up;
            else
            {
                new_surface_normal = hit_norm;

                var v = Vector3.ProjectOnPlane(new_surface_normal, move_vec_right); //.normalized;//

                new_move_vec = Vector3.ProjectOnPlane(current_move_vec, v).normalized;
            }
        }
        
        move_dist = Mathf.Max(0,hit_dist - (moveCastBackStepDistance + obstacleSeparationDistance));
        
        return true;

        bool step_check2()
        {
            if (!check_step_capsule(out var hit_step_capsule))
            {
                hit_norm = hit_step_capsule.normal;

                return false;
            }

            var check_center = check_step_spheres(vec_zero, out var hit_step_high, out var step_hit_check_high);
            var check_left = check_step_spheres(-move_vec_right*(collider_radius-0.05f + obstacleSeparationDistance), out var hit_step_high_left, out var step_hit_check_high_left);
            var check_right = check_step_spheres(move_vec_right*(collider_radius-0.05f + obstacleSeparationDistance), out var hit_step_high_right, out var step_hit_check_high_right);

            if (check_center && check_left && check_right)
            {
                is_climbing = true;
                return true;
            }

            var count = 0;
            var h_n = vec_zero;

            if (step_hit_check_high)
            {
                count += 1;
                h_n += hit_step_high.normal;
            }

            if (step_hit_check_high_left)
            {
                count += 1;
                h_n += hit_step_high_left.normal;
            }

            if (step_hit_check_high_right)
            {
                count += 1;
                h_n += hit_step_high_right.normal;
            }

            h_n /= count;
            hit_norm = h_n;

            return false;
        }

        bool check_step_capsule( out RaycastHit hit)
        {
            var step_hit_check = Physics.CapsuleCast(
                current_position + capsule_points_offset1 + maxStepHeight * vec_up + vec_up * obstacleSeparationDistance,
                current_position + capsule_points_offset2 - vec_up * obstacleSeparationDistance,
                collider_radius+obstacleSeparationDistance,
                //current_move_vec,
                current_move_vec_flat,
                out var hit_step,
                raycastMask);

            hit = hit_step;
            
            return !step_hit_check || hit_step.distance - obstacleSeparationDistance - moveCastBackStepDistance > minStepDepth;
        }
        
        bool check_step_spheres( Vector3 offset, out RaycastHit hit, out bool is_hit)
        {
            var step_hit_check_low = Physics.SphereCast(
                current_position - vec_up * (collider_height * 0.5f) + vec_up * 0.1f + offset - current_move_vec_flat * moveCastBackStepDistance * 4,
                0.05f,
                //current_move_vec,
                current_move_vec_flat,
                out var hit_step_low,
                raycastMask);

            hit = hit_step_low;
            is_hit = false;
            
            var step_hit_check_high = Physics.SphereCast(
                current_position - vec_up * (collider_height * 0.5f) + vec_up * (maxStepHeight + 0.05f) + offset - current_move_vec_flat * moveCastBackStepDistance * 4,
                0.05f,
                //current_move_vec,
                current_move_vec_flat,
                out var hit_step_high,
                raycastMask);

            hit = hit_step_high;
            is_hit = step_hit_check_high;

            var v1 = !step_hit_check_high;
            var v2 = !step_hit_check_low && hit_step_high.distance - collider_radius - moveCastBackStepDistance * 4 > minStepDepth;
            var v3 = Vector3.Dot(vec_up,(hit_step_high.point - hit_step_low.point).normalized) < climb_angle_dot && hit_step_high.distance - collider_radius - moveCastBackStepDistance * 4 > minStepDepth;
            
            //return !step_hit_check_low || !step_hit_check_high || hit_step_high.distance - collider_radius - moveCastBackStepDistance > minStepDepth;
            return v1 || v2 || v3;
        }
    }
    
    bool check_movement2(Vector3 initial_move_vec, Vector3 current_move_vec, Vector3 surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal, out bool will_avoid_obstacle, out float obstacle_dot)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = surface_normal;
        will_avoid_obstacle = false;
        obstacle_dot = 1;

        //var current_position = transform_local.position;
        //var capsule_points_offset1 = -(collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_move_vec;
        //var capsule_points_offset2 = (collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_move_vec;
        //
        //var move_hit_check = Physics.CapsuleCast(
        //    //current_position - 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec,
        //    //current_position + 0.5f * Vector3.up - moveCastBackStepDistance * current_move_vec,
        //    current_position + capsule_points_offset1,
        //    current_position + capsule_points_offset2,
        //    collider_radius,
        //    current_move_vec,
        //    out var hit_move,
        //    raycastMask);
        
        var current_position = transform_local.position;
        var capsule_points_offset1 = -(collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_move_vec;
        var capsule_points_offset2 = (collider_height - collider_radius_x2) * 0.5f * vec_up - moveCastBackStepDistance * current_move_vec;

        var move_hit_check = Physics.CapsuleCast(
            current_position + capsule_points_offset1,
            current_position + capsule_points_offset2,
            collider_radius,
            current_move_vec,
            out var hit_move,
            raycastMask);
        

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
