using UnityEngine;
using Assets.Scripts;
using System.Collections;
using System;

[RequireComponent(typeof(OctreeGameObject))]
public class InsertPoints : MonoBehaviour {
    public int Amount = 150;

    OctreeGameObject Octree;

    void Start()
    {
        Octree = GetComponent<OctreeGameObject>();

        StartCoroutine(WaitForSecondsThen(0.5f, InsertRandomPoints));
    }

    void InsertRandomPoints()
    {        
        for (int i = 0; i < Amount; i++)
        {
            var point = Octree.Octree.Bounds.center.RandomPoint(Octree.Octree.Bounds.size);
            Octree.Octree.InsertPoint(point);
        }
    }

    public static IEnumerator WaitForSecondsThen(float seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);

        action();
    }
}
