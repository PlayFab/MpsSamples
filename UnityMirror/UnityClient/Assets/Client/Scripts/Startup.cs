using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

public class Startup : MonoBehaviour
{
    NewNetworkManager _nm;
    MessageWindow _messageWindow;

    // Use this for initialization
    void Start()
    {
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
        Debug.Log("connected");
    }
    private void OnDisconnected(int? code)
    {
        _messageWindow.Title.text = "Disconnected!";
        _messageWindow.Message.text = "You were disconnected from the server";
        _messageWindow.gameObject.SetActive(true);
    }
}