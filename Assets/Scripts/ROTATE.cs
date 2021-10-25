using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct ROTATE : IComponentData
{
    public float speed;

    public float3 rotationVector;
}
