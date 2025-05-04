using System.IO;
using UnityEngine;

namespace Hamad.Scripts
{
    public class Packet
    {
        protected MemoryStream _memoryStreamWriter;
        protected BinaryWriter _binaryWriter;

        protected MemoryStream _memoryStreamReader;
        protected BinaryReader _binaryReader;

        public enum PacketType
        {
            None,
            Message,
            Id,
            Color,
            PlayersPositionData,
            PlayersRotationData,
            Ping,
            PingResponse,
            PushEvent,
            Heartbeat,
            JoinLobby,
            Restart,
            ReadyInLobby,
            FreezeEvent,
            Blockade,
            LobbyState,
            LobbyPlayerJoin,
            LobbyPlayerLeave,
            GameStart
        }

        public PacketType packetType;
        public PlayerData playerData;

        public Packet()
        {
            packetType = PacketType.None;
            playerData = null;
        }

        public Packet(PacketType packetType, PlayerData playerData)
        {
            this.packetType = packetType;
            this.playerData = playerData;
        }

        protected void BeginSerialize()
        {
          /*  if (playerData == null)
            {
                Debug.LogError("playerData is null during serialization!");
                return;
            } */
            _memoryStreamWriter = new MemoryStream();
            _binaryWriter = new BinaryWriter(_memoryStreamWriter);
            _binaryWriter.Write((int)packetType);
            _binaryWriter.Write(playerData.name);
            _binaryWriter.Write(playerData.tag);
        
            //maybe color
            //and quaternion
            //tag as well
        }

        protected byte[] EndSerialize()
        {
            return _memoryStreamWriter.ToArray();
        }

        public void Deserialize(byte[] buffer)
        {
            _memoryStreamReader = new MemoryStream(buffer);
            _binaryReader = new BinaryReader(_memoryStreamReader);

            packetType = (PacketType)_binaryReader.ReadInt32();
            //playerData = new PlayerData(_binaryReader.ReadString(), _binaryReader.ReadInt32(), _binaryReader.ReadInt64(), _binaryReader.ReadInt64(), _binaryReader.ReadInt64());
            playerData = new PlayerData(_binaryReader.ReadString(), _binaryReader.ReadInt32());
            //for now we have the name, id, x y and z positions

        }

    }
}
