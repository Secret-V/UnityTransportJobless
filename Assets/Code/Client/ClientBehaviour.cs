using UnityEngine;
using System.Collections;
using Unity.Networking.Transport;
using System.IO;
using Assets.Code;

public class ClientBehaviour : MonoBehaviour
{
    private NetworkDriver networkDriver;
    private NetworkConnection connection;

    // Use this for initialization
    void Start()
    {
        networkDriver = NetworkDriver.Create();
        connection = default;

        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = 9000;
        connection = networkDriver.Connect(endpoint);
    }

    // Update is called once per frame
    void Update()
    {
        networkDriver.ScheduleUpdate().Complete();

        if(!connection.IsCreated)
        {
            return;
        }

        DataStreamReader reader;
        NetworkEvent.Type cmd;
        while((cmd = connection.PopEvent(networkDriver, out reader)) != NetworkEvent.Type.Empty)
        {
            if(cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("Connected to server");
            }
            else if(cmd == NetworkEvent.Type.Data)
            {
                var messageType = (MessageHeader.MessageType)reader.ReadUShort();
                switch (messageType)
                {
                    case MessageHeader.MessageType.None:
                        break;
                    case MessageHeader.MessageType.NewPlayer:
                        break;
                    case MessageHeader.MessageType.Welcome:
                        var welcomeMessage = new WelcomeMessage();
                        welcomeMessage.DeserializeObject(ref reader);

                        Debug.Log("Got a welcome message");

                        var setNameMessage = new SetNameMessage
                        {
                            Name = "Vincent"
                        };
                        var writer = networkDriver.BeginSend(connection);
                        setNameMessage.SerializeObject(ref writer);
                        networkDriver.EndSend(writer);
                        break;
                    case MessageHeader.MessageType.SetName:
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
                Debug.Log("Disconnected from server");
                connection = default;
            }
        }
    }

    private void OnDestroy()
    {
        networkDriver.Dispose();
    }
}
