using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.IO;
using Assets.Code;
using UnityEngine.Events;
using Unity.Jobs;
using UnityEditor;

public class ServerBehaviour : MonoBehaviour
{
    private NetworkDriver networkDriver;
    private NativeList<NetworkConnection> connections;

    private JobHandle networkJobHandle;

    private Queue<MessageHeader> messagesQueue;

    public MessageEvent[] ServerCallbacks = new MessageEvent[(int)MessageHeader.MessageType.Count - 1];

    void Start()
    {
        networkDriver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 9000;
        if(networkDriver.Bind(endpoint) != 0)
        {
            Debug.Log("Failed to bind port");
        }
        else
        {
            networkDriver.Listen();
        }

        connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        messagesQueue = new Queue<MessageHeader>();

        for (int i = 0; i < ServerCallbacks.Length; i++)
        {
            ServerCallbacks[i] = new MessageEvent();
        }
        ServerCallbacks[(int)MessageHeader.MessageType.SetName].AddListener(HandleSetName);
    }

    private void HandleSetName(MessageHeader message)
    {
        Debug.Log($"Got a name: {(message as SetNameMessage).Name}");
    }

    void Update()
    {
        networkJobHandle.Complete();

        for(int i = 0; i < connections.Length; ++i)
        {
            if(!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        NetworkConnection c;
        while((c = networkDriver.Accept()) != default)
        {
            connections.Add(c);
            Debug.Log("Accepted connection");

            var colour = (Color32)Color.magenta;
            var message = new WelcomeMessage
            {
                PlayerID = c.InternalId,
                Colour = ((uint)colour.r << 24) | ((uint)colour.g << 16) | ((uint)colour.b << 8) | colour.a
            };

            var writer = networkDriver.BeginSend(c);
            message.SerializeObject(ref writer);
            networkDriver.EndSend(writer);
        }

        DataStreamReader reader;
        for(int i = 0; i < connections.Length; ++i)
        {
            if (!connections[i].IsCreated) continue;

            NetworkEvent.Type cmd;
            while((cmd = networkDriver.PopEventForConnection(connections[i], out reader)) != NetworkEvent.Type.Empty)
            {
                if(cmd == NetworkEvent.Type.Data)
                {
                    var messageType = (MessageHeader.MessageType)reader.ReadUShort();
                    switch (messageType)
                    {
                        case MessageHeader.MessageType.None:
                            break;
                        case MessageHeader.MessageType.NewPlayer:
                            break;
                        case MessageHeader.MessageType.Welcome:
                            break;
                        case MessageHeader.MessageType.SetName:
                            var message = new SetNameMessage();
                            message.DeserializeObject(ref reader);
                            messagesQueue.Enqueue(message);
                            break;
                        case MessageHeader.MessageType.RequestDenied:
                            break;
                        case MessageHeader.MessageType.PlayerLeft:
                            break;
                        case MessageHeader.MessageType.StartGame:
                            break;
                        case MessageHeader.MessageType.Count:
                            break;
                        default:
                            break;
                    }
                }
                else if(cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected");
                    connections[i] = default;
                }
            }
        }

        networkJobHandle = networkDriver.ScheduleUpdate();

        ProcessMessagesQueue();
    }

    private void ProcessMessagesQueue()
    {
        while(messagesQueue.Count > 0)
        {
            var message = messagesQueue.Dequeue();
            ServerCallbacks[(int)message.Type].Invoke(message);
        }
    }

    private void OnDestroy()
    {
        networkDriver.Dispose();
        connections.Dispose();
    }
}
