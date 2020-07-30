using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public struct Boid {
    public Vector3 PrevPos;
    Vector3 CurPos;
    public Vector3 Pos;
    public Vector3 Vel;
    public Vector3 Acl;

    public float AvgSpeedMod;
    public float CohesionMod;
    public float SeparationMod;

    public float MaxSpeed;
    public float MinSpeed;
    public float MaxForce;
    public float PerceptionRadius;

    public Matrix4x4 matrix(Quaternion ModelRotation, Vector3 Scale)
    {
        var lookRotation = Vel != Vector3.zero ? Quaternion.LookRotation(Vel.normalized, Vector3.up) : Quaternion.identity;
        
        return Matrix4x4.TRS(
                    Pos,
                    lookRotation * ModelRotation,
                    Scale);
    }

    public void Update()
    {
        Vel = Vector3.ClampMagnitude(Vel, MaxSpeed);
        //Acl = Vector3.ClampMagnitude(Acl, MaxForce);

        PrevPos = Pos;
        CurPos = Pos;
        Pos += Vel * Time.deltaTime;

        Vel += Acl;
        Acl = Vector3.zero;

        if (Vel.magnitude < MinSpeed)
        {
            if (Vel == Vector3.zero)
            {
                Vel = Vector3.one.RandomPoint(Vector3.one);
            }
            else
            {
                Vel = Vel.normalized * MinSpeed;
            }
        }
    }

    public Vector3 SteeringForce(List<Boid> others)
    {
        Vector3 avgSpeed = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 seperation = Vector3.zero;

        int total = 0;

        foreach (Boid boid in others)
        {
            float distance = Vector3.Distance(Pos, boid.Pos);
            if (!boid.Equals(this) && distance < PerceptionRadius && distance > 0)
            {
                avgSpeed += boid.Vel;
                cohesion += boid.Pos;

                Vector3 diff = Pos - boid.Pos;
                diff /= distance;
                seperation += diff;

                total++;
            }
        }

        if (total > 0)
        {
            avgSpeed /= total;
            avgSpeed -= Vel;

            cohesion /= total;
            cohesion -= Pos;

            seperation /= total;
        }

        avgSpeed.Normalize();
        cohesion.Normalize();
        seperation.Normalize();

        avgSpeed *= AvgSpeedMod;
        cohesion *= CohesionMod;
        seperation *= SeparationMod;

        return avgSpeed + cohesion + seperation;
    }

    public Vector3 SteeringForce(List<OctreeData<Boid>> others)
    {
        Vector3 avgSpeed = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        Vector3 seperation = Vector3.zero;

        int total = 0;

        for (int i = 0; i < others.Count; i++)
        {
            var boid = others[i].AttachedObject;

            Vector3 diff = Pos - boid.Pos;
            float distance = diff.magnitude;

            if (!boid.Equals(this) && distance > 0 && distance < PerceptionRadius)
            {
                avgSpeed += boid.Vel;
                cohesion += boid.Pos;

                diff /= distance;
                seperation += diff;

                total++;
            }
        }

        if (total > 0)
        {
            avgSpeed /= total;
            avgSpeed -= Vel;

            cohesion /= total;
            cohesion -= Pos;

            seperation /= total;
        }

        avgSpeed *= AvgSpeedMod;
        cohesion *= CohesionMod;
        seperation *= SeparationMod;

        return avgSpeed + cohesion + seperation;
    }

    public void Spawn()
    {
        while (Acl.magnitude < MinSpeed)
        {
            Acl = new Vector3().RandomPoint(Vector3.one * MaxSpeed);
        }
    }
}
