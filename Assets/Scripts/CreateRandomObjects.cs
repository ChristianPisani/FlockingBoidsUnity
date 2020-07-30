using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreateRandomObjects : MonoBehaviour {
    public GameObject GameObject;
    Mesh Mesh;
    Renderer Renderer;
    public int Amount;

    Bounds Bounds;

    List<Matrix4x4> positions = new List<Matrix4x4>();

    private void Start()
    {
        Bounds = GetComponent<BoxCollider>().bounds;

        Mesh = GameObject.GetComponent<MeshFilter>().mesh;
        Renderer = GameObject.GetComponent<Renderer>();

        for (int i = 0; i < Amount; i++)
        {
            //var spawned = Instantiate(GameObject);
            //spawned.transform.position = Bounds.center.RandomPoint(Bounds.size);

            var newTransform = new Matrix4x4();

            newTransform = Matrix4x4.Translate(Bounds.center.RandomPoint(Bounds.size));

            positions.Add(newTransform);
        }
    }

    private void Update()
    {
        var drawAmount = 1000;

        for (int i = 0; i < positions.Count; i += drawAmount)
        {
            Graphics.DrawMeshInstanced(Mesh, 
                0, 
                Renderer.material, 
                positions.Skip(i).Take(drawAmount).ToArray(), 
                drawAmount,
                new MaterialPropertyBlock(),
                UnityEngine.Rendering.ShadowCastingMode.On,
                true);
        }
    }
}
