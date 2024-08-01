using Canyon.Database.Entities;
using Canyon.Game.States;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using static Canyon.Game.Sockets.Game.Packets.MsgItemStatus;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgQuench : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgQuench>();

        public PurificationMode Action { get; set; }
        public uint MainIdentity { get; set; }
        public uint MinorIdentity { get; set; }

        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Action = (PurificationMode)reader.ReadUInt32();
            MainIdentity = reader.ReadUInt32();
            MinorIdentity = reader.ReadUInt32();
        }

        public enum PurificationMode
        {
            /// <summary>
            /// Refineries.
            /// </summary>
            PurifyRerinery,
            /// <summary>
            /// Dragon Soul artifacts.
            /// </summary>
            PurifyArtifact,
            Solidify
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (!user.IsAlive)
            {
                return;
            }

            Item item = user.UserPackage.FindByIdentity(MainIdentity);
            Item minor = user.UserPackage[MinorIdentity];

            if (item == null || minor == null)
            {
                return;
            }

            if (Action == PurificationMode.PurifyRerinery)
            {
                user.InteractingItem = minor.Identity;
                if (await GameAction.ExecuteActionAsync(minor.Itemtype.IdAction, user, null, item, string.Empty))
                {
                    await user.UserPackage.SpendItemAsync(minor);
                }
            }
            else if (Action == PurificationMode.PurifyArtifact)
            {
                if (item.Position != Item.ItemPosition.Inventory)
                {
                    return;
                }

                if (!minor.IsArtifact())
                {
                    return;
                }

                if (item.RequiredLevel < minor.RequiredLevel)
                {
                    return;
                }

                if (minor.Magic1 != 0) // specific weapon
                {
                    string requiredSubtypeString = minor.Itemtype.Magic1.ToString();
                    if (requiredSubtypeString.Length % 3 == 0)
                    {
                        uint[] subTypes = new uint[requiredSubtypeString.Length / 3];
                        for (int i = 0; i < subTypes.Length; i++)
                        {
                            subTypes[i] = uint.Parse(requiredSubtypeString.Substring(i * 3, 3));
                        }
                        if (subTypes.All(x => x != item.GetItemSubType()))
                        {
                            return;
                        }
                    }
                    else
                    //if (requiredSubtypeString.Length == 1)
                    {
                        if ((byte)item.GetItemSort() != minor.Itemtype.Magic1)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    ItemQuench.QuenchPosition requiredPosition = (ItemQuench.QuenchPosition)(minor.Itemtype.Magic2);
                    switch (requiredPosition)
                    {
                        case ItemQuench.QuenchPosition.Headwear:
                            {
                                if (!item.IsHelmet())
                                {
                                    return;
                                }

                                break;
                            }
                        case ItemQuench.QuenchPosition.Neck:
                            {
                                if (!item.IsNeck())
                                {
                                    return;
                                }

                                break;
                            }
                        case ItemQuench.QuenchPosition.Armor:
                            {
                                if (!item.IsArmor())
                                {
                                    return;
                                }

                                break;
                            }
                        case ItemQuench.QuenchPosition.SingleHand:
                            {
                                if (!item.IsWeaponOneHand() && !item.IsWeaponProBased())
                                {
                                    return;
                                }

                                break;
                            }
                        case ItemQuench.QuenchPosition.DoubleHand:
                            {
                                if (!item.IsWeaponTwoHand())
                                {
                                    return;
                                }

                                break;
                            }
                        case ItemQuench.QuenchPosition.Shield:
                            {
                                if (!item.IsShield())
                                {
                                    return;
                                }

                                break;
                            }
                        case ItemQuench.QuenchPosition.Ring:
                            {
                                if (!item.IsRing() && !item.IsBangle())
                                {
                                    return;
                                }

                                break;
                            }
                        case ItemQuench.QuenchPosition.Shoes:
                            {
                                if (!item.IsShoes())
                                {
                                    return;
                                }

                                break;
                            }
                        default:
                            {
                                return;
                            }
                    }
                }

                if (!await user.UserPackage.SpendMeteorsAsync((int)minor.Itemtype.MeteorAmount))
                {
                    return;
                }

                await user.UserPackage.SpendItemAsync(minor);

                int days = 7;
                switch (user.VipLevel)
                {
                    case 1:
                    case 2:
                        {
                            days += (int)user.VipLevel;
                            break;
                        }
                    case 3:
                        {
                            days += 4;
                            break;
                        }
                    case 4:
                    case 5:
                    case 6:
                        {
                            days += 7;
                            break;
                        }
                }

                DbItemStatus status = new()
                {
                    ItemId = item.Identity,
                    Level = minor.Itemtype.Phase,
                    RealSeconds = (uint)UnixTimestamp.FromDateTime(DateTime.Now.AddDays(days)),
                    Status = minor.Type,
                    UserId = user.Identity
                };
                var data = await item.Quench.AppendAsync(status);
                item.Quench.ActivateNextArtifact();

                await item.Quench.SendToAsync(user);
                await user.SendAsync(new MsgItemStatus(data, ItemStatusType.PurificationEffect));
            }
            else
            {
                logger.LogWarning($"MsgQuench unhandled action {Action}\n{PacketDump.Hex(Encode())}");
            }
        }
    }
}
