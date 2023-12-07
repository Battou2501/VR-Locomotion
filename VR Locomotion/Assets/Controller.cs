using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    public enum SlideDecelTypes
    {
        broken_line_zero_to_one_to_N = 0,
        curve_zero_to_4_trhough_one = 1
    }
    
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

    public float moveSpeed;
    public float jumpStrength;
    
    //CapsuleCollider col;

    float fall_speed;

    float climb_angle_dot;

    Vector3 slide_vector;
    Vector3 slide_speed_vector;

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

        //var s = Stopwatch.StartNew();
        //
        //for (int i = 0; i < 1000000; i++)
        //{
        //    var r = Random.onUnitSphere;
        //
        //    //var c = Physics.Raycast(vec_zero, r, out var hit);
        //    //var c = Physics.SphereCast(vec_zero, 0.05f, r, out var hit);
        //    var c = Physics.CapsuleCast(vec_zero, vec_up, 0.05f, r, out var hit, raycastMask);
        //}
        //s.Stop();
        //
        //Debug.Log(s.ElapsedMilliseconds);
    }

    // Update is called once per frame
    void Update()
    //void FixedUpdate()
    {
        Physics.SyncTransforms();
        
        delta_time = Time.deltaTime;
        //delta_time = Time.fixedDeltaTime;
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
        
        current_position = transform_local.position;
        
        var capsule_point_offset = (collider_height - collider_radius_x2) * 0.5f * vec_up;
        
        capsule_top_point = current_position + capsule_point_offset;
        capsule_bottom_point = current_position - capsule_point_offset;
        
        handle_jump();

        handle_movement();
        
        capsule_top_point = current_position + capsule_point_offset;
        capsule_bottom_point = current_position - capsule_point_offset;

        handle_gravity();
        
        capsule_top_point = current_position + capsule_point_offset;
        capsule_bottom_point = current_position - capsule_point_offset;

        handle_slide();

        transform_local.position = current_position;
        
        Debug.DrawRay(current_position, slide_speed_vector.normalized*10, Color.red);
        Debug.Log(slide_speed_vector.magnitude);
        
        Physics.SyncTransforms();
    }

    void handle_jump()
    {
        if (!is_grounded || !Keyboard.current.spaceKey.wasPressedThisFrame) return;
        
        var head_collision_check = Physics.SphereCast(
            //transform.position + (collider_height - collider_radius_x2) * 0.5f * vec_down,
            capsule_bottom_point,
            collider_radius,
            vec_up,
            out var hit_jump,
            4,
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
            capsule_top_point,
            collider_radius,
            vec_down,
            out var hit_ground, 10, raycastMask);

            var ground_check = ground_collision_check && hit_ground.distance <= ground_height_check_val + obstacleSeparationDistance + 0.01f;

        //On the ground
        if (ground_check && !is_jumping)
        {
            surface_normal = hit_ground.normal;
            slide_vector = Vector3.ProjectOnPlane(vec_down, hit_ground.normal).normalized;
            var up_ground_norm_dot = Vector3.Dot(vec_up, surface_normal);
            
            //when landed from in air state
            if (!is_grounded)
            {
                slide_speed_vector = Vector3.Project(vec_down * fall_speed, slide_vector);
                fall_speed = 0;
                is_grounded = true;
            }
            
            //fix position if clipping through ground
            if (hit_ground.distance <= ground_height_check_val + obstacleSeparationDistance)
            {
                current_position += vec_down * (hit_ground.distance - (ground_height_check_val + obstacleSeparationDistance));
            }

            //stand
            if (up_ground_norm_dot >= climb_angle_dot || is_climbing)
            {

                if (slide_speed_vector.sqrMagnitude > 0.01f)
                {
                    var slide_up_dot = Vector3.Dot(vec_up, slide_speed_vector.normalized);
                    slide_up_dot = groundSlideDecelType == SlideDecelTypes.broken_line_zero_to_one_to_N ? slide_up_dot : (slide_up_dot * 0.5f + 0.5f) * 2f;
                    
                    var slide_incline_slowdown_coef = groundSlideDecelType == SlideDecelTypes.curve_zero_to_4_trhough_one
                        ? slide_up_dot * slide_up_dot
                        : Mathf.Max(0, slide_up_dot) * (maxDecelCoefForBrokenLineType-1f) + Mathf.Min(1, slide_up_dot + 1);

                    slide_speed_vector = Vector3.Lerp(slide_speed_vector, vec_zero, delta_time * slideDecelerationGround * slide_incline_slowdown_coef);
                    return;
                }

                slide_speed_vector = vec_zero;
                
                return;
            }
            
            //slide
            if (!is_climbing)
            {
                if (slide_speed_vector.sqrMagnitude < terminalVelocitySlide*terminalVelocitySlide)
                {
                    slide_speed_vector += Mathf.Max(0,1f-up_ground_norm_dot/climb_angle_dot) * delta_time * gravity * slide_vector;
                        
                    var mag = slide_speed_vector.magnitude;
                    slide_speed_vector = Vector3.Lerp(slide_speed_vector.normalized, slide_vector, delta_time * slideDecelerationTrajectoryChange)*mag;
                        
                }
                else
                {
                    slide_speed_vector = Vector3.Lerp(slide_speed_vector, vec_zero, delta_time * slideDecelerationTrajectoryChange);
                }
            }
            else
            {
                slide_speed_vector = vec_zero;
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

        if (fall_speed < 0)
        {
            var head_collision_check = Physics.SphereCast(
                capsule_bottom_point,
                collider_radius,
                vec_up,
                out var hit_jump,
                4,
                raycastMask);
                
            var jump_check = head_collision_check && hit_jump.distance <= ground_height_check_val + obstacleSeparationDistance + 0.01f;

            if (jump_check)
            {
                fall_speed = 0;
                is_jumping = false;
            }
        }

        var touch_ground = ground_collision_check && hit_ground.distance - ground_height_check_val - obstacleSeparationDistance < delta_time * fall_speed;
            
        if(!touch_ground && fall_speed < terminalVelocityFall)
            fall_speed += delta_time * gravity;

        current_position += (!ground_collision_check ? delta_time * fall_speed :  Mathf.Min(hit_ground.distance - ground_height_check_val - obstacleSeparationDistance, delta_time * fall_speed)) * vec_down;

        slide_speed_vector = Vector3.Lerp(slide_speed_vector, vec_zero, delta_time * slideDecelerationTrajectoryChange);
        if (slide_speed_vector.sqrMagnitude < 0.01f)
            slide_speed_vector = vec_zero;



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
        var avoid_iterations = 0;

        for(var i=0;i<5;i++)
        {
            if (is_avoiding_obstacle)
                avoid_iterations += 1;
            else
            {
                avoid_iterations = 0;
            }

            check_movement2(current_move_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal, out var will_avoid_obstacle);
            
            current_position += move_dist * current_move_vec;
            
            //move_dot_mult = obstacle_dot;
            //var move_vec_right = Vector3.Cross(vec_up, current_move_vec);
            //var move_vec_normal = Vector3.Cross(current_move_vec, move_vec_right);
            
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
            
            is_avoiding_obstacle = will_avoid_obstacle;
            
            if(move_dist_left<=Mathf.Epsilon) break;

            if (avoid_iterations <= 1 || !(move_dist < Mathf.Epsilon)) continue;
            
            current_move_vec = vec_zero;
            slide_magnitude = 0;
            break;
        }

        slide_speed_vector = current_move_vec * slide_magnitude;
    }

    void handle_movement()
    {
        //MOVEMENT
        //--------------------------------------------------------------------------------------------------------------

        if(move_vec.sqrMagnitude< 0.01f) return;

        is_climbing = false;
        
        var v_right = Vector3.Cross(vec_up, move_vec);
            
        var v = Vector3.ProjectOnPlane(surface_normal, v_right);
            
        move_vec = Vector3.ProjectOnPlane(move_vec, v).normalized;
        
        var move_dist_left = delta_time * moveSpeed;
        var current_move_vec = move_vec;
        var current_surface_normal = surface_normal;
        var is_avoiding_obstacle = false;
        var iterations = 0;
        var iterations_obstacle = 0;
        var move_dot_mult = 1f;
        
        for(var i=0;i<5;i++)
        {
            iterations += 1;
            
            if (is_avoiding_obstacle)
                iterations_obstacle += 1;
            else
            {
                iterations_obstacle = 0;
            }
        
            var can_move = check_movement(iterations,move_vec, current_move_vec, current_surface_normal, move_dist_left, out var new_move_vec, out var move_dist, out var new_surface_normal, out var will_avoid_obstacle, out var obstacle_dot);

            current_position += move_dist * move_dot_mult * current_move_vec * (1f - Mathf.Clamp01(Vector3.Dot(slide_speed_vector, current_move_vec)));
            
            Debug.Log(move_dist<0);
            
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
        
        var move_surface_dot = Vector3.Dot(current_move_vec_flat, current_surface_normal);
        
        var current_iter_move_vec = iter_num > 1 || move_surface_dot >= 0? current_move_vec : current_move_vec_flat;
        
        var move_back_offset = -moveCastBackStepDistance * current_iter_move_vec;
        
        var move_hit_check = Physics.CapsuleCast(
            capsule_bottom_point + move_back_offset,
            capsule_top_point + move_back_offset,
            collider_radius,
            current_iter_move_vec,
            out var hit_move,
            2,
            raycastMask);

        var move_ceiling_check = Physics.SphereCast(capsule_top_point + moveCastBackStepDistance * vec_down + move_dist * current_move_vec_flat, collider_radius, vec_up, out var hit_ceiling);

        var ceiling_check = Vector3.Dot(vec_up, current_move_vec)<=0 || (!move_ceiling_check || hit_ceiling.distance > moveCastBackStepDistance + obstacleSeparationDistance);
        var move_check = (!move_hit_check || hit_move.distance > left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance);

        var hit_dist = hit_move.distance;

        if (ceiling_check && move_check) return true;

        var hit_norm = !move_check ? hit_move.normal : current_surface_normal;
        hit_norm = !ceiling_check ? hit_ceiling.normal : hit_norm;
        
        hit_dist = !ceiling_check ? 0 : hit_dist;
        
        var move_vec_right = Vector3.Cross(vec_up, current_move_vec);
        
        var move_hit_dot = Vector3.Dot(vec_up, hit_norm);
        
        var is_obstacle = move_hit_dot<0 || move_hit_dot < climb_angle_dot && !step_check2();// || head_check ;

        if (is_obstacle)
        {
            var obstacle_move_sign = Mathf.Sign(Vector3.Dot(hit_norm, move_vec_right));
            
            if (hit_norm.y < -0.999f)
            {
                obstacle_move_sign = Mathf.Sign(Vector3.Dot(hit_norm+current_surface_normal*0.1f, move_vec_right));
            }
            
            var obstacle_move_vec = obstacle_move_sign * Vector3.Cross(hit_norm, current_surface_normal).normalized;
        
            var dot = Vector3.Dot(obstacle_move_vec, initial_move_vec);
            obstacle_dot = dot;
        
            if (dot <= 0)
            {
                move_dist = !ceiling_check ? 0 : Mathf.Max(0,hit_move.distance - moveCastBackStepDistance - obstacleSeparationDistance);
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
        
                var v = Vector3.ProjectOnPlane(new_surface_normal, move_vec_right);
        
                new_move_vec = Vector3.ProjectOnPlane(current_move_vec_flat, v).normalized;
            }
        }
        
        move_dist = Mathf.Max(0,hit_dist - (moveCastBackStepDistance + obstacleSeparationDistance));
        
        return true;

        bool step_check2()
        {
            if (!check_step_capsule())
            {
                return false;
            }
        
            var check_center = check_step_spheres(vec_zero);
            var check_left = check_step_spheres(-move_vec_right*(collider_radius-0.05f));
            var check_right = check_step_spheres(move_vec_right*(collider_radius-0.05f));
        
            if (!check_center || !check_left || !check_right) return false;
            
            is_climbing = true;
            
            return true;
        }

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
        
            return !step_hit_check || hit_step.distance - obstacleSeparationDistance - moveCastBackStepDistance > minStepDepth;
        }
        
        bool check_step_spheres( Vector3 offset)
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
        
            var step_high_dist_check = hit_step_high.distance - collider_radius - moveCastBackStepDistance > minStepDepth;
            return step_high_dist_check;
        }
    }
    
    void check_movement2(Vector3 current_move_vec, Vector3 current_surface_normal, float left_move_dist, out Vector3 new_move_vec, out float move_dist, out Vector3 new_surface_normal, out bool will_avoid_obstacle)
    {
        new_move_vec = current_move_vec;
        move_dist = left_move_dist;
        new_surface_normal = current_surface_normal;
        will_avoid_obstacle = false;
        
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

        if (!move_hit_check || hit_dist > left_move_dist + moveCastBackStepDistance + obstacleSeparationDistance) return;

        var hit_norm = hit_move.normal;

        var move_vec_right = Vector3.Cross(vec_up, current_move_vec);
        
        new_surface_normal = hit_norm;
        
        var v = Vector3.ProjectOnPlane(new_surface_normal, move_vec_right);
        
        new_move_vec = Vector3.ProjectOnPlane(current_move_vec, v).normalized;
        
        //new_move_vec = Vector3.Cross(move_vec_right, new_surface_normal).normalized;
        
        move_dist = Mathf.Max(0, hit_dist - (moveCastBackStepDistance + obstacleSeparationDistance));
    }

}
