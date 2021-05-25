using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.ClientModels;
using Mirror;
using PlayFab.Helpers;

public class Startup : MonoBehaviour
{
    PlayFabAuthService _authService;
    NewNetworkManager _nm;
    MessageWindow _messageWindow;

    // Use this for initialization
    void Start()
    {
        _authService = PlayFabAuthService.Instance;
        PlayFabAuthService.OnDisplayAuthentication += OnDisplayAuth;
        PlayFabAuthService.OnLoginSuccess += OnLoginSuccess;

        _nm = NewNetworkManager.Instance;
        _nm.OnDisconnected.AddListener(OnDisconnected);
        _nm.OnConnected.AddListener(OnConnected);
        NetworkClient.RegisterHandler<ShutdownMessage>(OnServerShutdown);
        NetworkClient.RegisterHandler<MaintenanceMessage>(OnMaintenanceMessage);

        _messageWindow = MessageWindow.Instance;
    }

    private void OnMaintenanceMessage(MaintenanceMessage msg)
    {
        var message = msg;
        _messageWindow.Title.text = "Maintenance Shutdown scheduled";
        _messageWindow.Message.text = string.Format("Maintenance is scheduled for: {0}", message.ScheduledMaintenanceUTC.ToString("MM-DD-YYYY hh:mm:ss"));
        _messageWindow.gameObject.SetActive(true);
    }

    private void OnServerShutdown(ShutdownMessage msg)
    {
        _messageWindow.Title.text = "Shutdown In Progress";
        _messageWindow.Message.text = "Server has issued a shutdown.";
        _messageWindow.gameObject.SetActive(true);
        NetworkClient.Disconnect();
    }

    private void OnConnected()
    {
        _authService.Authenticate();
    }

    private void OnDisplayAuth()
    {
        _authService.Authenticate(Authtypes.Silent);
    }

    private void OnLoginSuccess(LoginResult success)
    {
        _messageWindow.Title.text = "Login Successful";
        _messageWindow.Message.text = string.Format("You logged in successfully. ID:{0}", success.PlayFabId);
        _messageWindow.gameObject.SetActive(true);

        NetworkClient.connection.Send<ReceiveAuthenticateMessage>(new ReceiveAuthenticateMessage()
        {
            PlayFabId = success.PlayFabId
        });
    }

    private void OnDisconnected(int? code)
    {
        _messageWindow.Title.text = "Disconnected!";
        _messageWindow.Message.text = "You were disconnected from the server";
        _messageWindow.gameObject.SetActive(true);
    }


}