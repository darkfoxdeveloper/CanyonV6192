using Canyon.Database.Entities;
using Canyon.Game.Services.Managers;
using Canyon.Game.States;
using Canyon.Game.States.Syndicates;
using Canyon.Game.States.User;
using Canyon.Game.States.World;
using Canyon.Network.Packets;

namespace Canyon.Game.Sockets.Game.Packets
{
    public sealed class MsgTaskDialog : MsgBase<Client>
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<MsgTaskDialog>();

        public MsgTaskDialog()
        {
            Text = string.Empty;
        }

        public int Timestamp { get; set; }
        public uint TaskIdentity { get; set; }
        public ushort Data { get; set; }
        public byte OptionIndex { get; set; }
        public TaskInteraction InteractionType { get; set; }
        public string Text { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            using var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType)reader.ReadUInt16();
            Timestamp = reader.ReadInt32();
            TaskIdentity = reader.ReadUInt32();
            Data = reader.ReadUInt16();
            OptionIndex = reader.ReadByte();
            InteractionType = (TaskInteraction)reader.ReadByte();
            List<string> strings = reader.ReadStrings();
            Text = strings.Count > 0 ? strings[0] : "";
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
            writer.Write((ushort)PacketType.MsgTaskDialog);
            writer.Write(Timestamp);
            writer.Write(TaskIdentity);
            writer.Write(Data);
            writer.Write(OptionIndex);
            writer.Write((byte)InteractionType);
            writer.Write(new List<string> { Text });
            return writer.ToArray();
        }

        public enum TaskInteraction : byte
        {
            ClientRequest = 0,
            Dialog = 1,
            Option = 2,
            Input = 3,
            Avatar = 4,
            LayNpc = 5,
            MessageBox = 6,
            Finish = 100,
            Answer = 101,
            TextInput = 102,
            UpdateWindow = 112
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (InteractionType)
            {
                case TaskInteraction.MessageBox:
                    {
                        if (user.MessageBox != null)
                        {
                            if (OptionIndex == 0)
                            {
                                await user.MessageBox.OnCancelAsync();
                            }
                            else
                            {
                                await user.MessageBox.OnAcceptAsync();
                            }

                            user.MessageBox = null;
                        }
                        else
                        {
                            string idTask = user.GetTaskId(OptionIndex);
                            user.ClearTaskId();

                            if (uint.TryParse(idTask, out var id))
                            {
                                await GameAction.ExecuteActionAsync(id, user, RoleManager.GetRole(user.InteractingNpc), user.UserPackage[user.InteractingItem], Text);
                            }
                            else
                            {
                                string function = LuaScriptManager.ParseTaskDialogAnswerToScript(idTask);
                                if (function.StartsWith("NULL"))
                                {
                                    user.CancelInteraction();
                                    return;
                                }

                                LuaScriptManager.Run(user, RoleManager.GetRole(user.InteractingNpc), user.UserPackage[user.InteractingItem], Text, function);
                            }
                        }

                        break;
                    }

                case TaskInteraction.Answer:
                    {
                        if (OptionIndex is 0 or byte.MaxValue)
                        {
                            return;
                        }

                        Role targetRole = RoleManager.GetRole(user.InteractingNpc);
                        if (targetRole != null
                            && targetRole.MapIdentity != 5000
                            && targetRole.MapIdentity != user.MapIdentity
                            && targetRole.MapIdentity != user.Map.BaseMapId)
                        {
                            user.CancelInteraction();
                            return;
                        }

                        if (targetRole != null
                            && targetRole.GetDistance(user) > Screen.VIEW_SIZE)
                        {
                            user.CancelInteraction();
                            return;
                        }

                        if (user.InteractingNpc == 0 && user.InteractingItem == 0)
                        {
                            user.CancelInteraction();
                            return;
                        }

                        string idTask = user.GetTaskId(OptionIndex);
                        if (uint.TryParse(idTask, out var id))
                        {
                            DbTask task = EventManager.GetTask(id);
                            if (task == null)
                            {
                                user.CancelInteraction();

                                if (OptionIndex != 0)
                                {
                                    if (user.IsGm() && id != 0)
                                    {
                                        await user.SendAsync($"Could not find InteractionAsnwer for task {idTask}");
                                    }
                                }

                                return;
                            }

                            user.ClearTaskId();
                            await GameAction.ExecuteActionAsync(user.TestTask(task) ? task.IdNext : task.IdNextfail, user,
                                targetRole, user.UserPackage[user.InteractingItem], Text);
                        }
                        else
                        {
                            string function = LuaScriptManager.ParseTaskDialogAnswerToScript(idTask);
                            if (function.StartsWith("NULL"))
                            {
                                user.CancelInteraction();
                                return;
                            }

                            LuaScriptManager.Run(user, targetRole, user.UserPackage[user.InteractingItem], Text, function);
                        }
                        break;
                    }

                case TaskInteraction.TextInput:
                    {
                        if (TaskIdentity == 31100)
                        {
                            if (user.SyndicateIdentity == 0 ||
                                user.SyndicateRank < SyndicateMember.SyndicateRank.DeputyLeader)
                            {
                                return;
                            }

                            await user.Syndicate.KickOutMemberAsync(user, Text);
                            await user.Syndicate.SendMembersAsync(0, user);
                            return;
                        }

                        if (TaskIdentity is > 20000000 and < 20099999)
                        {
                            await GameAction.ExecuteActionAsync(TaskIdentity, user, null, null, string.Empty);
                        }
                        break;
                    }

                default:
                    logger.LogWarning($"MsgTaskDialog: {Type}, {InteractionType} unhandled\n{PacketDump.Hex(Encode())}");
                    break;
            }
        }
    }
}