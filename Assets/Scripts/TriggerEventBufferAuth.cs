using Unity.Entities;
using UnityEngine;

// Add StatefulTriggerEvent buffer to GameObject's Entity
public class TriggerEventBufferAuth : MonoBehaviour, IConvertGameObjectToEntity
{
    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddBuffer<StatefulTriggerEvent>(entity);
    }
}