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
            Debug.DrawRay(moveVectorOrigin.position+slideVectorOrigin.up*0.01f, move_vector,Color.green);

            var combined_vector = slide_vector + move_vector;
            
            Debug.DrawRay(moveVectorOrigin.position-slideVectorOrigin.up*0.03f, combined_vector,Color.red);

            //var project_slide_to_move = Vector3.Project(slide_vector, move_vector);
            //var project_move_to_slide_up_plane = Vector3.ProjectOnPlane(move_vector, slideVectorOrigin.up);
            //var project_move_to_slide_plane = Vector3.ProjectOnPlane(project_move_to_slide_up_plane, slide_vector);
            //var project_move_to_slide = Vector3.Project(project_move_to_slide_up_plane, slide_vector);
            //project_move_to_slide = Vector3.ClampMagnitude(slide_vector + project_move_to_slide, Mathf.Max(moveVectorMaxMagnitude, slide_vector.magnitude));
            //
            //var new_vec = project_move_to_slide_plane + project_move_to_slide;
            
            
            
            Debug.DrawRay(slideVectorOrigin.position-slideVectorOrigin.up*0.01f, combine_move_with_slide(move_vector, slide_vector, slideVectorOrigin.up) + slide_vector,Color.yellow);
            

        }

        Vector3 combine_move_with_slide(Vector3 move_vector, Vector3 slide_vector, Vector3 surface_normal)
        {
            var project_move_to_slide_up_plane = Vector3.ProjectOnPlane(move_vector, surface_normal);
            var project_move_to_slide_plane = Vector3.ProjectOnPlane(project_move_to_slide_up_plane, slide_vector);
            var new_vec = Vector3.Project(project_move_to_slide_up_plane, slide_vector);
            new_vec = Vector3.ClampMagnitude(slide_vector + new_vec, Mathf.Max(moveVectorMaxMagnitude, slide_vector.magnitude)) - slide_vector + project_move_to_slide_plane;

            return new_vec;
        }
    }
}