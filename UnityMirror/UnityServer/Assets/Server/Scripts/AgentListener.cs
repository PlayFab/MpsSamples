using System.Collections;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.Networking;
using System.Collections.Generic;
using PlayFab.MultiplayerAgent.Model;
using System.Linq;

public class AgentListener : MonoBehaviour {

    private List<ConnectedPlayer> _connectedPlayers;
    public bool Debugging = true;
    // Use this for initialization
    void Start () {
        _connectedPlayers = new List<ConnectedPlayer>();
        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = Debugging;
        PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;

        UnityNetworkServer.Instance.OnPlayerAdded.AddListener(OnPlayerAdded);
        UnityNetworkServer.Instance.OnPlayerRemoved.AddListener(OnPlayerRemoved);

        // get the port that the server will listen to
        // We *have to* do it on process mode, since there might be more than one game server instances on the same VM and we want to avoid port collision
        // On container mode, we can omit the below code and set the port directly, since each game server instance will run on its own network namespace. However, below code will work as well
        // we have to do that on process
        var connInfo = PlayFabMultiplayerAgentAPI.GetGameServerConnectionInfo();
        // make sure the ListeningPortKey is the same as the one configured in your Build settings (either on LocalMultiplayerAgent or on MPS)
        const string ListeningPortKey = "game_port";
        var portInfo = connInfo.GamePortsConfiguration.Where(x=>x.Name == ListeningPortKey);
        if(portInfo.Count() > 0)
        {
            Debug.Log(string.Format("port with name {0} was found in GSDK Config Settings.", ListeningPortKey));
            UnityNetworkServer.Instance.Port = portInfo.Single().ServerListeningPort;
        }
        else
        {
            string msg = string.Format("Cannot find port with name {0} in GSDK Config Settings. Please make sure the LocalMultiplayerAgent is running and that the MultiplayerSettings.json file includes correct name as a GamePort Name.", ListeningPortKey);
            Debug.LogError(msg);
            throw new Exception(msg);
        }
        
        StartCoroutine(ReadyForPlayers());
    }

    IEnumerator ReadyForPlayers()
    {
        yield return new WaitForSeconds(.5f);
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
    }
    
    private void OnServerActive()
    {
        UnityNetworkServer.Instance.StartListen();
        Debug.Log("Server Started From Agent Activation");
    }

    private void OnPlayerRemoved(string playfabId)
    {
        ConnectedPlayer player = _connectedPlayers.Find(x => x.PlayerId.Equals(playfabId, StringComparison.OrdinalIgnoreCase));
        _connectedPlayers.Remove(player);
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
    }

    private void OnPlayerAdded(string playfabId)
    {
        _connectedPlayers.Add(new ConnectedPlayer(playfabId));
        PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
    }

    private void OnAgentError(string error)
    {
        Debug.Log(error);
    }

    private void OnShutdown()
    {
        Debug.Log("Server is shutting down");
        foreach(var conn in UnityNetworkServer.Instance.Connections)
        {
            conn.Connection.Send<ShutdownMessage>(new ShutdownMessage());
        }
        StartCoroutine(Shutdown());
    }

    IEnumerator Shutdown()
    {
        yield return new WaitForSeconds(5f);
        Application.Quit();
    }

    private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
    {
        Debug.LogFormat("Maintenance scheduled for: {0}", NextScheduledMaintenanceUtc.Value.ToLongDateString());
        foreach (var conn in UnityNetworkServer.Instance.Connections)
        {
            conn.Connection.Send<MaintenanceMessage>(new MaintenanceMessage() {
                ScheduledMaintenanceUTC = (DateTime)NextScheduledMaintenanceUtc
            });
        }
    }
}
