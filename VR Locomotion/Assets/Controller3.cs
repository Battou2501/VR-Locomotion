using UnityEngine;
using UnityEngine.InputSystem;

public class Controller3 : MonoBehaviour
{
    public enum SlideDecelTypes
    {
        broken_line_zero_to_one_to_N = 0,
        curve_zero_to_4_trhough_one = 1
    }

    public enum UpdateTypes
    {
        every_frame = 0,
        fixed_frame = 1
    }

    public UpdateTypes updateType;
    
    public float gravity = 9.8f;

    public LayerMask raycastMask;
    public float colliderRadius;
    public float colliderHeight;
    public float colliderMinHeight;
    public float stepCheckSpheresRadius;
    public float terminalVelocityFall;
    public float terminalVelocitySlide;
    public float maxClimbAngle;
    public float maxStepHeight;
    public float minStepDepth;
    public float slideDecelerationGround;
    public float slideDecelerationTrajectoryChange;
    public SlideDecelTypes groundSlideDecelType;
    public float maxDecelCoefForBrokenLineType;
    public float moveCastBackStepDistance;
    public float obstacleSeparationDistance;
    //public bool useGroundLevelFixCheck;
    //public float minGroundIntersectionToIntervene;
    //public float maxGroundIntersectionToIntervene;

    public float moveSpeed;
    public float jumpStrength;

