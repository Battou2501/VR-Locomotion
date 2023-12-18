using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace DefaultNamespace
{
    [ExecuteAlways]
    public class SlideMoveVectorsTest : MonoBehaviour
    {
        public Transform slideVectorOrigin;
        public float slideVectorMagnitude;
        public Transform moveVectorOrigin;
        public float moveVectorMagnitude;
        public float moveVectorMaxMagnitude;


        void Update()
        {
            
            
            var slide_vector = slideVectorOrigin.forward * slideVectorMagnitude;
            var move_vector = moveVectorOrigin.forward*moveVectorMagnitude;
            
            Debug.DrawRay(slideVectorOrigin.position, slide_vector,Color.blue);
            Debug.DrawRay(moveVectorOrigin.position+slideVectorOrigin.up*0.0f, move_vector,Color.green);

            var combined_vector = slide_vector + move_vector;
            
            //Debug.DrawRay(moveVectorOrigin.position-slideVectorOrigin.up*0.03f, combined_vector,Color.red);
            
            
            //Debug.DrawRay(slideVectorOrigin.position-slideVectorOrigin.up*0.01f, combine_move_with_slide(move_vector, slide_vector, slideVectorOrigin.up) + slide_vector,Color.yellow);

            combine_move_with_slide(move_vector, slide_vector, slideVectorOrigin.up);
        }

        void combine_move_with_slide(Vector3 move_vector, Vector3 slide_vector, Vector3 surface_normal)
        {
            //var project_move_to_slide_up_plane = Vector3.ProjectOnPlane(move_vector, surface_normal);
            //var project_move_to_slide_plane = Vector3.ProjectOnPlane(project_move_to_slide_up_plane, slide_vector);

            var perpendicular_part = Vector3.ProjectOnPlane(move_vector, slide_vector);
            var parallel_part = Vector3.Project(move_vector, slide_vector);

            parallel_part -= Vector3.ClampMagnitude(slide_vector, parallel_part.magnitude);
            
            //Debug.DrawRay(moveVectorOrigin.position-slideVectorOrigin.up*0.03f, Vector3.ProjectOnPlane(move_vector, slide_vector),Color.red);
            //Debug.DrawRay(moveVectorOrigin.position-slideVectorOrigin.up*0.03f, Vector3.Project(move_vector, slide_vector),Color.yellow);
            
            //Debug.DrawRay(moveVectorOrigin.position-slideVectorOrigin.up*0.03f, Vector3.Project(move_vector, slide_vector)+Vector3.ProjectOnPlane(move_vector, slide_vector),Color.magenta);
            
            //Debug.DrawRay(slideVectorOrigin.position + slideVectorOrigin.forward*slideVectorMagnitude, perpendicular_part+parallel_part,Color.magenta);
            
            if(Vector3.Dot(slide_vector, move_vector-slide_vector)<=0)
                Debug.DrawRay(slideVectorOrigin.position + slideVectorOrigin.forward*slideVectorMagnitude, perpendicular_part,Color.magenta);
            else
                Debug.DrawRay(slideVectorOrigin.position + slideVectorOrigin.forward*slideVectorMagnitude, move_vector-slide_vector,Color.magenta);
            
            //var new_vec = Vector3.Project(project_move_to_slide_up_plane, slide_vector);
            //new_vec = Vector3.ClampMagnitude(slide_vector + new_vec, Mathf.Max(moveVectorMaxMagnitude, slide_vector.magnitude)) - slide_vector + project_move_to_slide_plane;

            //return new_vec;
            
            
            //Debug.Log(Vector3.Dot(slide_vector, move_vector-slide_vector));
        }
    }
}