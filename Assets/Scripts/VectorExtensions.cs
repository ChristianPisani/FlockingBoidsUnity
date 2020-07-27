using UnityEngine;

namespace Assets.Scripts {
    public static class VectorExtensions {
        public static Vector3 RandomPoint(this Vector3 origin, Vector3 bounds)
        {
            return origin + new Vector3(
                (Random.value - .5f) * bounds.x,
                (Random.value - .5f) * bounds.y,
                (Random.value - .5f) * bounds.z
            );
        }
    }
}