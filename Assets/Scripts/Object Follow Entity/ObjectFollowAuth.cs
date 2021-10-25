using Unity.Entities;
using UnityEngine;

public class ObjectFollowAuth : MonoBehaviour, IConvertGameObjectToEntity
{
    [Tooltip("Object whose position you wish to follow this entity's translation")]
    public GameObject follower;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSys)
    {
        FollowEntity followerComponent = follower.GetComponent<FollowEntity>();

        if (followerComponent == null)
            followerComponent = follower.AddComponent<FollowEntity>();

        followerComponent.entity = entity;
        followerComponent.enabled = true;
    }
}