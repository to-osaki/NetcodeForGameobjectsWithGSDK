using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace App
{
	public struct SerializedString32Bytes : INetworkSerializable
	{
		FixedString32Bytes m_ID;

		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref m_ID);
		}

		public override string ToString()
		{
			return m_ID.Value.ToString();
		}

		public static implicit operator string(SerializedString32Bytes s) => s.ToString();
		public static implicit operator SerializedString32Bytes(string s) => new SerializedString32Bytes() { m_ID = new FixedString32Bytes(s) };
	}

	/// <summary>
	/// クライアント常駐データ
	/// </summary>
	public class NetworkPlayerObject : NetworkBehaviour
	{
		[SerializeField]
		NetworkVariable<SerializedString32Bytes> m_Label = new NetworkVariable<SerializedString32Bytes>();

		public override void OnNetworkSpawn()
		{
			gameObject.name = "ID:" + OwnerClientId;

			if (IsServer)
			{
				m_Label.Value = gameObject.name;
				this.transform.position = new Vector3(OwnerClientId, 0, 0);
			}
			else
			{
				gameObject.GetComponentInChildren<TMPro.TMP_Text>().text = m_Label.Value;
				m_Label.OnValueChanged += (prev, next) =>
				{
					gameObject.GetComponentInChildren<TMPro.TMP_Text>().text = next;
				};
			}
			base.OnNetworkSpawn();
		}

		void Update()
		{
			if (IsOwner) { UpdateAsOwner(); }
		}

		void UpdateAsOwner()
		{
			var move = Vector3.zero;
			if (Input.GetKeyDown(KeyCode.UpArrow))
			{
				move.y += 1;
			}
			else if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				move.y -= 1;
			}
			else if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				move.x += 1;
			}
			else if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				move.x -= 1;
			}
			if (move != Vector3.zero)
			{
				MoveServerRpc(move);
			}

			if (Input.GetKeyDown(KeyCode.Space))
			{
				ChangeLabelServerRpc();
			}
		}

		[ServerRpc]
		void MoveServerRpc(Vector3 move)
		{
			this.transform.localPosition += move;
		}

		[ServerRpc]
		void ChangeLabelServerRpc()
		{
			m_Label.Value = m_Label.Value + "+";
		}
	}
}