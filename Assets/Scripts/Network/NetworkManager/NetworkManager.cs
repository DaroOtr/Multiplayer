using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkManager : MonoBehaviour , IReceiveData
{
    public int port { get; protected set; }
    public IPAddress ipAddress { get; protected set; }
    public static bool IsServer {get;protected set; }
    protected ServiceLocator _serviceLocator;

    protected int TimeOut = 15;
    protected Action<byte[], IPEndPoint> OnReceiveEvent;
    protected UdpConnection connection;
    protected List<Player> playersInMatch = new List<Player>();
    protected Dictionary<int,GameObject> spawnedPlayers = new Dictionary<int, GameObject>();
    
    protected void OnDispatchNetCon(string obj)
    {
        Debug.Log("OnDispatch (string obj)");
        _serviceLocator.Get(out ChatScreen chatScreen);
        chatScreen.ReceiveConsoleMessage(obj);
    }
    protected void RemovePlayer(int id)
    {
        Player aux = new Player();
        foreach (var player in playersInMatch)
        {
            if (player.playerID == id)
                aux = player;
        }

        if (playersInMatch.Contains(aux))
            playersInMatch.Remove(aux);

    }

    public virtual void OnReceiveData(byte[] data, IPEndPoint ip)
    {
        if (OnReceiveEvent != null)
            OnReceiveEvent.Invoke(data, ip);
    }

    protected GameObject SpawnSidePlayer()
    {
        _serviceLocator.Get(out NetworkScreen networkScreen);
        Vector2 rng = Random.insideUnitSphere * 5.0f;
        networkScreen.playerSpawn.position = new Vector3(rng.x, 0.0f, rng.y);
        return Instantiate(networkScreen.sidePlayerRep, networkScreen.playerSpawn);
    }

    public List<Player> GetCurrentPlayers() { return playersInMatch; }
    
    public virtual void Update()
    {
        // Flush the data in main thread
        if (connection != null)
            connection.FlushReceiveData();
    }
}
