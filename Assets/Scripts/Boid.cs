using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public class Boid {
    public Vector3 Pos;
    public Vector3 Vel;
    public Vector3 Acl;

    public float AvgSpeedMod = 1f;
    public float CohesionMod = 1f;
    public float SeparationMod = 1f;

    public float MaxForce = 0.2f;
    public float MaxSpeed = 8f;
    public float MinSpeed = 3f;
    public float PerceptionRadius = 5f;

    public Boid(Vector3 pos)
    {
        Pos = pos;
        Vector3 rndAcl = new Vector3().RandomPoint(Vector3.one * MaxSpeed) + Vector3.one * MinSpeed;
    }

    public void Update()
    {
        Vel = Vector3.ClampMagnitude(Vel, MaxSpeed);
        Acl = Vector3.ClampMagnitude(Acl, MaxForce);

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
            if (boid != this && distance < PerceptionRadius)
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

        if (avgSpeed.magnitude > 0)
        {
            avgSpeed.Normalize();
        }
        if (cohesion.magnitude > 0)
        {
            cohesion.Normalize();
        }
        avgSpeed *= MaxSpeed;
        avgSpeed = Vector3.ClampMagnitude(avgSpeed, MaxForce);

        cohesion *= MaxSpeed;
        cohesion = Vector3.ClampMagnitude(cohesion, MaxForce);

        seperation = Vector3.ClampMagnitude(seperation, MaxForce);

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

        foreach (OctreeData<Boid> octreeData in others)
        {
            var boid = octreeData.AttachedObject;

            float distance = Vector3.Distance(Pos, boid.Pos);
            if (boid != this && distance < PerceptionRadius)
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

        if (avgSpeed.magnitude > 0)
        {
            avgSpeed.Normalize();
        }
        if (cohesion.magnitude > 0)
        {
            cohesion.Normalize();
        }
        avgSpeed *= MaxSpeed;
        avgSpeed = Vector3.ClampMagnitude(avgSpeed, MaxForce);

        cohesion *= MaxSpeed;
        cohesion = Vector3.ClampMagnitude(cohesion, MaxForce);

        seperation = Vector3.ClampMagnitude(seperation, MaxForce);

        avgSpeed *= AvgSpeedMod;
        cohesion *= CohesionMod;
        seperation *= SeparationMod;

        return avgSpeed + cohesion + seperation;
    }
}