    float fall_speed;

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
    }

    
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

        handle_ground_check();

        handle_gravity();

        transform_local.position = current_position;

        Physics.SyncTransforms();
    }
    
    void update_capsule_points()
    {
        capsule_top_point = current_position + capsule_point_offset;
        capsule_bottom_point = current_position - capsule_point_offset;
    }
    
    void overlap_check()
    {
        //var r = Physics.OverlapSphere(capsule_bottom_point, collider_radius + obstacleSeparationDistance, raycastMask);
        var r = Physics.OverlapSphere(capsule_bottom_point, collider_radius, raycastMask);
        
        if(r == null || r.Length == 0) return;
        
        var closest_point = r[0].ClosestPoint(capsule_bottom_point);
        var closest_sqr_dist = (closest_point - capsule_bottom_point).sqrMagnitude;

        for (var i = 1; i < r.Length; i++)
        {
            var c = r[i];
            
            var p = c.ClosestPoint(capsule_bottom_point);
            var d = (p - capsule_bottom_point).sqrMagnitude;
            
            
            if(d>=closest_sqr_dist) continue;
            
            var dot = Vector3.Dot(vec_up, (p - capsule_bottom_point));

            if(dot>=0) continue;
            
            closest_point = p;
            closest_sqr_dist = d;
        }
        
        var dot_closest = Vector3.Dot(vec_up, (closest_point - capsule_bottom_point));

        if(dot_closest>=0) return;
        
        //Debug.DrawRay(closest_point, vec_up, Color.red);
        
        var dist = (closest_point - capsule_bottom_point).magnitude;

        //if(dist>=collider_radius + obstacleSeparationDistance) return;
        if(dist>=collider_radius) return;

        var vec = (capsule_bottom_point - closest_point).normalized;

        //current_position += (collider_radius + obstacleSeparationDistance - dist) * vec;
        current_position += (collider_radius - dist) * vec;

        update_capsule_points();
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
        
        is_jumping = true;
        fall_speed = -jumpStrength;
    }
    

    void handle_ground_check()
    {
        //GRAVITY
        //--------------------------------------------------------------------------------------------------------------

        surface_normal = vec_up;
        fall_vector = vec_down;
        
        var ground_collision_check = Physics.SphereCast(
            capsule_top_point,
            collider_radius,
            vec_down,
            out var hit_ground, 10, raycastMask);

        var ground_check = ground_collision_check && hit_ground.distance <= ground_height_check_val + obstacleSeparationDistance * 2;// + 0.001f;

        //On the ground
        if (ground_check && !is_jumping)
        {
            surface_normal = hit_ground.normal;
            fall_vector = Vector3.ProjectOnPlane(vec_down, hit_ground.normal).normalized;
            is_sliding_incline = Vector3.Dot(vec_up, surface_normal) < climb_angle_dot;
            
            //when landed from in air state
            if (!is_grounded)
            {
                fall_speed_vector += Vector3.Project(vec_down * fall_speed, fall_vector);
                fall_speed = 0;
                is_grounded = true;
            }
            
            return;
        }
        
        //In the air
        if (is_climbing)
        {
            fall_speed = 0;
            return;
        }

        if (is_grounded)
        {
            is_grounded = false;
        }

        if (!ground_check && is_jumping)
            is_jumping = false;

        if (fall_speed < -0.1f)
        {
            var head_collision_check = Physics.SphereCast(
                capsule_bottom_point,
                collider_radius,
                vec_up,
                out var hit_jump,
                4,
                raycastMask);
                
            var jump_check = head_collision_check && hit_jump.distance <= ground_height_check_val + 0.01f;

            if (jump_check)
            {
                fall_speed = 0;
                is_jumping = false;
            }
        }

        ////var touch_ground = ground_collision_check && hit_ground.distance - ground_height_check_val - obstacleSeparationDistance < delta_time * fall_speed;
        //    
        ////if(!touch_ground && fall_speed < terminalVelocityFall)
        //if(fall_speed < terminalVelocityFall)
        //    fall_speed += delta_time * gravity;
        //
        ////var mag = (!ground_collision_check ? delta_time * fall_speed : Mathf.Max(0, Mathf.Min(hit_ground.distance - ground_height_check_val - obstacleSeparationDistance, delta_time * fall_speed)));
        //var mag = !ground_collision_check ? delta_time * fall_speed : Mathf.Min(hit_ground.distance - ground_height_check_val - obstacleSeparationDistance, delta_time * fall_speed);
        //
        //current_position += mag * vec_down;
        //
        ////Debug.Log(mag);
        //
        //update_capsule_points();
        //
        //slide_speed_vector = Vector3.Lerp(slide_speed_vector, vec_zero, delta_time * slideDecelerationTrajectoryChange);
        //if (slide_speed_vector.sqrMagnitude < 0.001f)
        //    slide_speed_vector = vec_zero;


        //--------------------------------------------------------------------------------------------------------------
    }
    
    void handle_gravity()
    {
        if(is_climbing || is_grounded && !is_sliding_incline && fall_speed_vector.sqrMagnitude < 0.001f)
        {
            fall_speed_vector = vec_zero;
            return;
        }

        var slide_magnitude = fall_speed_vector.magnitude;
        var move_dist_left = delta_time * slide_magnitude;
        var current_move_vec = fall_speed_vector.normalized;
        var current_surface_normal = surface_normal;

        for(var i=1;i<6;i++)
        {
            if(move_dist_left<=Mathf.Epsilon) break;
            
            check_movement2(i, current_move_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal);
            
            current_position += move_dist * current_move_vec;
            
            update_capsule_points();

            var current_move_vec_flat = Vector3.ProjectOnPlane(current_move_vec, vec_up).normalized;
            
            var move_dot_mult = Mathf.Max(0, Vector3.Dot(current_move_vec, new_move_vec)) * (Vector3.Dot(current_move_vec_flat, new_move_vec) < 0 ? 0 : 1);

            if (move_dot_mult <= 0)
            {
                current_move_vec = vec_zero;
                slide_magnitude = 0;
                break;
            }
            
            current_surface_normal = new_surface_normal;
            
            current_move_vec = new_move_vec;
            
            move_dist_left -= move_dist;
            move_dist_left *= move_dot_mult;
            
            slide_magnitude *= move_dot_mult;

            if(move_dist_left<=Mathf.Epsilon) break;
        }

        fall_speed_vector = current_move_vec * slide_magnitude;

        
        //fall
        if (!is_grounded || is_sliding_incline)
        {
            if (fall_speed_vector.sqrMagnitude < terminalVelocitySlide * terminalVelocitySlide)
            {
                fall_speed_vector += Mathf.Max(0, Vector3.Dot(vec_down, fall_vector)) * delta_time * gravity * fall_vector;

                var mag = fall_speed_vector.magnitude;
                fall_speed_vector = Vector3.Lerp(fall_speed_vector.normalized, fall_vector, delta_time * slideDecelerationTrajectoryChange) * mag;

            }
            else
            {
                fall_speed_vector = Vector3.Lerp(fall_speed_vector, vec_zero, delta_time * slideDecelerationTrajectoryChange);
            }
            
            return;
        }

        //stand
        if (fall_speed_vector.sqrMagnitude > 0.01f)
        {
            var slide_up_dot = Vector3.Dot(vec_up, fall_speed_vector.normalized);
            slide_up_dot = groundSlideDecelType == SlideDecelTypes.broken_line_zero_to_one_to_N ? slide_up_dot : (slide_up_dot * 0.5f + 0.5f) * 2f;
                
            var slide_incline_slowdown_coefficient = groundSlideDecelType == SlideDecelTypes.curve_zero_to_4_trhough_one
                ? slide_up_dot * slide_up_dot
                : Mathf.Max(0, slide_up_dot) * (maxDecelCoefForBrokenLineType-1f) + Mathf.Min(1, slide_up_dot + 1);

            fall_speed_vector = Vector3.Lerp(fall_speed_vector, vec_zero, delta_time * slideDecelerationGround * slide_incline_slowdown_coefficient);
            return;
        }

        fall_speed_vector = vec_zero;
            
        

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

            current_position += move_dist * move_dot_mult * (1f - Mathf.Clamp01(Vector3.Dot(fall_speed_vector, current_move_vec))) * current_move_vec;

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
    
    void check_movement2(int iter_num, Vector3 current_move_vec, Vector3 current_surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = current_surface_normal;
        
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

        
        
        if (!move_hit_check || hit_dist > left_move_dist + moveCastBackStepDistance + 0.001f) return;

        var hit_norm = hit_move.normal;

        var move_vec_right = Vector3.Cross(vec_up, current_move_vec);
        
        new_surface_normal = hit_norm;
        
        var v = Vector3.ProjectOnPlane(new_surface_normal, move_vec_right);
        
        new_move_vec = Vector3.ProjectOnPlane(current_move_vec, v).normalized;
        
        move_dist = Mathf.Max(iter_num == 1? -1000 : 0, hit_dist - moveCastBackStepDistance - obstacleSeparationDistance);
        //move_dist = Mathf.Max(0, hit_dist - moveCastBackStepDistance - 0.001f);
    }

}
