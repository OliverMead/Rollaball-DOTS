using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

// Component added by ObjectFollowAuth on an entity
// Object will follow the original entity
public class FollowEntity : MonoBehaviour
{

    public Entity entity;
    public float3 offset = float3.zero;

    private EntityManager manager;

    void Start()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entityPosition = manager.GetComponentData<Translation>(entity);
        offset = new float3(transform.position) - entityPosition.Value;
    }

    void LateUpdate()
    {
        var entityPosition = manager.GetComponentData<Translation>(entity);
        transform.position = entityPosition.Value + offset;
    }

}
