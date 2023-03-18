using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;

namespace App
{
	public class UnityTransportServer : IDisposable
	{
		public delegate void ClientEvent(NetworkConnection connection);
		public delegate void DataReceivedEvent(ref DataStreamReader reader);

		public event ClientEvent OnClientConnected;
		public event ClientEvent OnClientDisconnected;
		public event DataReceivedEvent OnDataReceived;

		NetworkDriver m_Driver;
		List<NetworkConnection> m_Connections;

		public bool IsListening => m_Driver.Listening;

		public void Dispose()
		{
			if (m_Driver.IsCreated)
			{
				foreach (NetworkConnection connection in m_Connections)
				{
					if (connection.IsCreated)
					{
						connection.Disconnect(m_Driver);
					}
				}
				m_Driver.Dispose();
			}
			m_Driver = default;
			m_Connections?.Clear();
			m_Connections = null;
		}

		public bool Listen(ushort port)
		{
			var e = NetworkEndPoint.AnyIpv4.WithPort(port);
			m_Driver = NetworkDriver.Create();
			if (m_Driver.Bind(e) == 0)
			{
				int returnCode = m_Driver.Listen();
				if (returnCode == 0 && m_Driver.IsCreated)
				{
					m_Connections = new List<NetworkConnection>(capacity: 16);
				}
			}
			return m_Driver.Listening;
		}

		public void UpdateNetwork()
		{
			if (!m_Driver.IsCreated) { return; }

			m_Driver.ScheduleUpdate().Complete();

			AcceptNewConnections();

			HandleEvents();
		}

		private void AcceptNewConnections()
		{
			while (true)
			{
				NetworkConnection newone = m_Driver.Accept();
				if (newone != default)
				{
					m_Connections.Add(newone);
				}
				else
				{
					break;
				}
			}
		}

		public void Send(int id, byte[] data)
		{
			Send(m_Connections.Find(c => c.InternalId == id), data);
		}

		public void Send(NetworkConnection c, byte[] data)
		{
			if (!m_Driver.IsCreated) return;
			if (!c.IsCreated) return;

			int returnCode = m_Driver.BeginSend(c, out var writer);
			if (returnCode == 0)
			{
				using (var bin = new NativeArray<byte>(data, Allocator.Temp))
				{
					writer.WriteBytes(bin);
				}
				m_Driver.EndSend(writer);
			}
		}

		private void HandleEvents()
		{
			DataStreamReader reader;
			for (int i = 0; i < m_Connections.Count; i++)
			{
				var c = m_Connections[i];
				if (!c.IsCreated) { continue; }

				while (true)
				{
					NetworkEvent.Type cmd = m_Driver.PopEventForConnection(c, out reader);
					if (cmd == NetworkEvent.Type.Connect)
					{
						OnClientConnected?.Invoke(c);
					}
					else if (cmd == NetworkEvent.Type.Data)
					{
						OnDataReceived?.Invoke(ref reader);
					}
					else if (cmd == NetworkEvent.Type.Disconnect)
					{
						OnClientDisconnected?.Invoke(c);
						m_Connections[i] = default;
					}
					else
					{
						break;
					}
				}
			}
		}
	}
}
