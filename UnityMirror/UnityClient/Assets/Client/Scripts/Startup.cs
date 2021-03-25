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
        NetworkClient.RegisterHandler<ShutdownMessage>( /*CustomGameServerMessageTypes.ShutdownMessage, */OnServerShutdown);
        NetworkClient.RegisterHandler<MaintenanceMessage>(/*CustomGameServerMessageTypes.MaintenanceMessage, */OnMaintenanceMessage);

        _messageWindow = MessageWindow.Instance;
    }

    private void OnMaintenanceMessage( MaintenanceMessage netMsg )
    {
        var message = netMsg;
        _messageWindow.Title.text = "Maintenance Shutdown scheduled";
        _messageWindow.Message.text = string.Format("Maintenance is scheduled for: {0}", message.ScheduledMaintenanceUTC.ToString("MM-DD-YYYY hh:mm:ss"));
        _messageWindow.gameObject.SetActive(true);
		showCanvas = true;
    }

    private void OnServerShutdown( ShutdownMessage netMsg )
    {
        _messageWindow.Title.text = "Shutdown In Progress";
        _messageWindow.Message.text = "Server has issued a shutdown.";
        _messageWindow.gameObject.SetActive(true);
		showCanvas = true;
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
		showCanvas = false;

        NetworkClient.connection.Send<ReceiveAuthenticateMessage>( /*CustomGameServerMessageTypes.ReceiveAuthenticate, */new ReceiveAuthenticateMessage()
        {
            PlayFabId = success.PlayFabId
        });

		//MyMiniGame.Begin();
	}

    private void OnDisconnected(int? code) {
		_messageWindow.Title.text = "Disconnected!";
        _messageWindow.Message.text = "You were disconnected from the server";
        _messageWindow.gameObject.SetActive(true);
		showCanvas = true;
    }


	bool mWaitForInput = false;
	bool showCanvas {
		set {
			if( value == false ) {
				mWaitForInput = true;

			} else {
				mWaitForInput = false;
				if( _messageWindow != null ) {
					Canvas c = _messageWindow.GetComponentInParent<Canvas>();
					if( c != null ) {
						c.gameObject.SetActive( true );
					}
				}
			}
		}
	}
	private void Update() {
		if( mWaitForInput && Input.anyKeyDown ) {
			mWaitForInput = false;
			if( _messageWindow != null ) {
				Canvas c = _messageWindow.GetComponentInParent<Canvas>();
				if( c != null ) {
					c.gameObject.SetActive( false );
				}
			}
		}
	}

}