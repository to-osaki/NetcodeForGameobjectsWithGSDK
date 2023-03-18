using MemoryPack;

namespace App.PacketSchema
{
	[MemoryPackable]
	public partial class Message
	{
		[MemoryPackOrder(0)]
		public int Id { get; set; }
		[MemoryPackOrder(1)]
		public string Name { get; set; }
	}
}
