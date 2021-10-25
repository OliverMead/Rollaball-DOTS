/* 
Adapted from source of the UnityPhysicsSamples in Unity-Technologies' 
public EntityComponentSystemSamples repository:
https://github.com/Unity-Technologies/EntityComponentSystemSamples/blob/master/UnityPhysicsSamples/Assets/Demos/2.%20Setup/2d.%20Events/2d1.%20Triggers/Scripts/DynamicBufferTriggerEventAuthoring.cs 
*/

using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using System;

public enum EventOverlapState
{
    Enter, Stay, Exit
}

public struct ExcludeTriggerEventConversion : IComponentData { }

// Collision members + state 
public struct StatefulTriggerEvent : IBufferElementData, IComparable<StatefulTriggerEvent>
{
    internal EntityPair Entities;
    internal BodyIndexPair BodyIndices;
    internal ColliderKeyPair ColliderKeys;

    public EventOverlapState State;
    public Entity EntityA => Entities.EntityA;
    public Entity EntityB => Entities.EntityB;
    public int BodyIndexA => BodyIndices.BodyIndexA;
    public int BodyIndexB => BodyIndices.BodyIndexB;
    public ColliderKey ColliderKeyA => ColliderKeys.ColliderKeyA;
    public ColliderKey ColliderKeyB => ColliderKeys.ColliderKeyB;

    public int CompareTo(StatefulTriggerEvent other)
    {
        var cmp = EntityA.CompareTo(other.EntityA);
        if (cmp != 0) return cmp;
        cmp = EntityB.CompareTo(other.EntityB);
        if (cmp != 0) return cmp;
        if (ColliderKeyA.Value != other.ColliderKeyA.Value)
            // return ColliderKeyA.Value < other.ColliderKeyA.Value ? -1 : 1;
            return math.select(1, -1, ColliderKeyA.Value < other.ColliderKeyA.Value);
        if (ColliderKeyB.Value != other.ColliderKeyB.Value)
            // return ColliderKeyB.Value < other.ColliderKeyB.Value ? -1 : 1;
            return math.select(1, -1, ColliderKeyB.Value < other.ColliderKeyB.Value);
        return 0;
    }

    public Entity GetOther(Entity entity)
    {
        Assert.IsTrue((entity == EntityA) || (entity == EntityB));
        int2 iv = math.select(new int2(EntityB.Index, EntityB.Version),
            new int2(EntityA.Index, EntityA.Version), entity == EntityB);
        return new Entity { Index = iv[0], Version = iv[1] };
    }

    public StatefulTriggerEvent(Entity entityA, Entity entityB, int bodyIndexA
      , int bodyIndexB, ColliderKey colliderKeyA, ColliderKey colliderKeyB)
    {
        Entities = new EntityPair { EntityA = entityA, EntityB = entityB };
        BodyIndices = new BodyIndexPair { BodyIndexA = bodyIndexA, BodyIndexB = bodyIndexB };
        ColliderKeys = new ColliderKeyPair { ColliderKeyA = colliderKeyA, ColliderKeyB = colliderKeyB };
        State = default;
    }

    public StatefulTriggerEvent(TriggerEvent triggerEvent)
        : this(triggerEvent.EntityA, triggerEvent.EntityB,
               triggerEvent.BodyIndexA, triggerEvent.BodyIndexB,
               triggerEvent.ColliderKeyA, triggerEvent.ColliderKeyB
           )
    { }
}

// attributes similar to FixedUpdate for MonoBehaviours
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(StepPhysicsWorld))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
[AlwaysUpdateSystem]
public class CollisionSystem : SystemBase
{
    public JobHandle OutDependency => base.Dependency;

    [BurstCompile]
    private struct CollectTriggerEvents : ITriggerEventsJob
    {
        public NativeList<StatefulTriggerEvent> TriggerEvents;

        // THIS is where we 'handle' the collision by adding it to the list
        public void Execute(Unity.Physics.TriggerEvent triggerEvent)
        {
            TriggerEvents.Add(new StatefulTriggerEvent(triggerEvent));
            // Debug.Log($"Trigger event between entities { triggerEvent.EntityA.Index } and { triggerEvent.EntityB.Index }");
        }
    }

    private BuildPhysicsWorld m_BuildPhysicsWorld;
    private StepPhysicsWorld m_StepPhysicsWorld;
    private EndFramePhysicsSystem m_EndFramePhysicsSystem;
    private EntityQuery m_Query;

    private NativeList<StatefulTriggerEvent> m_PreviousFrameTriggerEvents;
    private NativeList<StatefulTriggerEvent> m_CurrentFrameTriggerEvents;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();

