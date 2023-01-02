using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerAgent.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace App
{
	public class AppEntryPoint : MonoBehaviour
	{
		public static bool IsOnNetworkServer =>
#if UNITY_SERVER && !UNITY_EDITOR && ENABLE_PLAYFABSERVER_API
			true;
#else
			false;
#endif

		public class Connection
		{
			public string PlayFabId { get; set; }
		}

		readonly Dictionary<ulong, Connection> m_connectedPlayers = new();

		string m_PlayFabCustomID = "CustomID";

		// Start is called before the first frame update
		void Start()
		{
			if (IsOnNetworkServer)
			{
				Debug.Log("PlayFabMultiplayerAgentAPI.Start");
				PlayFabMultiplayerAgentAPI.Start();
				PlayFabMultiplayerAgentAPI.IsDebugging = false;
				//PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
				//PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
				PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += msg => Debug.LogError(msg);
				PlayFabMultiplayerAgentAPI.OnServerActiveCallback += () =>
				{
					Debug.Log("PlayFabMultiplayerAgentAPI.OnServerActive -> NetworkManager.StartServer");
					NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
					NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
					NetworkManager.Singleton.StartServer();
				};

				var connInfo = PlayFabMultiplayerAgentAPI.GetGameServerConnectionInfo();
				var portInfo = connInfo.GamePortsConfiguration.First(x => x.Name == "game_port");
				Debug.Log($"Listening port = {portInfo.ServerListeningPort}");
				var transport = this.GetComponent<UnityTransport>();
				transport.ConnectionData.Port = (ushort)portInfo.ServerListeningPort;

				PlayFabMultiplayerAgentAPI.ReadyForPlayers();
			}
		}

		private void OnGUI()
		{
			if (IsOnNetworkServer) { return; }
			if (NetworkManager.Singleton.IsClient) { return; }

			m_PlayFabCustomID = GUILayout.TextField(m_PlayFabCustomID);
			if (GUILayout.Button("Login"))
			{
				Debug.Log("StartClient");
				var request = new LoginWithCustomIDRequest { CustomId = m_PlayFabCustomID, CreateAccount = true };
				PlayFabClientAPI.LoginWithCustomID(request,
					result =>
					{
						Debug.Log($"OnLoggedIn:{result.PlayFabId}");
						NetworkManager.Singleton.OnClientConnectedCallback += clientId =>
						{
							OnConnectedServerRpc(result.PlayFabId);
						};
						NetworkManager.Singleton.StartClient();
					},
					error =>
					{
						Debug.LogError($"{error}");
					});
			}
		}

		void OnClientConnected(ulong clientId)
		{
			// work on server 
			Debug.Log($"OnClientConnected:{clientId}");
			Debug.Assert(!m_connectedPlayers.ContainsKey(clientId));
			m_connectedPlayers[clientId] = new Connection();
		}

		void OnClientDisconnected(ulong clientId)
		{
			// work on server
			Debug.Log($"OnClientDisconnected:{clientId}");
			m_connectedPlayers.Remove(clientId);
			PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(m_connectedPlayers.Values.Where(it => !string.IsNullOrEmpty(it.PlayFabId)).Select(it => new ConnectedPlayer(it.PlayFabId)).ToList());
		}

		[ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
		void OnConnectedServerRpc(string playfabId, ServerRpcParams serverRpcParams = default)
		{
			Debug.Log($"OnConnectedServerRpc {serverRpcParams.Receive.SenderClientId} {playfabId}");

			if (!IsOnNetworkServer) { return; }
			// work on server 
			ulong clientId = serverRpcParams.Receive.SenderClientId;
			bool connected = NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId);
			if (connected)
			{
				if (m_connectedPlayers.TryGetValue(clientId, out var connection))
				{
					connection.PlayFabId = playfabId;
					PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(m_connectedPlayers.Values.Where(it => !string.IsNullOrEmpty(it.PlayFabId)).Select(it => new ConnectedPlayer(it.PlayFabId)).ToList());
				}
				else
				{
					NetworkManager.Singleton.DisconnectClient(clientId);
				}
			}
		}
	}
}