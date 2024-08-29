using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Movement : MonoBehaviour
{
    private ServiceLocator _serviceLocator;
    [SerializeField] private Rigidbody _rigidbody;
    private NetworkManagerClient _client;
    private InputReader _clientInputReader;

    [Header("Movement")] 
    [SerializeField] private float speed = 5.0f;
    private Vector3 _CurrentMovement;

    private void OnEnable()
    {
        _serviceLocator = ServiceLocator.Global;
        _serviceLocator.Register<Player_Movement>(GetType(), this);
        _serviceLocator.Get(out NetworkManagerClient client);
        _client = client;
        _serviceLocator.Get(out InputReader clientInputReader);
        _clientInputReader = clientInputReader;
        _clientInputReader.OnPlayerMove += MovePlayer;
    }

    public void MovePlayer(Vector3 obj)
    {
        _CurrentMovement = new Vector3(obj.x, 0f, obj.y);
    }

    private void FixedUpdate()
    {
        _rigidbody.AddForce(_CurrentMovement * speed);
        
        (int, int, Vector3) message;
        message.Item1 = (int)ObjectType.Player;
        message.Item2 = _client.clientId;
        message.Item3 = transform.position;
        
        NetPosition movement = new NetPosition(message);
        _client.SendToServer(movement.Serialize());
    }
}
