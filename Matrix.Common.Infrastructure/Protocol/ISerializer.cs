using Matrix.Common.Infrastructure.Protocol.Messages;
namespace Matrix.Common.Infrastructure.Protocol
{
	interface ISerializer
	{
		byte[] SerializeMessage(DoMessage message);
		DoMessage DeserializeMessage(byte[] data);
	}
}
