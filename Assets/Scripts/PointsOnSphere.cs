using UnityEngine;
using System.Collections.Generic;

public class PointsOnSphere : MonoBehaviour {
    public int n = 128;
    public float scale = 10;
    public int highlightUpTo = 10;

    void Start()
    {        
        Vector3[] pts = SpherePoints(n);
        List<GameObject> uspheres = new List<GameObject>();
        
        for (int i = 0; i < pts.Length; i++)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            if(i < highlightUpTo)
            {
                gameObject.GetComponent<Renderer>().material.color = Color.green;
            }

            uspheres.Add(gameObject);


            uspheres[i].transform.parent = transform;
            uspheres[i].transform.position = transform.position + pts[i] * scale;
        }
    }

    Vector3[] SpherePoints(int n)
    {
        List<Vector3> upts = new List<Vector3>();
        float inc = Mathf.PI * (3 - Mathf.Sqrt(5));
        float off = 2.0f / n;
        float x = 0;
        float y = 0;
        float z = 0;
        float r = 0;
        float phi = 0;

        for (var k = 0; k < n; k++)
        {
            y = k * off - 1 + (off / 2);
            r = Mathf.Sqrt(1 - y * y);
            phi = k * inc;
            x = Mathf.Cos(phi) * r;
            z = Mathf.Sin(phi) * r;

            upts.Add(new Vector3(x, y, z));
        }
        Vector3[] pts = upts.ToArray();
        return pts;
    }
}