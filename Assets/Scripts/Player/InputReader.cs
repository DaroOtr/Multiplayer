using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    private ServiceLocator _serviceLocator;
    private NetworkManagerClient _client;
    private ChatScreen _chatScreen;
    public Action<Vector3> OnPlayerMove;


    void OnEnable()
    {
        _serviceLocator = ServiceLocator.Global;
        _serviceLocator.Register<Player_Movement>(GetType(), this);
        _serviceLocator.Get(out ChatScreen chat);
        _chatScreen = chat;
        _serviceLocator.Get(out NetworkManagerClient client);
        _client = client;
    }
    
    public void OnMove(InputValue input)
    {
        OnPlayerMove?.Invoke(input.Get<Vector3>());
        //NetVector3 movement = new NetVector3(aux);
        //_client.SendToServer(movement.Serialize());
    }

    public void OnOpenChat(InputValue input)
    {
        if (_chatScreen.isActiveAndEnabled)
        {
            _chatScreen.inputMessage.ActivateInputField();
        }
        else
        {
            _chatScreen.SwitchToChat();
        }
    }
}