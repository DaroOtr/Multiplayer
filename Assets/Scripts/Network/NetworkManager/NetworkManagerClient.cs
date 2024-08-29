using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkManagerClient : NetworkManager
{
    public bool setLastTime = true;
    public int clientId = 0; //  cliente
    public string playerName; //  cliente
    private DateTime lastTimeReceivedPing = DateTime.UtcNow;
    private Player clientPlayer;

    public void StartClient(IPAddress ip, int port, string name) // cliente pero con mensaje para el servidor
    {
        _serviceLocator = ServiceLocator.Global;
        _serviceLocator.Register<NetworkManagerClient>(GetType(), this);
        NetConsole.OnDispatch += OnDispatchNetCon;
        NetServerToClientHS.OnDispatch += OnDispatchNetS2C;

        this.port = port;
        this.ipAddress = ip;
        playerName = name;

        connection = new UdpConnection(ip, port, this);

        clientPlayer = new Player(name, -7);
        NetClientToServerHS nacho = new NetClientToServerHS(clientPlayer);
        SendToServer(nacho.Serialize());

        NetPing ping = new NetPing();
        SendToServer(ping.Serialize());
    }

    private void OnDisable()
    {
        NetConsole.OnDispatch -= OnDispatchNetCon;
        NetServerToClientHS.OnDispatch -= OnDispatchNetS2C;
    }

    public void SendToServer(byte[] data)
    {
        connection.Send(data);
    }

    protected void OnDispatchNetS2C(List<Player> obj)
    {
        playersInMatch = obj;
    }

    public override void OnReceiveData(byte[] data, IPEndPoint ip)
    {
        HandleMessage(data);
    }

    public void SetLastRecivedPingTime(DateTime currentTime)
    {
        lastTimeReceivedPing = currentTime;
    }

    public bool CheckTimeDiference(DateTime currentTime)
    {
        float diference = currentTime.Second - lastTimeReceivedPing.Second;
        if (diference > TimeOut)
        {
            return false;
        }

        return true;
    }

    public void HandleMessage(byte[] message)
    {
        MessageType temp = (MessageType)BitConverter.ToInt32(message, 0);
        switch (temp)
        {
            case MessageType.Console:
                NetConsole con = new NetConsole(message);
                if (con.CheckMessage(message))
                {
                    NetConsole.OnDispatch?.Invoke(con.GetData());
                    Debug.Log(nameof(MessageType.Console) + ": The message is ok");
                }
                else
                    Debug.Log(nameof(MessageType.Console) + ": The message is corrupt");

                break;
            case MessageType.Position:
                NetPosition pos = new NetPosition(message);
                if (pos.CheckMessage(message))
                {
                    if (pos.GetData().Item1 == (int)ObjectType.Player)
                    {
                        if (spawnedPlayers.ContainsKey(pos.GetData().Item2))
                        {
                            spawnedPlayers[pos.GetData().Item2].transform.position = pos.GetData().Item3;
                            playersInMatch[pos.GetData().Item2].SetPosition(pos.GetData().Item3);
                        }
                    }
                    Debug.Log(nameof(MessageType.Position) + ": The message is ok");
                }
                else
                {
                    Debug.Log(nameof(MessageType.Position) + ": The message is corrupt");
                }

                break;
            case MessageType.ServerToClientHS:
                NetServerToClientHS s2c = new NetServerToClientHS(message);
                if (s2c.CheckMessage(message))
                {
                    //Chekear si mi nombre esta en la lista , si no esta volver a mandar un handshake 
                    foreach (Player player in s2c.GetData())
                    {
                        Debug.Log("Player Name : " + player.playerName);
                        Debug.Log("Player ID : " + player.playerID);
                        if (player.playerName == playerName)
                        {
                            clientId = player.playerID;
                            clientPlayer.playerID = player.playerID;
                        }
                    }

                    NetServerToClientHS.OnDispatch.Invoke(s2c.GetData());
                    Debug.Log(nameof(MessageType.ServerToClientHS) + ": The message is ok");
                }
                else
                {
                    Debug.Log(nameof(MessageType.ServerToClientHS) + ": The message is corrupt");
                }

                break;
            case MessageType.Ping:
                NetPing ping = new NetPing();
                if (ping.CheckMessage(message))
                {
                    if (CheckTimeDiference(DateTime.UtcNow))
                    {
                        if (setLastTime)
                            SetLastRecivedPingTime(DateTime.UtcNow);

                        SendToServer(ping.Serialize());
                    }
                    else
                    {
                        //NetworkManager.Instance.RemoveClient(ep);
                    }

                    Debug.Log(nameof(MessageType.Ping) + ": The message is ok");
                }
                else
                {
                    Debug.Log(nameof(MessageType.Ping) + ": The message is corrupt");
                }

                break;
            case MessageType.PlayerList:
                NetPlayerList newList = new NetPlayerList(message);
                if (newList.CheckMessage(message))
                {
                    _serviceLocator.Get(out NetworkManagerClient client);
                    playersInMatch = newList.GetData();
                    foreach (Player player in playersInMatch)
                    {
                        if (player.playerID != clientId && !spawnedPlayers.ContainsKey(player.playerID))
                        {
                            spawnedPlayers.Add(player.playerID,SpawnSidePlayer());
                        }
                        else if (player.playerID == clientId && !spawnedPlayers.ContainsKey(player.playerID))
                        {
                            spawnedPlayers.Add(player.playerID,SpawnMainPlayer());
                        }
                    }

                    Debug.Log(nameof(MessageType.PlayerList) + ": The message is ok");
                }
                else
                {
                    Debug.Log(nameof(MessageType.PlayerList) + ": The message is corrupt");
                }

                break;
        }
    }

    private GameObject SpawnMainPlayer()
    {
        _serviceLocator.Get(out NetworkScreen networkScreen);
        
        Vector2 rng = Random.insideUnitSphere * 5.0f;
        networkScreen.playerSpawn.position = new Vector3(rng.x, 0.0f, rng.y);
        _serviceLocator.Get(out CameraController cameraController);
        cameraController.InitCamera(networkScreen.mainPlayerRep.transform);
        clientPlayer.playerPos = networkScreen.mainPlayerRep.transform.position;
        return Instantiate(networkScreen.mainPlayerRep, networkScreen.playerSpawn);
    }

    public override void Update()
    {
        base.Update();
        if (!CheckTimeDiference(DateTime.UtcNow))
        {
            Debug.LogWarning("NetworkManagerClient : Disconecting client " + playerName);
        }
    }
}