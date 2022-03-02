using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using UnityEngine.Events;
using System;

/*
	Documentation: https://mirror-networking.com/docs/Components/NetworkManager.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class NewNetworkManager : NetworkManager
{

    public static NewNetworkManager Instance { get; private set; }

    public ConnectedEvent OnConnected = new ConnectedEvent();
    public DisconnectedEvent OnDisconnected = new DisconnectedEvent();

    public class ConnectedEvent : UnityEvent { }
    public class DisconnectedEvent : UnityEvent<int?> { }

    public override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    public override void Start()
    {
        base.Start();
        this.StartClient();
    }


    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("client connected");
        OnConnected.Invoke();
    }


    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("client disconnected");
        OnDisconnected.Invoke(null);
    }

    /// <summary>
    /// Called on clients when a network error occurs.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public override void OnClientError(Exception ex) { }

    /// <summary>
    /// Called on clients when a servers tells the client it is no longer ready.
    /// <para>This is commonly used when switching scenes.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientNotReady() { }

    /// <summary>
    /// Called from ClientChangeScene immediately before SceneManager.LoadSceneAsync is executed
    /// <para>This allows client to do work / cleanup / prep before the scene changes.</para>
    /// </summary>
    /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
    /// <param name="sceneOperation">Scene operation that's about to happen</param>
    /// <param name="customHandling">true to indicate that scene loading will be handled through overrides</param>
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) { }

    /// <summary>
    /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
    /// <para>Scene changes can cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkManager is to add a player object for the connection if no player object exists.</para>
    /// </summary>
    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
    }



    /// <summary>
    /// This is invoked when the client is started.
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
    }


    /// <summary>
    /// This is called when a client is stopped.
    /// </summary>
    public override void OnStopClient() { }

}


public class CustomGameServerMessageTypes
{
    public const short ReceiveAuthenticate = 900;
    public const short ShutdownMessage = 901;
    public const short MaintenanceMessage = 902;
}

public struct ReceiveAuthenticateMessage : NetworkMessage
{
    public string PlayFabId;
}

public struct ShutdownMessage : NetworkMessage { }

[Serializable]
public struct MaintenanceMessage : NetworkMessage
{
    public DateTime ScheduledMaintenanceUTC;
}
