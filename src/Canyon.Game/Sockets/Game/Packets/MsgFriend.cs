using Canyon.Game.Services.Managers;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using static Canyon.Game.States.User.Character;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgFriend : MsgBase<Client>
    {
        public uint Identity { get; set; }
        public MsgFriendAction Action { get; set; }
        public bool Online { get; set; }
        public int Nobility { get; set; }
        public int Gender { get; set; }
        public string Name { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();                 // 0
            Type = (PacketType)reader.ReadUInt16();      // 2
            Identity = reader.ReadUInt32();               // 4
            Action = (MsgFriendAction)reader.ReadByte(); // 8
            Online = reader.ReadBoolean();                // 9
            reader.ReadInt16();                           // 10
            Nobility = reader.ReadInt32();                           // 12
            Gender = reader.ReadInt32();                           // 16
            Name = reader.ReadString(16);                 // 20
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            using var writer = new PacketWriter();
            writer.Write((ushort)PacketType.MsgFriend);
            writer.Write(Identity);
            writer.Write((byte)Action);
            writer.Write(Online);
            writer.Write((ushort)0);
            writer.Write(Nobility);
            writer.Write(Gender);
            writer.Write(Name, 16);
            return writer.ToArray();
        }

        public enum MsgFriendAction : byte
        {
            RequestFriend = 10,
            NewFriend = 11,
            SetOnlineFriend = 12,
            SetOfflineFriend = 13,
            RemoveFriend = 14,
            AddFriend = 15,
            SetOnlineEnemy = 16,
            SetOfflineEnemy = 17,
            RemoveEnemy = 18,
            AddEnemy = 19
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Character target = null;
            switch (Action)
            {
                case MsgFriendAction.RequestFriend:
                    {
                        target = RoleManager.GetUser(Identity);
                        if (target == null)
                        {
                            await user.SendAsync(StrTargetNotOnline);
                            return;
                        }

                        if (user.FriendAmount >= user.MaxFriendAmount)
                        {
                            await user.SendAsync(StrFriendListFull);
                            return;
                        }

                        if (target.FriendAmount >= target.MaxFriendAmount)
                        {
                            await user.SendAsync(StrTargetFriendListFull);
                            return;
                        }

                        uint request = target.QueryRequest(RequestType.Friend);
                        if (request == user.Identity)
                        {
                            target.PopRequest(RequestType.Friend);
                            await target.CreateFriendAsync(user);
                        }
                        else
                        {
                            user.SetRequest(RequestType.Friend, target.Identity);
                            await target.SendRelationAsync(user);
                            await target.SendAsync(new MsgFriend
                            {
                                Action = MsgFriendAction.RequestFriend,
                                Identity = user.Identity,
                                Name = user.Name
                            });
                            await user.SendAsync(StrMakeFriendSent);
                        }

                        break;
                    }

                case MsgFriendAction.RemoveFriend:
                    await user.DeleteFriendAsync(Identity, true);
                    break;

                case MsgFriendAction.RemoveEnemy:
                    await user.DeleteEnemyAsync(Identity);
                    break;

                case MsgFriendAction.NewFriend:
                    {
                        target = RoleManager.GetUser(Identity);
                        if (target == null)
                        {
                            await user.SendAsync(StrTargetNotOnline);
                            return;
                        }

                        if (user.FriendAmount >= user.MaxFriendAmount)
                        {
                            await user.SendAsync(StrFriendListFull);
                            return;
                        }

                        if (target.FriendAmount >= target.MaxFriendAmount)
                        {
                            await user.SendAsync(StrTargetFriendListFull);
                            return;
                        }

                        uint request = target.QueryRequest(RequestType.Friend);
                        if (request == user.Identity)
                        {
                            target.PopRequest(RequestType.Friend);
                            await target.CreateFriendAsync(user);
                        }
                        break;
                    }

            }
        }
    }
}
