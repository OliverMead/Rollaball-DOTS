using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public partial class RotationSystem : SystemBase
{

    private static void Log(string msg)
    {
        Debug.Log("RotationSystem: " + msg);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        Log("created");
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Log("started");
    }

    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        Entities
          .WithAll<ROTATE, Rotation>()
          .ForEach((ref Rotation rotation, in ROTATE change) =>
          {
              var speed = change.speed * dt;
              var rot = change.rotationVector;

              // rotate through the Euler vector in the same way as transform.Rotate()
              rotation.Value = math.mul(
                  math.normalize(rotation.Value)
                , quaternion.EulerZXY(rot * speed)
                );
          })
          .WithBurst()
          .ScheduleParallel();
    }
}