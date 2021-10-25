using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using FM = Unity.Physics.Extensions.ForceMode;
using PCE = Unity.Physics.Extensions.PhysicsComponentExtensions;

public partial class PlayerSystem : SystemBase
{
    EndSimulationEntityCommandBufferSystem m_EndSimulationECBSystem;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_EndSimulationECBSystem = World
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        Log("created");
    }
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Log("started");
    }
    protected static void Log(string msg)
    {
        Debug.Log("PlayerSystem: " + msg);
    }


    protected void UpdateLocation(float dt)
    {
        var x = InputCapture.movementX;
        var y = InputCapture.movementY;

        Entities
          .WithName("Apply_Movement_To_Player")
          .WithAll<MOVEMENT, PhysicsVelocity>()
          .ForEach(
              (ref PhysicsVelocity pv, in PhysicsMass pm, in Translation translation, in Rotation rotation, in Player p) =>
              {
                  float3 impulse = default(float3);
                  PhysicsMass impulseMass;

                  PCE.GetImpulseFromForce(pm, p.speed * new float3(x, 0, y), FM.Force, dt, out impulse, out impulseMass);
                  PCE.ApplyLinearImpulse(ref pv, impulseMass, impulse);
              }).WithBurst().ScheduleParallel();

    }

    protected void UpdateTriggers(float dt, EntityCommandBuffer.ParallelWriter ecb)
    {
        Entities
            .WithName("PlayerTriggerEvents_Scores")
            .WithAll<Player>()
            .ForEach(
                (int entityInQueryIndex, ref Player p, in Entity e,
                 in DynamicBuffer<StatefulTriggerEvent> buf) =>
            {
                foreach (var te in buf)
                {
                    var other = te.GetOther(e);
                    // OnTriggerEnter
                    if (te.State == EventOverlapState.Enter
                        && HasComponent<Pickup>(other))
                    {
                        p.count++;
                        ecb.DestroyEntity(entityInQueryIndex, other);
                    }
                }
            })
            .WithBurst()
            .ScheduleParallel();
    }

    protected override void OnUpdate()
    {
        float dt = Time.DeltaTime;
        // Debug.Log($"Running PlayerSystem after {dt.ToString()} seconds");

        var ecb = m_EndSimulationECBSystem
            .CreateCommandBuffer().AsParallelWriter();

        UpdateLocation(dt);

        UpdateTriggers(dt, ecb);

        // merge ecb
        m_EndSimulationECBSystem.AddJobHandleForProducer(this.Dependency);
    }

}