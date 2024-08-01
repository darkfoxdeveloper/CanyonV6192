﻿using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTeamArenaFightingMemberInfo : MsgBase<Client>
    {
        public enum ShowType
        {
            Neutral,
            Opponent
        }

        public ShowType Mode { get; set; }
        public uint TeamId { get; set; }
        public List<TeamMemberInfoStruct> Members { get; set; } = new();

        public override byte[] Encode()
        {
            using PacketWriter writer = new();
            writer.Write((ushort) PacketType.MsgTeamArenaFightingMemberInfo);
            writer.Write((int)Mode);
            writer.Write(TeamId);
            writer.Write(Members.Count);
            foreach (var member in Members)
            {
                writer.Write(member.UserId);
                writer.Write(member.Level);
                writer.Write(member.Profession);
                writer.Write(member.Mesh);
                writer.Write(member.Rank);
                writer.Write(member.Score);
                writer.Write(member.Name, 16);
            }
            return writer.ToArray();
        }

        public struct TeamMemberInfoStruct
        {
            public uint UserId { get; set; }
            public int Level { get; set; }
            public int Profession { get; set; }
            public uint Mesh { get; set; }
            public int Rank { get; set; }
            public int Score { get; set; }
            public string Name { get; set; }
        }
    }
}
