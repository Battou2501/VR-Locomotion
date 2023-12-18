using UnityEngine;

namespace DefaultNamespace
{
    public static class Vector3Extensions
    {
        public static Vector3 flattened(this Vector3 v)
        {
            return new Vector3(v.x, 0, v.z);
        }
        
        public static Vector3 flattened_normalized(this Vector3 v)
        {
            return new Vector3(v.x, 0, v.z).normalized;
        }

    }
}