using MemoryPack;
using System;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.VisualScripting;

namespace App
{
	public class UnityTransportClient : IDisposable
	{
		public delegate void OnDataReceivedEvent(ref DataStreamReader reader);

		public event OnDataReceivedEvent OnDataReceived;

		NetworkDriver m_Driver;
		NetworkConnection m_Connection;

		public NetworkConnection.State State => m_Connection.GetState(m_Driver);

		public void Connect(NetworkEndPoint endPoint)
		{
			m_Driver = NetworkDriver.Create();
			m_Connection = m_Driver.Connect(endPoint);
		}

		public void Dispose()
		{
			if (m_Connection.IsCreated) m_Connection.Disconnect(m_Driver);
			if (m_Driver.IsCreated) m_Driver.Dispose();
			m_Driver = default;
			m_Connection = default;
		}

		public void UpdateNetwork()
		{
			if (!m_Driver.IsCreated) { return; }

			m_Driver.ScheduleUpdate().Complete();

			HandleEvents();
		}

		public void Send(byte[] data)
		{
			int returnCode = m_Driver.BeginSend(m_Connection, out var writer);
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
			var c = m_Connection;
			if (!c.IsCreated) { return; }

			while (true)
			{
				NetworkEvent.Type cmd = m_Driver.PopEventForConnection(c, out reader);
				if (cmd == NetworkEvent.Type.Connect)
				{
				}
				else if (cmd == NetworkEvent.Type.Data)
				{
					OnDataReceived?.Invoke(ref reader);
				}
				else if (cmd == NetworkEvent.Type.Disconnect)
				{
					m_Connection = default;
				}
				else
				{
					break;
				}
			}
		}
	}
}
