using System;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class  Controller7 : MonoBehaviour
{
    public enum UpdateTypes
    {
        every_frame = 0,
        fixed_frame = 1
    }

    public enum SlidingTrajectoryChangeTypes
    {
        project_sliding_vector_on_surface = 0,
        parallel_to_sliding_vector = 1
    }

    public enum SlideSlowdownTypes
    {
        slowdown_speedup = 0,
        slowdown_full_speed = 1
    }
    
    [Header("Which method is used to calculate movement")]
    [FormerlySerializedAs("updateType")] public UpdateTypes updateMovementDuring;
    
    public float gravity = 9.81f;
    
    [Header("Air resistance while falling or sliding")]
    [Range(0,1)]
    public float airDrag = 0.1f;
    
    [Header("Friction when sliding ground with incline\nlower than Max climb angle")]
    [Range(0,1)]
    public float groundFriction = 1f;
    
    [Header("Sliding speed needed to continue slide\nwhen touching ground with incline lower than Max climb angle\nWill stop sliding if speed falls lower than threshold")]
    public float groundSlideSpeedThreshold = 0;

    [Header("Friction when sliding ground with incline\ngreater than Max climb angle")]
    [Range(0,1)]
    public float slideFriction = 0.3f;

    [Header("How to calculate ne trajectory on collision when sliding\nProject - will project sliding vector to surface plane (Same as Unity's physics)\nParallel - new vector will only change incline")]
    public SlidingTrajectoryChangeTypes slidingTrajectoryChangeType;

    public SlideSlowdownTypes slideSlowdownType;

    [Header("Do not accelerate down slope due to gravity on inclines smaller than max climb angle")]
    public bool doNotAccelerateDueToGravityOnClimbableIncline = true;
    
    [Space(20)]
    public LayerMask raycastMask;
    public float colliderRadius = 0.34f;
    public float colliderHeight = 2.08f;
    public float colliderMinHeight;
    public float stepCheckSpheresRadius = 0.05f;
    public float maxClimbAngle = 45f;
    public float maxStepHeight = 0.35f;
    public float minStepDepth = 0.25f;
    public float moveCastBackStepDistance = 0.1f;
    public float stopDistanceBeforeObstacle = 0.002f;

    public float moveSpeed=4;
    public float jumpStrength=6;

    float climb_angle_cos_aka_vertical_dot;
    float climb_angle_sin_aka_horizontal_dot;

    Vector3 fall_vector;
    Vector3 fall_speed_vector;

    Transform transform_local;
    Vector3 current_position;
    float collider_radius;
    float collider_radius_x2;
    float collider_height;
    
    Vector3 surface_normal;
    Vector3 move_vec;
    Vector3 move_vec_flat;

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
    float one_over_delta_time;
    
    bool is_grounded;

    bool is_climbing;

    bool is_jump_requested;

    bool is_sliding_incline;

    bool is_moving_this_frame;
    bool was_moving_last_frame;

    Vector3 current_frame_move_vector;
    Vector3 last_frame_move_vector;

    float max_slope_angle_normal;
    //float calculated_step_depth;

    void Start()
    {
        climb_angle_cos_aka_vertical_dot = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        climb_angle_sin_aka_horizontal_dot = Mathf.Sin(maxClimbAngle * Mathf.Deg2Rad);
        //calculated_step_depth = climb_angle_cos * (maxStepHeight / climb_angle_sin);
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
    }

    void Update() 
    {
        get_input();
        
        if(updateMovementDuring != UpdateTypes.every_frame) return;
        
        delta_time = Time.deltaTime;
        one_over_delta_time = 1.0f / delta_time;
        
        calculate_position();
    }
    
    void FixedUpdate()
    {
        if(updateMovementDuring != UpdateTypes.fixed_frame) return;
        
        delta_time = Time.fixedDeltaTime;
        one_over_delta_time = 1.0f / delta_time;
        
        calculate_position();
    }


    void get_input()
    {
        move_vec = vec_zero;
        
        ground_height_check_val = collider_height - collider_radius_x2;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            is_jump_requested = true;
        
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
        
        if(Keyboard.current.cKey.isPressed)
            move_vec += vec_right+vec_back;
        
        if(Keyboard.current.zKey.isPressed)
            move_vec += vec_left+vec_back;
        
        if(move_vec.sqrMagnitude > 0.01f)
            move_vec.Normalize();

        move_vec_flat = move_vec;
    }

    void calculate_position()
    {
        Physics.SyncTransforms();
        
        current_position = transform_local.position;
        
        capsule_point_offset = (collider_height - collider_radius_x2) * 0.5f * vec_up;

        update_capsule_points();

        
        handle_jump();

        var position_before_move = current_position;
        
        handle_movement();

        var position_after_move = current_position;

        current_frame_move_vector = position_after_move - position_before_move;

        handle_gravity();
        
        was_moving_last_frame = is_moving_this_frame;
        last_frame_move_vector = current_frame_move_vector;
        
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
        if (!is_grounded || !is_jump_requested) return;

        is_jump_requested = false;

        var head_collision_check = Physics.SphereCast(
            capsule_bottom_point,
            collider_radius + 0.02f,
            vec_up,
            out var hit_jump,
            4,
            raycastMask);
                
        var jump_check = head_collision_check && hit_jump.distance <= ground_height_check_val + 0.05f;
        
        if (jump_check) return;
        
        is_climbing = false;
        fall_speed_vector = new Vector3(fall_speed_vector.x, jumpStrength, fall_speed_vector.z);
    }


    bool check_ground(out RaycastHit hit)
    {
        var ground_collision_check = Physics.SphereCast(
            capsule_top_point,
            collider_radius,
            vec_down,
            out var hit_ground, 3, raycastMask);

        hit = hit_ground;

        return ground_collision_check && hit_ground.distance <= ground_height_check_val + (is_grounded ? 0.05f : 0.02f);//Consider adding extra clearance only when grounded (is_grounded ? 4 : 1);
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
            is_sliding_incline = Vector3.Dot(vec_up, surface_normal) < climb_angle_cos_aka_vertical_dot;
            
            //var d = Mathf.Min(0, ground_hit.distance - (ground_height_check_val));
            var d = ground_hit.distance - (ground_height_check_val);
            
            //if(!is_climbing && d<-0.001f)
            if(!is_climbing && d> Mathf.Abs(0.001f))
                current_position += d * vec_down;
            
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
        
        handle_ground_check();
        
        if(is_climbing || is_grounded && !is_sliding_incline && doNotAccelerateDueToGravityOnClimbableIncline && fall_speed_vector.sqrMagnitude < 0.0001f)
        {
            fall_speed_vector = vec_zero;
            return;
        }
        
        //EXECUTES ONLY AT THE FRAME AFTER MOVEMENT INPUT STOPS
        //correct slide if needed when movement input stops
        if (is_grounded && slideSlowdownType == SlideSlowdownTypes.slowdown_speedup && was_moving_last_frame && !is_moving_this_frame)
        {
            var move_on_slide_plane = Vector3.ProjectOnPlane(last_frame_move_vector, surface_normal);
            var dot = Vector3.Dot(move_on_slide_plane, fall_speed_vector) <= 0;
            var move_parallel_to_slide = one_over_delta_time * Vector3.Project(move_on_slide_plane, fall_speed_vector);

            var fall_speed_lower_than_parallel_move = fall_speed_vector.sqrMagnitude < move_parallel_to_slide.sqrMagnitude;
            
            //If movement vector was at least partially in the opposite direction to slide
            if (dot)
            {
                //if movement speed along slide vector was already greater than slide speed (we were accelerating in opposite direction from slide) than stop
                if (fall_speed_lower_than_parallel_move)
                {
                    fall_speed_vector = vec_zero;
                    return;
                }
        
                //if movement speed along slide vector was lower than slide speed (we were decelerating in the direction of slide) than slide with new slide speed due to deceleration
                fall_speed_vector += move_parallel_to_slide;
            }
            //If movement input vector was in the same direction as slide and is greater along slide vector than the slide vector
            else if (fall_speed_lower_than_parallel_move)
            {
                //Stops is is on incline smaller that max climb angle
                if (is_grounded && !is_sliding_incline && (doNotAccelerateDueToGravityOnClimbableIncline || fall_speed_vector.sqrMagnitude < 0.0001f))
                {
                    fall_speed_vector = vec_zero;
                    return;
                }
        
                //Continue sliding with last movement input speed along slide vector is is on incline greater than max climb angle
                fall_speed_vector += move_parallel_to_slide;
            }
        }

        //EXECUTES EVERY FRAME THAT THERE IS MOVEMENT INPUT
        //slows down sliding and eventually speed up in opposite direction
        if (is_grounded && is_moving_this_frame)
        {
            var move_on_slide_plane = Vector3.ProjectOnPlane(current_frame_move_vector, surface_normal);
            var dot = Vector3.Dot(move_on_slide_plane, fall_speed_vector);
            
            if (dot <= 0)
            {
                var move_parallel_to_slide = Vector3.Project(one_over_delta_time * current_frame_move_vector, fall_speed_vector);
                
                if (!was_moving_last_frame)
                {
                    fall_speed_vector -= move_parallel_to_slide;
                }

                var is_fall_speed_lower_than_move_vector = (move_parallel_to_slide * delta_time).sqrMagnitude > fall_speed_vector.sqrMagnitude;
                
                fall_speed_vector -= is_fall_speed_lower_than_move_vector ? fall_speed_vector : -move_parallel_to_slide * delta_time;
            }
        }
        

        var fall_speed = fall_speed_vector.magnitude;
        var fall_dist_left = fall_speed * delta_time;
        var current_fall_vec = fall_speed <= Mathf.Epsilon ? is_grounded ? fall_vector.normalized : vec_down : fall_speed_vector.normalized;

        Debug.Log(fall_speed);

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
            }

            if (is_grounded && !is_sliding_incline && fall_speed < groundSlideSpeedThreshold)
            {
                fall_speed = 0;
                break;
            }
            
            if(fall_dist_left<=0.0001f*delta_time || fall_speed < 0.0001f) break;
        }
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
            var friction_force = normal_force * current_friction;
            friction = Mathf.Min(friction_force * delta_time, accel);
        }
        //--------------------------------------------------------------------------------------------------------------

        
        //applying acceleration, drag and friction
        //--------------------------------------------------------------------------------------------------------------
        
        //acceleration
        if(!is_grounded || is_sliding_incline || !doNotAccelerateDueToGravityOnClimbableIncline)
            fall_speed_vector += accel * fall_vector;
        
        //drag
        fall_speed_vector *= drag;

        //friction
        if (fall_speed_vector.sqrMagnitude < friction * friction)
        {
            fall_speed_vector = vec_zero;
        }
        else
        {
            var fall_speed_vector_normalized = fall_speed_vector.normalized;
            fall_speed_vector -= friction * fall_speed_vector_normalized;
        }

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
        if (!move_hit_check || hit_dist > move_dist + moveCastBackStepDistance) return;

        //move_dist = Mathf.Max(iter_num == 1? -1000 : 0, hit_dist - moveCastBackStepDistance - obstacleSeparationDistance);
        move_dist = Mathf.Max( 0, hit_dist - moveCastBackStepDistance - stopDistanceBeforeObstacle);
        
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
        
        var current_friction = is_sliding_incline ? slideFriction : groundFriction;
        move_dot_mult = Mathf.Max(-1,Mathf.Max(-1, Vector3.Dot(current_move_vec, new_move_vec)) - Mathf.Max(-1, Vector3.Dot(current_move_vec, -hit_norm)) * current_friction);
    }
    
    
    void handle_movement()
    {
        //MOVEMENT
        //--------------------------------------------------------------------------------------------------------------

        is_moving_this_frame = false;
        
        if(move_vec.sqrMagnitude< 0.01f) return;

        is_moving_this_frame = true;
        
        is_climbing = false;

        var v_right = Vector3.Cross(vec_up, move_vec);
            
        var v = Vector3.ProjectOnPlane(surface_normal, v_right);
            
        move_vec = Vector3.ProjectOnPlane(move_vec, v).normalized;
        
        var ms = moveSpeed;
        
        //Limit movement to not accelerate sliding down hill if it is already faster than move
        if (is_grounded && fall_speed_vector.sqrMagnitude > 0.0001f && Vector3.Dot(move_vec, fall_speed_vector) > 0)
        {
            var m2 = move_vec * moveSpeed;
            var vec = m2 - fall_speed_vector;
        
            if (Vector3.Dot(fall_speed_vector, vec) <= 0)
            {
                var perpendicular_part = Vector3.ProjectOnPlane(m2, fall_speed_vector);
                vec = perpendicular_part;
            }
            
            move_vec = vec;
            ms = move_vec.magnitude;
            move_vec /= ms;
        }
        
        var move_surface_dot = Vector3.Dot(move_vec_flat, surface_normal);
        
        var move_dist_left = delta_time * ms;
        var current_move_normalized_vec = move_surface_dot >= 0 ? move_vec : move_vec_flat;
        var current_surface_normal = surface_normal;
        var is_avoiding_obstacle = false;
        var iterations_obstacle = 0;
        var move_dot_mult = 1f;

        for(var i=1;i<6;i++)
        {
            if(move_dist_left<=Mathf.Epsilon) break;
            
            if (is_avoiding_obstacle)
                iterations_obstacle += 1;
            else
            {
                iterations_obstacle = 0;
            }

            var can_move = check_movement(i, current_move_normalized_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal, out var will_avoid_obstacle, out var obstacle_dot);

            current_position += move_dist * move_dot_mult * current_move_normalized_vec;

            update_capsule_points();

            if(!can_move) break;
            
            move_dot_mult = obstacle_dot > -0.5f ? obstacle_dot : move_dot_mult;
            current_surface_normal = new_surface_normal;
            current_move_normalized_vec = new_move_vec;
            //move_dist_left -= Mathf.Max(0,move_dist);
            move_dist_left -= move_dist;
            is_avoiding_obstacle = will_avoid_obstacle;
            
            if(move_dist_left<=Mathf.Epsilon) break;
            
            if(move_dot_mult < 0.005f) break;
            
            if(iterations_obstacle > 2 && move_dist <= Mathf.Epsilon) break;
        }

        //--------------------------------------------------------------------------------------------------------------
    }

    bool check_movement(int iter_num, Vector3 current_move_vec, Vector3 current_surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal, out bool will_avoid_obstacle, out float obstacle_dot)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = current_surface_normal;
        will_avoid_obstacle = false;
        obstacle_dot = -1;
        
        var move_back_offset = -moveCastBackStepDistance * current_move_vec;

        var move_hit_check = Physics.CapsuleCast(
            capsule_bottom_point + move_back_offset + 0.001f*vec_up,
            capsule_top_point + move_back_offset,
            collider_radius,
            current_move_vec,
            out var hit_move,
            2,
            raycastMask);

        var move_check = !move_hit_check || hit_move.distance > left_move_dist + moveCastBackStepDistance;

        var hit_norm = hit_move.normal;

        var hit_dist = hit_move.distance;

        if (move_check) return true;
        
        var move_vec_right = Vector3.Cross(vec_up, current_move_vec);
        
        var move_hit_dot = Vector3.Dot(vec_up, hit_norm);

        var current_move_vec_flat = current_move_vec.flattened_normalized();
        
        var is_obstacle = move_hit_dot < 0 || move_hit_dot < climb_angle_cos_aka_vertical_dot && !step_check4();

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
            
            new_move_vec = obstacle_move_sign * Vector3.Cross(hit_norm, used_surf_normal).normalized;
            obstacle_dot = Vector3.Dot(new_move_vec, move_vec_flat);
            will_avoid_obstacle = true;
            move_dist = Mathf.Max(0,hit_dist - moveCastBackStepDistance - stopDistanceBeforeObstacle);
            
            return obstacle_dot > 0;
        }
        
        if (hit_norm.y < 0.00001f)
            new_move_vec = vec_up;
        else
        {
            new_surface_normal = hit_norm;

            var v = Vector3.ProjectOnPlane(new_surface_normal, move_vec_right);

            new_move_vec = Vector3.ProjectOnPlane(current_move_vec_flat, v).normalized;
        }

        move_dist = Mathf.Max(iter_num == 1 ? -100 : 0,hit_dist - moveCastBackStepDistance - stopDistanceBeforeObstacle);

        //move_dist = Mathf.Max(0,hit_dist - moveCastBackStepDistance - obstacleSeparationDistance - 0.002f);

        
        return true;

        bool step_check4()
        {
            var back_offset = moveCastBackStepDistance * current_move_vec_flat;
            
            if (!check_step_capsule())
            {
                return false;
            }

            var check_center = check_step_spheres(vec_zero);
            
            if (!check_center) return false;
            
            //var sphere_offset = (collider_radius - stepCheckSpheresRadius) * move_vec_right;
            var sphere_offset = (collider_radius - stepCheckSpheresRadius) * move_vec_right - collider_radius * 0.5f * current_move_vec_flat;
            
            var check_right = check_step_spheres(sphere_offset);
            
            if (!check_right) return false;
            
            var check_left = check_step_spheres(-sphere_offset);
            
            if (!check_left) return false;

            
            
            is_climbing = true;

            return true;


            bool check_step_capsule()
            {
                var bottom_point = capsule_bottom_point + maxStepHeight * vec_up;

                var is_capsule_hit_something = Physics.CapsuleCast(
                    bottom_point - back_offset,
                    capsule_top_point - back_offset,
                    collider_radius,
                    current_move_vec_flat,
                    out var hit_step,
                    //calculated_step_depth + moveCastBackStepDistance,
                    minStepDepth + moveCastBackStepDistance,
                    raycastMask
                );
                
                var capsule_hit_distance_is_greater_than_min_step_depth = hit_step.distance-moveCastBackStepDistance > minStepDepth;
                
                if (is_capsule_hit_something && !capsule_hit_distance_is_greater_than_min_step_depth) return false;
                
                //var move_hit_to_capsule_hit_slope_is_smaller_than_max_climbable_angle = Vector3.Dot(vec_up, (hit_step.point - hit_move.point).normalized) <= climb_angle_sin;

                var is_cast_down_hit_something = Physics.SphereCast(
                    //capsule_top_point + (minStepDepth + collider_radius * (1f - Vector3.Dot(vec_up, hit_move.normal))) * current_move_vec_flat,
                    capsule_top_point + (minStepDepth) * current_move_vec_flat,
                    collider_radius,
                    vec_down,
                    out var cast_down_hit,
                    colliderHeight - collider_radius_x2,
                    raycastMask
                );
                
                var is_hit_lower_than_max_step_height = colliderHeight - collider_radius_x2 - cast_down_hit.distance < maxStepHeight;

                var is_cast_down_hit_slope_smaller_than_max_climbable_angle = Vector3.Dot(vec_up, cast_down_hit.normal) >= climb_angle_cos_aka_vertical_dot;

                var cast_down_hit_to_capsule_hit_slope_is_smaller_than_max_climbable_angle = Vector3.Dot(vec_up, (cast_down_hit.point - hit_move.point).normalized) <= climb_angle_sin_aka_horizontal_dot;
                
                //if (is_capsule_hit_something && !move_hit_to_capsule_hit_slope_is_smaller_than_max_climbable_angle) return false;
                
                switch (is_cast_down_hit_something)
                {
                    case true when !is_hit_lower_than_max_step_height:
                    case true when !is_cast_down_hit_slope_smaller_than_max_climbable_angle && !cast_down_hit_to_capsule_hit_slope_is_smaller_than_max_climbable_angle:
                        return false;
                    default:
                        return true;
                }
            }
            
            bool check_step_spheres(Vector3 offset)
            {
                var high_sphere_pos = current_position - (collider_height * 0.5f - stepCheckSpheresRadius - maxStepHeight) * vec_up + offset;

                var step_hit_check_high = Physics.SphereCast(
                    high_sphere_pos - back_offset,
                    stepCheckSpheresRadius,
                    current_move_vec_flat,
                    out _,
                    //minStepDepth + moveCastBackStepDistance + collider_radius,
                    minStepDepth + moveCastBackStepDistance,
                    raycastMask);


                return !step_hit_check_high;
            }
        }
    }
}
