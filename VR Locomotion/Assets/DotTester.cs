using UnityEngine;
using UnityEngine.PlayerLoop;

namespace DefaultNamespace
{
    public class DotTester : MonoBehaviour
    {
        public Transform obj1;
        public Transform obj2;

        void Update()
        {
            var du1 = Mathf.Max(-1, Vector3.Dot(Vector3.down, obj1.forward));
            var du2 = Mathf.Max(-1, Vector3.Dot(Vector3.down, obj2.forward));
            
            var vr = Vector3.Cross(obj2.forward, obj1.forward).normalized;
            var vn = Vector3.Cross(obj1.forward, vr).normalized;

            //var d = (1f - Mathf.Max(0,Vector3.Dot(vn, obj2.forward))) * (Vector3.Dot(obj1.forward, obj2.forward) > 0 ? 1 : 0) * Mathf.Clamp01((1f - Mathf.Max(0, Vector3.Dot(Vector3.up, obj2.forward)))/du);
            //var d = (1f - Mathf.Max(0, Vector3.Dot(vn, obj2.forward))) * (Vector3.Dot(obj1.forward, obj2.forward) > 0 ? 1 : 0);
            
            Debug.Log(Mathf.Round(du1-du2));
            Debug.DrawRay(obj1.position,obj1.forward*10,Color.red);
            Debug.DrawRay(obj1.position,vr*10,Color.yellow);
            Debug.DrawRay(obj1.position,vn*10,Color.green);
            Debug.DrawRay(obj1.position,Vector3.up*10,Color.magenta);
            Debug.DrawRay(obj2.position,obj2.forward*10,Color.blue);
        }
    }
}