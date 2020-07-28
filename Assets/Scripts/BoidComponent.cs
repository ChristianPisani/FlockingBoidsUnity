using Assets.Scripts;
using UnityEngine;

public class BoidComponent : MonoBehaviour {
    public float MaxForce = 0.2f;
    public float MaxSpeed = 8f;
    public float MinSpeed = 3f;
    public float PerceptionRadius = 500;

    public Boid Boid;

    void Start()
    {
        if (Boid == null) Spawn();
    }

    public void Update()
    {
        if (Boid == null) return;

        Boid.Update();

        transform.position = Boid.Pos;
        transform.forward = Boid.Vel;        
    }

    public void Spawn()
    {
        Boid = new Boid(transform.position)
        {
            Acl = new Vector3().RandomPoint(Vector3.one),
            MaxForce = MaxForce,
            MaxSpeed = MaxSpeed,
            MinSpeed = MinSpeed,
            PerceptionRadius = PerceptionRadius
        };
    }
}
