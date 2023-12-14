using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class  Controller4 : MonoBehaviour
{
    public enum UpdateTypes
    {
        every_frame = 0,
        fixed_frame = 1
    }

    public enum ThresholdSpeedActions
    {
        stop = 0,
        subtract = 1
    }
    
    public enum SlidingTrajectoryChangeTypes
    {
        parallel_to_sliding_vector = 0,
        project_sliding_vector_on_surface = 1
    }

    public UpdateTypes updateType;
    
    public float gravity = 9.8f;
    
    [Header("Air resistance while falling or sliding")]
    [Range(0,1)]
    public float airDrag;
    
    [Header("Friction when sliding ground with incline\nlower than Max climb angle")]
    [Range(0,1)]
    public float groundFriction;
    
    [Header("Falling/Sliding speed needed to continue slide\nwhen touching ground with incline lower than Max climb angle")]
    public float groundSlideSpeedThreshold;

    [Header("Stop - stops if fall speed is lower\nSubtract - subtracts threshold speed from fall speed")]
    public ThresholdSpeedActions thresholdSpeedAction;
    
    [Header("Friction when sliding ground with incline\ngreater than Max climb angle")]
    [Range(0,1)]
    public float slideFriction;

    [Header("How to calculate ne trajectory on collision when sliding\nParallel - new vector will only change incline\nProject - will project sliding vector to surface plane")]
    public SlidingTrajectoryChangeTypes slidingTrajectoryChangeType;
    
    [Space(20)]
    public LayerMask raycastMask;
    public float colliderRadius;
    public float colliderHeight;
    public float colliderMinHeight;
    public float stepCheckSpheresRadius;
    public float maxClimbAngle;
    public float maxStepHeight;
    public float minStepDepth;
    public float moveCastBackStepDistance;
    public float obstacleSeparationDistance;

    public float moveSpeed;
    public float jumpStrength;

    float climb_angle_dot;

    Vector3 fall_vector;
    Vector3 fall_speed_vector;

    Transform transform_local;
    Vector3 current_position;
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
    
    Vector3 capsule_top_point;
    Vector3 capsule_bottom_point;
    Vector3 capsule_point_offset;

    float ground_height_check_val;
    
    float delta_time;
    
    bool is_grounded;

    bool is_climbing;

    bool is_jumping;

    bool is_sliding_incline;
    
    
    
    void Start()
    {
        climb_angle_dot = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        transform_local = transform;
        collider_radius = colliderRadius;
        collider_radius_x2 = collider_radius * 2;
        collider_height = colliderHeight;
        vec_up = Vector3.up;
        vec_down = Vector3.down;
        vec_zero = Vector3.zero;
        vec_forward = Vector3.forward;
        vec_back = Vector3.back;
        vec_left = Vector3.left;
        vec_right = Vector3.right;

        //fall_speed_vector = vec_down * 100;
    }


    float s;
    
    void Update() 
    //void FixedUpdate()
    {
        get_input();
        
        if(updateType != UpdateTypes.every_frame) return;
        
        delta_time = Time.deltaTime;
        //delta_time = Time.fixedDeltaTime;
        
        calculate_position();
    }
    
    void FixedUpdate()
    {
        if(updateType != UpdateTypes.fixed_frame) return;
        
        delta_time = Time.fixedDeltaTime;
        
        calculate_position();
        
        Debug.Log(fall_speed_vector.magnitude);
        //Debug.Log((fall_speed_vector.magnitude - s)/delta_time);
        
        s = fall_speed_vector.magnitude;
    }


    void get_input()
    {
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
        
        if(Keyboard.current.qKey.isPressed)
            move_vec += vec_left+vec_forward;
        
        if(move_vec.sqrMagnitude > 0.01f)
            move_vec.Normalize();
    }

    void calculate_position()
    {
        Physics.SyncTransforms();
        
        current_position = transform_local.position;
        
        capsule_point_offset = (collider_height - collider_radius_x2) * 0.5f * vec_up;

        update_capsule_points();
        
        
        handle_jump();

        handle_movement();

        handle_gravity();

        transform_local.position = current_position;

        Physics.SyncTransforms();
    }
    
    void update_capsule_points()
    {
        capsule_top_point = current_position + capsule_point_offset;
        capsule_bottom_point = current_position - capsule_point_offset;
    }
    
    
    void handle_jump()
    {
        if (!is_grounded || !Keyboard.current.spaceKey.wasPressedThisFrame) return;
        
        var head_collision_check = Physics.SphereCast(
            capsule_bottom_point,
            collider_radius,
            vec_up,
            out var hit_jump,
            4,
            raycastMask);
                
        var jump_check = head_collision_check && hit_jump.distance <= ground_height_check_val + 0.05f;
        
        if (jump_check) return;
        
        //is_jumping = true;
        fall_speed_vector = new Vector3(fall_speed_vector.x, jumpStrength, fall_speed_vector.z);
    }


    bool check_ground(out RaycastHit hit)
    {
        var ground_collision_check = Physics.SphereCast(
            capsule_top_point,
            collider_radius,
            vec_down,
            out var hit_ground, 10, raycastMask);

        hit = hit_ground;
        
        return ground_collision_check && hit_ground.distance <= ground_height_check_val + obstacleSeparationDistance * 2;// + 0.001f;
    }
    
    void handle_ground_check()
    {
        surface_normal = vec_up;
        fall_vector = vec_down;
        
        var ground_check = check_ground(out var ground_hit);

        //On the ground
        if (ground_check)
        {
            surface_normal = ground_hit.normal;
            fall_vector = Vector3.ProjectOnPlane(vec_down, ground_hit.normal);//.normalized;
            is_sliding_incline = Vector3.Dot(vec_up, surface_normal) < climb_angle_dot;
            
            //when landed from in air state
            if (is_grounded) return;
            
            is_grounded = true;
        }
        //In the air and not climbing
        else if (is_grounded && !is_climbing)
        {
            is_grounded = false;
        }
    }
    
    void handle_gravity()
    {
        if(is_climbing)
        {
            fall_speed_vector = vec_zero;
            return;
        }
        
        handle_ground_check();
        
        if(is_grounded && !is_sliding_incline && fall_speed_vector.sqrMagnitude < 0.001f)
        {
            fall_speed_vector = vec_zero;
            return;
        }
        
        var fall_speed = fall_speed_vector.magnitude;
        var fall_dist_left = delta_time * fall_speed;
        var current_fall_vec = fall_speed <= Mathf.Epsilon ? vec_down : fall_speed_vector.normalized;

        //Debug.Log(fall_speed);
        
        
        for(var i=1;i<6;i++)
        {
            
            if(fall_dist_left<=Mathf.Epsilon) break;
            
            check_fall_movement(i, current_fall_vec, fall_dist_left, out var new_move_vec, out var move_dist, out var move_dot_mult);

            if(move_dist>0.001f*delta_time)
                current_position += move_dist * current_fall_vec;

            update_capsule_points();

            handle_ground_check();

            if (move_dot_mult <= 0)
            {
                current_fall_vec = vec_zero;
                fall_speed = 0;
                break;
            }

            current_fall_vec = new_move_vec;
            
            
            fall_dist_left -= move_dist;

            if (move_dot_mult < 1f)
            {
                fall_dist_left *= move_dot_mult;
                fall_speed *= move_dot_mult;

                if (is_grounded && !is_sliding_incline)
                {
                    switch (thresholdSpeedAction)
                    {
                        case ThresholdSpeedActions.stop:
                            if (fall_speed < groundSlideSpeedThreshold)
                                fall_speed = 0;
                            break;
                        case ThresholdSpeedActions.subtract:
                            fall_speed = Mathf.Max(0, fall_speed - groundSlideSpeedThreshold);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            if(fall_dist_left<=0.0001f*delta_time || fall_speed < 0.0001f) break;
        }

        var fall_speed_vector_normalized = current_fall_vec;
        fall_speed_vector = current_fall_vec * fall_speed;
        
        
        
        //--------------------------------------------------------------------------------------------------------------
        //fall      ////////////////////////////////////////////////////////////////////////////////////////////////////
        //--------------------------------------------------------------------------------------------------------------
        
        
        //acceleration
        //--------------------------------------------------------------------------------------------------------------
        var accel = delta_time * gravity;
        //--------------------------------------------------------------------------------------------------------------
        
        //drag
        //--------------------------------------------------------------------------------------------------------------
        var drag = 1f - airDrag * delta_time;
        //--------------------------------------------------------------------------------------------------------------

        //friction
        //--------------------------------------------------------------------------------------------------------------
        var friction = 0f;
        if (is_grounded)
        {
            var current_friction = is_sliding_incline ? slideFriction : groundFriction;
            var normal_force = gravity * Vector3.Dot(surface_normal, vec_up);
            var friction_corce = normal_force * current_friction;
            friction = Mathf.Min(friction_corce * delta_time, accel);
        }
        //--------------------------------------------------------------------------------------------------------------

        
        //applying acceleration, drag and friction
        //--------------------------------------------------------------------------------------------------------------
        fall_speed_vector += accel * fall_vector;
        fall_speed_vector *= drag;
        fall_speed_vector -= friction * fall_speed_vector_normalized;
        //--------------------------------------------------------------------------------------------------------------

    }
    
    void check_fall_movement(int iter_num, Vector3 current_move_vec, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out float move_dot_mult)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        move_dot_mult = 1f;
        
        var move_back_offset = -moveCastBackStepDistance * current_move_vec;
        
        var move_hit_check = Physics.CapsuleCast(
            capsule_bottom_point + move_back_offset,
            capsule_top_point + move_back_offset,
            collider_radius,
            current_move_vec,
            out var hit_move,
            2,
            raycastMask);
        
        var hit_dist = hit_move.distance;

        //Check if we hit something along the way
        //if (!move_hit_check || hit_dist > left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance) return;
        if (!move_hit_check || hit_dist > move_dist + moveCastBackStepDistance + obstacleSeparationDistance) return;

        move_dist = Mathf.Max(iter_num == 1? -1000 : 0, hit_dist - moveCastBackStepDistance - obstacleSeparationDistance);
        
        var hit_norm = hit_move.normal;

        switch (slidingTrajectoryChangeType)
        {
            //Normalize new vec parallel to current move vec
            case SlidingTrajectoryChangeTypes.parallel_to_sliding_vector:
            {
                var move_vec_right = Vector3.Cross(vec_up, current_move_vec);
                var v = Vector3.ProjectOnPlane(hit_norm, move_vec_right);
                new_move_vec = Vector3.ProjectOnPlane(current_move_vec, v).normalized;
                break;
            }
            //Just project move vec to hit normal plane
            case SlidingTrajectoryChangeTypes.project_sliding_vector_on_surface:
                new_move_vec = Vector3.ProjectOnPlane(current_move_vec, hit_norm).normalized;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        //var dot_down_1 = Vector3.Dot(vec_down, current_fall_vec);
        //var dot_down_2 = Vector3.Dot(vec_down, new_move_vec);

        //var move_dot_mult = 1f;

        //if (dot_down_2 < dot_down_1)
        //{
            //var n = Vector3.Cross(vec_up, new_move_vec);
            //n = Vector3.Cross(new_move_vec,n).normalized;
                
        move_dot_mult = Mathf.Max(-1,Mathf.Max(-1, Vector3.Dot(current_move_vec, new_move_vec)) - Mathf.Max(-1, Vector3.Dot(current_move_vec, -hit_norm)) * slideFriction);
        //}
        
    }
    
    
    void handle_movement()
    {
        //MOVEMENT
        //--------------------------------------------------------------------------------------------------------------

        if(move_vec.sqrMagnitude< 0.01f) return;

        is_climbing = false;

        var current_move_vec_flat = move_vec;
        
        var v_right = Vector3.Cross(vec_up, move_vec);
            
        var v = Vector3.ProjectOnPlane(surface_normal, v_right);
            
        move_vec = Vector3.ProjectOnPlane(move_vec, v).normalized;
        
        var move_surface_dot = Vector3.Dot(current_move_vec_flat, surface_normal);
        
        var move_dist_left = delta_time * moveSpeed;
        var current_move_vec = move_surface_dot >= 0 ? move_vec : current_move_vec_flat;
        var current_surface_normal = surface_normal;
        var is_avoiding_obstacle = false;
        // iterations = 0;
        var iterations_obstacle = 0;
        var move_dot_mult = 1f;
        
        for(var i=1;i<6;i++)
        {
            if(move_dist_left<=Mathf.Epsilon) break;
            
            //iterations += 1;
            
            if (is_avoiding_obstacle)
                iterations_obstacle += 1;
            else
            {
                iterations_obstacle = 0;
            }
            
            var can_move = check_movement(i, move_vec, current_move_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal, out var will_avoid_obstacle, out var obstacle_dot);

            //current_position += move_dist * move_dot_mult * (1f - Mathf.Clamp01(Vector3.Dot(fall_speed_vector, current_move_vec))) * current_move_vec;
            current_position += move_dist * move_dot_mult * current_move_vec;

            update_capsule_points();

            if(!can_move) break;
            
            move_dot_mult = obstacle_dot;
            current_surface_normal = new_surface_normal;
            current_move_vec = new_move_vec;
            move_dist_left -= move_dist;
            is_avoiding_obstacle = will_avoid_obstacle;
            
            if(move_dist_left<=Mathf.Epsilon) break;
            
            if(iterations_obstacle > 1 && move_dist <= Mathf.Epsilon) break;
        }

        //--------------------------------------------------------------------------------------------------------------
    }

    bool check_movement(int iter_num, Vector3 initial_move_vec, Vector3 current_move_vec, Vector3 current_surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal, out bool will_avoid_obstacle, out float obstacle_dot)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = current_surface_normal;
        will_avoid_obstacle = false;
        obstacle_dot = 1;
        
        var current_move_vec_flat = new Vector3(current_move_vec.x, 0, current_move_vec.z).normalized;

        var move_back_offset = -moveCastBackStepDistance * current_move_vec;

        var move_hit_check = Physics.CapsuleCast(
            capsule_bottom_point + move_back_offset,
            capsule_top_point + move_back_offset,
            //collider_radius - (iter_num == 1 ? 0 : 0.001f),
            //collider_radius - 0.001f,
            collider_radius,
            current_move_vec,
            out var hit_move,
            2,
            raycastMask);

        var move_check = !move_hit_check || hit_move.distance > left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance;

        var hit_norm = hit_move.normal;

        var hit_dist = hit_move.distance;

        if (move_check) return true;
        
        var move_vec_right = Vector3.Cross(vec_up, current_move_vec);
        
        var move_hit_dot = Vector3.Dot(vec_up, hit_norm);
        
        var is_obstacle = move_hit_dot<0 || move_hit_dot < climb_angle_dot && !step_check2();

        if (is_obstacle)
        {
            var obstacle_move_sign = Mathf.Sign(
                hit_norm.y < -0.999f 
                ? 
                Vector3.Dot(hit_norm+current_surface_normal*0.1f, move_vec_right) 
                : 
                Vector3.Dot(hit_norm, move_vec_right)
                );

            var used_surf_normal = (hit_norm - current_surface_normal).sqrMagnitude < 0.0001f ? vec_up : current_surface_normal;
            
            var obstacle_move_vec = obstacle_move_sign * Vector3.Cross(hit_norm, used_surf_normal).normalized;
        
            var dot = Vector3.Dot(obstacle_move_vec, initial_move_vec);
            
            obstacle_dot = dot;
            will_avoid_obstacle = true;
            new_move_vec = obstacle_move_vec;
            move_dist = 0;
            
            return dot > 0;
        }
        
        if (hit_norm.y < 0.00001f)
            new_move_vec = vec_up;
        else
        {
            new_surface_normal = hit_norm;

            var v = Vector3.ProjectOnPlane(new_surface_normal, move_vec_right);

            new_move_vec = Vector3.ProjectOnPlane(current_move_vec_flat, v).normalized;
        }

        move_dist = Mathf.Max(iter_num == 1 ? -100 : 0,hit_dist - moveCastBackStepDistance - obstacleSeparationDistance);
        //move_dist = Mathf.Max(0,hit_dist - moveCastBackStepDistance - 0.00f);

        return true;

        bool step_check2()
        {
            if (!check_step_capsule())
            {
                return false;
            }

            var check_center = check_step_spheres(vec_zero);

            if (!check_center) return false;

            var sphere_offset = (collider_radius - stepCheckSpheresRadius) * move_vec_right;
            
            var check_right = check_step_spheres(sphere_offset);

            if (!check_right) return false;
            
            var check_left = check_step_spheres(-sphere_offset);

            if (!check_left) return false;

            is_climbing = true;

            return true;


            bool check_step_capsule()
            {
                var step_hit_check = Physics.CapsuleCast(
                    capsule_bottom_point + move_back_offset + maxStepHeight * vec_up,
                    capsule_top_point + move_back_offset,
                    collider_radius,
                    current_move_vec_flat,
                    out var hit_step,
                    2,
                    raycastMask);

                return !step_hit_check || hit_step.distance - moveCastBackStepDistance - obstacleSeparationDistance > minStepDepth;
            }

            bool check_step_spheres(Vector3 offset)
            {
                var high_sphere_pos = current_position - (collider_height * 0.5f - stepCheckSpheresRadius - maxStepHeight) * vec_up + offset - moveCastBackStepDistance * current_move_vec_flat;

                var step_hit_check_high = Physics.SphereCast(
                    high_sphere_pos,
                    stepCheckSpheresRadius,
                    current_move_vec_flat,
                    out var hit_step_high,
                    2,
                    raycastMask);

                var v1 = !step_hit_check_high;
                if (v1) return true;

                var step_high_dist_check = hit_step_high.distance - collider_radius - moveCastBackStepDistance - obstacleSeparationDistance > minStepDepth;
                return step_high_dist_check;
            }
        }
    }
    
    

}
