using System;
using UnityEngine;
using UnityEngine.UI;
using System.Net;

public class NetworkScreen : MonoBehaviourSingleton<NetworkScreen>
{
    public Button connectBtn;
    public Button startServerBtn;
    public InputField portInputField;
    public InputField addressInputField;
    public InputField nameInputField;
    public GameObject mainPlayerRep;
    public GameObject sidePlayerRep;
    public Transform playerSpawn;
    public Transform serverCameraPos;
    
    private ServiceLocator _serviceLocator;
    private CameraController _camController;
    private NetworkManagerServer _networkServer;
    private NetworkManagerClient _networkClient;

    protected void Start()
    {
        _serviceLocator = ServiceLocator.Global;
        _serviceLocator.Register<NetworkScreen>(GetType(), this);
        connectBtn.onClick.AddListener(OnConnectBtnClick);
        startServerBtn.onClick.AddListener(OnStartServerBtnClick);
    }
    
    void OnConnectBtnClick()
    {
        IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
        int port = System.Convert.ToInt32(portInputField.text);
        string playerName = nameInputField.text;

        _networkClient = _serviceLocator.gameObject.AddComponent<NetworkManagerClient>();
        
        _networkClient.StartClient(ipAddress, port,playerName);
        _serviceLocator.Get(out NetworkManagerClient client);
        _networkClient = client;
        
        _serviceLocator.Get(out ChatScreen chatScreen);
        chatScreen.InitChatScreen();
        
        SwitchToChatScreen();
    }

    void OnStartServerBtnClick()
    {
        int port = Convert.ToInt32(portInputField.text);
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        
        _networkServer = _serviceLocator.gameObject.AddComponent<NetworkManagerServer>();
        
        _networkServer.StartServer(port);
        _serviceLocator.Get(out NetworkManagerServer server);
        _networkServer = server;
        
        _serviceLocator.Get(out ChatScreen chatScreen);
        chatScreen.InitChatScreen();
        
        SwitchToChatScreen();
        _serviceLocator.Get(out CameraController cameraController);
        cameraController.InitCamera(serverCameraPos);
    }

    void SwitchToChatScreen()
    {
        _serviceLocator.Get(out ChatScreen chatScreen);
        chatScreen.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
    }
}
