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
	public class PlayfabLoginDemo : MonoBehaviour
	{
		public static bool IsOnPlayfabDedicatedServer =>
#if UNITY_SERVER && !UNITY_EDITOR && ENABLE_PLAYFABSERVER_API
			true;
#else
			false;
#endif

		public class Connection
		{
			public string PlayFabId { get; set; }
		}

		readonly Dictionary<ulong, Connection> m_ClientIdToPlayerTable = new();

		string m_PlayFabCustomID = "CustomID";

		// Start is called before the first frame update
		void Start()
		{
			if (IsOnPlayfabDedicatedServer)
			{
				Debug.Log("PlayFabMultiplayerAgentAPI.Start");
				PlayFabMultiplayerAgentAPI.Start();
				PlayFabMultiplayerAgentAPI.IsDebugging = true;
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
			if (IsOnPlayfabDedicatedServer) { return; }
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
							// send my PlayfabId to server
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
			Debug.Assert(!m_ClientIdToPlayerTable.ContainsKey(clientId));
			m_ClientIdToPlayerTable[clientId] = new Connection();
		}

		void OnClientDisconnected(ulong clientId)
		{
			// work on server
			Debug.Log($"OnClientDisconnected:{clientId}");
			if (m_ClientIdToPlayerTable.ContainsKey(clientId))
			{
				m_ClientIdToPlayerTable.Remove(clientId);
				PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(m_ClientIdToPlayerTable.Values.Where(it => !string.IsNullOrEmpty(it.PlayFabId)).Select(it => new ConnectedPlayer(it.PlayFabId)).ToList());
			}
		}

		[ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
		void OnConnectedServerRpc(string playfabId, ServerRpcParams serverRpcParams = default)
		{
			Debug.Log($"OnConnectedServerRpc {serverRpcParams.Receive.SenderClientId} {playfabId}");

			if (!IsOnPlayfabDedicatedServer) { return; }
			// work on server 
			ulong clientId = serverRpcParams.Receive.SenderClientId;
			bool connected = NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId);
			if (connected)
			{
				if (m_ClientIdToPlayerTable.TryGetValue(clientId, out var connection))
				{
					// notify joining player to Playfab
					connection.PlayFabId = playfabId;
					PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(
						m_ClientIdToPlayerTable.Values.Where(it => !string.IsNullOrEmpty(it.PlayFabId)).Select(it => new ConnectedPlayer(it.PlayFabId)).ToList());
				}
				else
				{
					NetworkManager.Singleton.DisconnectClient(clientId);
				}
			}
		}
	}
}