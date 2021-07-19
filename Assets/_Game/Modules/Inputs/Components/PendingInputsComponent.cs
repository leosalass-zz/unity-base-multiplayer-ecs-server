using Unity.Entities;

[GenerateAuthoringComponent]
public struct PendingInputsComponent : IComponentData
{
    public int prediction;
}