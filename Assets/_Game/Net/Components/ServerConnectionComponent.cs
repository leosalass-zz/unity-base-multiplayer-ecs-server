using Unity.Entities;
using Unity.Networking.Transport;

public struct ServerConnectionComponent : IComponentData
{
    public NetworkConnection connection;
}
