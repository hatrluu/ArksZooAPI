using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;

namespace ArksZooAPI
{
    public class A2S_PLAYER
    {
        public struct Player
        {
            public Player(ref BinaryReader br)
            {
                Index = br.ReadByte();
                Name = ReadNullTerminatedString(ref br);
                Score = br.ReadInt32();
                Duration = br.ReadSingle();
            }
            public byte Index { get; set; }
            public string Name { get; set; }
            public int Score { get; set; }
            public float Duration { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        // \xFF\xFF\xFF\xFFU\xFF\xFF\xFF\xFF
        public static readonly byte[] REQUEST = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };

        public byte Header { get; set; }        // D
        public Player[] Players { get; set; }

        public A2S_PLAYER(IPEndPoint ep)
        {
            UdpClient udp = new UdpClient();
            udp.Send(REQUEST, REQUEST.Length, ep); // Request Challenge.
            byte[] challenge_response = udp.Receive(ref ep);
            if (challenge_response.Length == 9 && challenge_response[4] == 0x41) //B
            {
                challenge_response[4] = 0x55; //U
                // \xFF\xFF\xFF\xFFU[CHALLENGE]
                udp.Send(challenge_response, challenge_response.Length, ep); // Request data.

                MemoryStream ms = new MemoryStream(udp.Receive(ref ep));    // Saves the received data in a memory buffer
                BinaryReader br = new BinaryReader(ms, Encoding.UTF8);      // A binary reader that treats charaters as Unicode 8-bit
                ms.Seek(4, SeekOrigin.Begin);   // skip the 4 0xFFs
                Header = br.ReadByte(); // D
                Players = new Player[br.ReadByte()];
                for (int i = 0; i < Players.Length; i++)
                    Players[i] = new Player(ref br);
                br.Close();
                ms.Close();
                udp.Close();
            }
            else
                throw new Exception("Response invalid.");

        }

        /// <summary>Reads a null-terminated string into a .NET Framework compatible string.</summary>
        /// <param name="input">Binary reader to pull the null-terminated string from.  Make sure it is correctly positioned in the stream before calling.</param>
        /// <returns>String of the same encoding as the input BinaryReader.</returns>
        public static string ReadNullTerminatedString(ref BinaryReader input)
        {
            StringBuilder sb = new StringBuilder();
            char read = input.ReadChar();
            while (read != '\x00')
            {
                sb.Append(read);
                read = input.ReadChar();
            }
            return sb.ToString();
        }
    }
}
