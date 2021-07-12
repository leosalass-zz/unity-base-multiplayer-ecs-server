using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class LNL_ServerSystem : SystemBase, INetEventListener, INetLogger
{
    private NetManager _netManager;
    private NetPeer _ourPeer;
    private NetDataWriter _dataWriter;

    private PlayerCharacterEntitySpawner _entitySpawner;

#if UNITY_EDITOR
    protected override void OnCreate() { Init(); }

    protected override void OnDestroy() { Shutdown(); }

    protected override void OnUpdate() { UpdateClient(); }
#endif

    void Init()
    {
        NetDebug.Logger = this;
        _dataWriter = new NetDataWriter();
        _netManager = new NetManager(this);
        _netManager.Start(5000);
        _netManager.BroadcastReceiveEnabled = true;
        _netManager.UpdateTime = 15;

        _entitySpawner = new PlayerCharacterEntitySpawner();
    }

    void UpdateClient()
    {
        _netManager.PollEvents();
    }

    void Shutdown()
    {
        NetDebug.Logger = null;
        if (_netManager != null)
            _netManager.Stop();
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log("[SERVER] We have new peer " + peer.EndPoint);
        _ourPeer = peer;

        float3 pos = new float3(2f, 0, 4f);
        _entitySpawner.SpawnPlayerCharacterEntity(pos);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        Debug.Log("[SERVER] error " + socketErrorCode);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.Broadcast)
        {
            Debug.Log("[SERVER] Received discovery request. Send discovery response");
            NetDataWriter resp = new NetDataWriter();
            resp.Put(1);
            _netManager.SendUnconnectedMessage(resp, remoteEndPoint);
        }
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey("sample_app");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("[SERVER] peer disconnected " + peer.EndPoint + ", info: " + disconnectInfo.Reason);
        if (peer == _ourPeer)
            _ourPeer = null;
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        MessageCode code = (MessageCode)reader.GetInt();
        string msg = reader.GetString();

        Debug.LogWarning("Mesage received from " + peer.EndPoint);
        Debug.LogWarning("Mesage type: " + code);
        Debug.LogWarning("Mesage: " + msg);
    }

    public void WriteNet(NetLogLevel level, string str, params object[] args)
    {
        Debug.LogFormat(str, args);
    }
}
