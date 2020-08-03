
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Extensions {
    public static class Float3Extensions {
        public static Vector3 ToVector3(this float3 f)
        {
            return new Vector3(f.x, f.y, f.z);
        }
    }
}