        m_Query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(StatefulTriggerEvent) },
            None = new ComponentType[] { typeof(ExcludeTriggerEventConversion) }
        });

        m_PreviousFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Persistent);
        m_CurrentFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    { // Clean up
        m_PreviousFrameTriggerEvents.Dispose();
        m_CurrentFrameTriggerEvents.Dispose();
    }

    // swap the event lists' references
    // then clear the 'current' list
    // for the this frame to populate 
    protected void FrameStartEventMove()
    {
        var tmp = m_PreviousFrameTriggerEvents;
        m_PreviousFrameTriggerEvents = m_CurrentFrameTriggerEvents;
        m_CurrentFrameTriggerEvents = tmp;
        m_CurrentFrameTriggerEvents.Clear();
    }

    protected static void AddTriggerEventsToBuffers(
        NativeList<StatefulTriggerEvent> triggerEventList,
        ref BufferFromEntity<StatefulTriggerEvent> bufferFromEntity,
        NativeHashMap<Entity, byte> entitiesWithTriggerBuffers
    )
    {
        foreach (var triggerEvent in triggerEventList)
        { // add the event to the buffers of the entities involved
            if (entitiesWithTriggerBuffers.ContainsKey(triggerEvent.EntityA))
                bufferFromEntity[triggerEvent.EntityA].Add(triggerEvent);
            if (entitiesWithTriggerBuffers.ContainsKey(triggerEvent.EntityB))
                bufferFromEntity[triggerEvent.EntityB].Add(triggerEvent);
        }
    }

    // take events from previous and current frame,
    // fill into resultList with correct State set
    public static void UpdateTriggerEventState(
        in NativeList<StatefulTriggerEvent> pframeEvents,
        in NativeList<StatefulTriggerEvent> cframeEvents,
        ref NativeList<StatefulTriggerEvent> resultList
    )
    {
        int i = 0, j = 0;
        while (i < cframeEvents.Length && j < pframeEvents.Length)
        {
            var cfe = cframeEvents[i];
            var pfe = pframeEvents[j];
            var cmp = cfe.CompareTo(pfe);

            if (cmp == 0)
            { // event exists in current and previous frame
                cfe.State = EventOverlapState.Stay;
                resultList.Add(cfe);
                i++; j++;
            }
            else if (cmp < 0)
            { // current but not previous
                cfe.State = EventOverlapState.Enter;
                resultList.Add(cfe);
                i++;
            }
            else
            { // previous but not current
                pfe.State = EventOverlapState.Exit;
                resultList.Add(pfe);
                j++;
            }
        }

        if (i == cframeEvents.Length)
        { // fewer events this frame than last
            while (j < pframeEvents.Length)
            { // remaining previous frame events change to Exit
                var triggerEvent = pframeEvents[j++];
                triggerEvent.State = EventOverlapState.Exit;
                resultList.Add(triggerEvent);
            }
        }
        else if (j == pframeEvents.Length)
        { // more events this fram than last
            while (i < cframeEvents.Length)
            { // remaining current frame events change to Enter
                var triggerEvent = cframeEvents[i++];
                triggerEvent.State = EventOverlapState.Enter;
                resultList.Add(triggerEvent);
            }
        }
    }

    // + Clear all trigger event buffers
    // + move current frame triggers to previous
    // + collect trigger events for the frame
    // + collect entities with a trigger event buffer
    // + update the States of the trigger events
    // + add the trigger events to the respective buffers
    protected override void OnUpdate()
    {
        // Debug.Log("Collision System update");
        if (m_Query.CalculateEntityCount() == 0)
            return; // no buffers to populate

        // Debug.Log("Collision System update found entities");

        // update the system's dependencies to include the physics simulation
        Dependency = JobHandle.CombineDependencies(m_StepPhysicsWorld.FinalSimulationJobHandle, Dependency);

        Entities
            .WithName("Clear_Trigger_Event_Buffers")
            .WithNone<ExcludeTriggerEventConversion>()
            .ForEach((ref DynamicBuffer<StatefulTriggerEvent> buf) => buf.Clear())
            .WithBurst()
            .ScheduleParallel();

        FrameStartEventMove();

        // bring references into local scope for the lambdas
        var cfte = m_CurrentFrameTriggerEvents;
        var pfte = m_PreviousFrameTriggerEvents;

        BufferFromEntity<StatefulTriggerEvent> triggerEventBufferFromEntity =
            GetBufferFromEntity<StatefulTriggerEvent>();

        var physicsWorld = m_BuildPhysicsWorld.PhysicsWorld;

        var teCollectJob = new CollectTriggerEvents
        { /// collect trigger events into the current frame's list
            TriggerEvents = cfte
        };

        var collectJobHandle = teCollectJob.Schedule(m_StepPhysicsWorld.Simulation, ref physicsWorld, Dependency);

        // HashSet, value will result in 0
        NativeHashMap<Entity, byte> entityBuffersMap = new NativeHashMap<Entity, byte>(0, Allocator.TempJob);
        // populate entityBuffersMap with all entities that 
        // have a trigger event buffer
        var collectBuffers = Entities
            .WithNone<ExcludeTriggerEventConversion>()
            .ForEach((Entity e, ref DynamicBuffer<StatefulTriggerEvent> buf)
                => entityBuffersMap.Add(e, 0))
            .WithBurst()
            .Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(collectJobHandle, collectBuffers);

        Job // With current frame triggers gathered, populate the buffers
            .WithName("Convert_Trigger_Event_Stream_to_Dynamic_Buffers")
            .WithCode(() =>
            {
                cfte.Sort();
                var triggerEventsWithStates = new NativeList<StatefulTriggerEvent>(cfte.Length, Allocator.Temp);
                UpdateTriggerEventState(in pfte, in cfte, ref triggerEventsWithStates);
                AddTriggerEventsToBuffers(triggerEventsWithStates, ref triggerEventBufferFromEntity, entityBuffersMap);
            })
            .WithBurst()
            .Schedule();

        m_EndFramePhysicsSystem.AddInputDependency(Dependency);
        entityBuffersMap.Dispose(Dependency);
    }
}
