using MemoryPack;
using PlayFab;
using System;
using System.Buffers;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport;
using UnityEngine;

namespace App
{
	public class DedicatedServerEntryPoint : MonoBehaviour
	{
		[SerializeField] private bool m_IsDebugging;

		UnityTransportServer m_Server = new();

		// Start is called before the first frame update
		void Start()
		{
			PlayFabMultiplayerAgentAPI.Start(); // to GameState.Initializing
			PlayFabMultiplayerAgentAPI.IsDebugging = m_IsDebugging;
			PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
			PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
			PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;
			PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;

			var connInfo = PlayFabMultiplayerAgentAPI.GetGameServerConnectionInfo();
			var portInfo = connInfo.GamePortsConfiguration.First(x => x.Name == "game_port");
			Debug.Log($"Listening port = {portInfo.ServerListeningPort}");

			if (m_Server.Listen((ushort)portInfo.ServerListeningPort))
			{
				m_Server.OnClientConnected += OnClientConnected;
				m_Server.OnDataReceived += OnDataReceived;
				PlayFabMultiplayerAgentAPI.ReadyForPlayers(); // to GameState.StandingBy
			}
			else
			{
				Application.Quit();
			}
		}

		private void OnClientConnected(NetworkConnection c)
		{
			var msg = new PacketSchema.Message
			{
				Id = c.InternalId,
				Name = "ÉTÅ[ÉoÇÊÇË",
			};
			var data = MemoryPackSerializer.Serialize(msg);
			m_Server.Send(c, data);
		}

		private void OnDataReceived(ref DataStreamReader reader)
		{
			using var arr = new NativeArray<byte>(reader.Length, Allocator.Temp);
			reader.ReadBytes(arr);
			var message = MemoryPackSerializer.Deserialize<PacketSchema.Message>(arr.AsReadOnlySpan());

			Debug.Log(message.Id + message.Name);
		}

		private void OnServerActive()
		{
			// GameState.Active
		}

		private void OnAgentError(string error)
		{
			Debug.LogError(error);
		}

		private void OnShutdown()
		{
			Application.Quit();
		}

		private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
		{
			// 
		}

		void Update()
		{
			m_Server?.UpdateNetwork();
		}

		private void OnDestroy()
		{
			m_Server?.Dispose();
			m_Server = null;
		}
	}
}
