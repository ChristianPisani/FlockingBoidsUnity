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
        if (Boid.Equals(default(Boid))) Spawn();
    }

    public void Update()
    {        
        if (Boid.Equals(default(Boid))) return;

        Boid.Update();

        transform.position = Boid.Pos;
        transform.forward = Boid.Vel;
    }

    public void Spawn()
    {
        Boid = new Boid()
        {
            Pos = transform.position,
            Acl = Vector3.zero,
            MaxSpeed = MaxSpeed,
            MinSpeed = MinSpeed,
            MaxForce = MaxForce,
            PerceptionRadius = PerceptionRadius
        };

        while(Boid.Acl.magnitude < MinSpeed)
        {
            Boid.Acl = new Vector3().RandomPoint(Vector3.one * MaxSpeed);
        }
    }
}
