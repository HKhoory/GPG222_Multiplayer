namespace Hamad.Scripts.Message
{
    public class MessagePacket : Packet
    {
        public string Message { get; private set; }

        public MessagePacket() : base(PacketType.None, null)
        {
            Message = "";
        }

        public MessagePacket(PlayerData playerData, string message) : base(PacketType.Message, playerData)
        {
            this.playerData = playerData;
            Message = message;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(Message);
            return EndSerialize();
        }

        public new MessagePacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            Message = _binaryReader.ReadString();
            return this;
        }

    }
}
