using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaChe.Common
{
    public enum MessageType : byte
    {
        Initial,
        Text,
        Image,
        Quit
    }

    public class Message
    {
        public MessageType Type     { get; set; }

        public ushort UsernameLength { get; private set; }
        public string Username { get; set; }

        public ushort ContentLength        { get; private set; }
        public byte[] Content       { get; set; }

        public void FromBytes(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                Type = (MessageType)reader.ReadByte();

                UsernameLength = reader.ReadUInt16();
                byte[] usernameData = reader.ReadBytes(UsernameLength);
                Username = Encoding.ASCII.GetString(usernameData);

                ContentLength = reader.ReadUInt16();
                Content = reader.ReadBytes(ContentLength);
            }
        }

        public byte[] ToBytes()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((byte) Type);

                byte[] usernameData = Encoding.ASCII.GetBytes(Username);
                UsernameLength = (ushort) usernameData.Length;
                writer.Write(UsernameLength);
                writer.Write(usernameData);

                ContentLength = (ushort)Content.Length;
                writer.Write(ContentLength);
                writer.Write(Content);

                return stream.ToArray();
            }
        }
    }
}
