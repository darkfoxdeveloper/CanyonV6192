using Canyon.Database.Entities;
using Canyon.Game.States;
using Canyon.Game.States.User;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgWeaponSkill : MsgBase<Client>
    {
        public MsgWeaponSkill(DbWeaponSkill ws)
        {
            Identity = ws.Type;
            Level = ws.Level;
            if (Level < Role.MAX_WEAPONSKILLLEVEL)
            {
                Experience = ws.Experience;
                LevelExperience = WeaponSkill.RequiredExperience[Level];
            }
        }

        public uint Identity { get; set; }
        public uint Level { get; set; }
        public uint Experience { get; set; }
        public uint LevelExperience { get; set; }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgWeaponSkill);
            writer.Write(Identity);
            writer.Write(Level);
            writer.Write(Experience);
            writer.Write(LevelExperience);
            return writer.ToArray();
        }
    }
}
