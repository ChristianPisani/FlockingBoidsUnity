using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Extensions {
    public static class Vector3Extensions {
        public static float3 ToFloat3(this Vector3 v)
        {
            return new float3(v.x, v.y, v.z);
        }
    }
}
