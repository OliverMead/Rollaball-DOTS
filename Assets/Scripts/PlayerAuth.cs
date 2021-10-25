using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;

using UnityEngine.InputSystem;

// dummy component to indicate the entity gets its movement 
// through the InputProxy and InputCapture
public struct MOVEMENT : IComponentData { }

public class PlayerAuth : MonoBehaviour, IConvertGameObjectToEntity
{
    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSys)
    {
        // dstManager.AddComponent<Player>(entity);
        dstManager.AddComponent<MOVEMENT>(entity);
        Debug.Log("Created player entity");
    }
}
