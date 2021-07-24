using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;

public class LNL_ServerSystem : SystemBase, INetEventListener, INetLogger
{
    private NetManager _netManager;
    private NetDataWriter _writer;
    private Dictionary<int, NetPeer> _peers;

    private PlayerCharacterEntityManager _entitySpawner;

    #region System Methods
#if UNITY_EDITOR
    protected override void OnCreate() { Init(); }

    protected override void OnDestroy() { Shutdown(); }

    protected override void OnUpdate() { UpdateClient(); }
#endif
    #endregion

    #region System Methods Renamed
    void Init()
    {
        NetDebug.Logger = this;
        _writer = new NetDataWriter();
        _netManager = new NetManager(this);
        _netManager.Start(5000);
        _netManager.BroadcastReceiveEnabled = true;
        _netManager.UpdateTime = 15;

        _entitySpawner = new PlayerCharacterEntityManager();
        _peers = new Dictionary<int, NetPeer>();
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
    #endregion

    #region INetEventListener Methods
    public void OnPeerConnected(NetPeer peer)
    {
        _peers.Add(peer.Id, peer);

#if UNITY_EDITOR
        Debug.Log("[SERVER] Connected peers: " + _netManager.ConnectedPeersCount);
        Debug.Log("[SERVER] We have new peer " + peer.EndPoint);
#endif
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
        _entitySpawner.DestroyEntity(peer.Id);
        _peers.Remove(peer.Id);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        NetworkMessagesHandler(peer, reader, deliveryMethod);
    }
    #endregion

    #region INetLogger Methods
    public void WriteNet(NetLogLevel level, string str, params object[] args)
    {
        Debug.LogFormat(str, args);
    }
    #endregion

    #region Incoming Network Messages
    private void NetworkMessagesHandler(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        MessageCode code = (MessageCode)reader.GetInt();

        switch (code)
        {
            case MessageCode.CHAT_MESSAGE:
                ReceiveChatMessage(peer, reader, deliveryMethod);
                break;

            case MessageCode.SPAWN_PLAYER_CHARACTER_ENTITY:
                ReceiveCreatePlayerCharacterMessage(peer, reader, deliveryMethod);
                break;

            case MessageCode.INPUT_STATE:
                ReceiveInputStatesMessage(peer, reader, deliveryMethod);
                break;
        }
    }
    private void ReceiveChatMessage(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        string message = reader.GetString();

        Debug.LogWarning("Chat Mesage received from: " + peer.EndPoint + " with the connection id: " + peer.Id);
        Debug.LogWarning("Mesage: " + message);
    }
    private void ReceiveInputStatesMessage(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) { }
    private void ReceiveCreatePlayerCharacterMessage(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        //TODO, Set spawn points
        float3 pos = new float3(-3f, 5, 0f);
        _entitySpawner.SpawnPlayerCharacterEntity(pos, peer.Id);

        PlayerCharacterEntityMessage playerCharacterEntityMessage = new PlayerCharacterEntityMessage(pos);
        createNetworkPlayerCharacterEntity(peer, playerCharacterEntityMessage);
    }
    #endregion

    #region Other Methods
    private void createNetworkPlayerCharacterEntity(NetPeer peer, PlayerCharacterEntityMessage playerCharacterEntityMessage)
    {
        _writer.Reset();
        _writer.Put((int)playerCharacterEntityMessage.Code);
        _writer.Put(playerCharacterEntityMessage.Position.x);
        _writer.Put(playerCharacterEntityMessage.Position.y);
        _writer.Put(playerCharacterEntityMessage.Position.z);

        peer.Send(_writer, DeliveryMethod.ReliableOrdered);
    }
    #endregion
}
