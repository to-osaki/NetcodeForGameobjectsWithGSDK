using Cysharp.Threading.Tasks;
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
	public class ClientEntryPoint : MonoBehaviour
	{
		UnityTransportClient m_Client = new();

		// Start is called before the first frame update
		async UniTaskVoid Start()
		{
			m_Client.Connect(NetworkEndPoint.LoopbackIpv4.WithPort(8080));
			m_Client.OnDataReceived += OnDataReceived;
			await UniTask.WaitUntil(() => m_Client.State == NetworkConnection.State.Connected, cancellationToken: this.GetCancellationTokenOnDestroy());
			Debug.Log("Connected");

			var msg = new PacketSchema.Message
			{
				Id = 99,
				Name = "Client‚©‚ç",
			};
			var data = MemoryPackSerializer.Serialize(msg);
			m_Client.Send(data);
		}

		private void OnDataReceived(ref DataStreamReader reader)
		{
			using var arr = new NativeArray<byte>(reader.Length, Allocator.Temp);
			reader.ReadBytes(arr);
			var message = MemoryPackSerializer.Deserialize<PacketSchema.Message>(arr.AsReadOnlySpan());

			Debug.Log(message.Id + message.Name);
		}
		void Update()
		{
			m_Client?.UpdateNetwork();
		}

		private void OnDestroy()
		{
			m_Client?.Dispose();
			m_Client = null;
		}
	}
}
