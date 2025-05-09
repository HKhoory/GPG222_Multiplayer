using System.Collections.Generic;

namespace Hamad.Scripts.Rotation
{
    public class PlayersRotationDataPacket : Packet
    {
        public List<PlayerRotationData> PlayerRotationData { get; private set; }

        public PlayersRotationDataPacket() : base(PacketType.None, null)
        {
            PlayerRotationData = new List<PlayerRotationData>();
        }

        public PlayersRotationDataPacket(PlayerData playerData, List<PlayerRotationData> playerRotationData) : base(PacketType.PlayersRotationData, playerData)
        {
            this.playerData = playerData;
            this.PlayerRotationData = playerRotationData;
        }

        public byte[] Serialize()
        {
            BeginSerialize();
            _binaryWriter.Write(PlayerRotationData.Count);

            for (int i = 0; i < PlayerRotationData.Count; i++)
            {
                _binaryWriter.Write(PlayerRotationData[i].playerData.name);
                _binaryWriter.Write(PlayerRotationData[i].playerData.tag);
                _binaryWriter.Write(PlayerRotationData[i].xRot);
                _binaryWriter.Write(PlayerRotationData[i].yRot);
                _binaryWriter.Write(PlayerRotationData[i].zRot);
            }
            return EndSerialize();
        }

        public new PlayersRotationDataPacket Deserialize(byte[] buffer)
        {
            base.Deserialize(buffer);
            int playerRotationDataListCount = _binaryReader.ReadInt32();

            PlayerRotationData = new List<PlayerRotationData>(playerRotationDataListCount);

            for (int i = 0; i < playerRotationDataListCount; i++)
            {
                //PlayerData pData = new PlayerData(_binaryReader.ReadString(), _binaryReader.ReadInt64(), _binaryReader.ReadInt64(), _binaryReader.ReadInt64());
                PlayerData pData = new PlayerData(_binaryReader.ReadString(), _binaryReader.ReadInt32());
                
                float xRot = _binaryReader.ReadSingle();
                float yRot = _binaryReader.ReadSingle();
                float zRot = _binaryReader.ReadSingle();
                
                PlayerRotationData prData = new PlayerRotationData(pData, xRot, yRot, zRot);
                PlayerRotationData.Add(prData);
            }
            return this;
        }
    }
}
