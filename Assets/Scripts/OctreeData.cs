using UnityEngine;

namespace Assets.Scripts {
    public struct OctreeData<T> where T : struct {
        public Vector3 Point;
        public T AttachedObject;
    }
}
