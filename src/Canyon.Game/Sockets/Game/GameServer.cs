using Canyon.Game.Services.Managers;
using Canyon.Game.Services.Processors;
using Canyon.Game.Sockets.Ai.Packets;
using Canyon.Game.Sockets.Game.Packets;
using Canyon.Game.States.User;
using Canyon.Network.Packets;
using Canyon.Network.Sockets;
using IdentityModel;
using System.Net.Sockets;

namespace Canyon.Game.Sockets.Game
{
    public sealed class GameServer : TcpServerListener<Client>
    {
        // Fields and Properties
        private readonly PacketProcessor<Client> processor;
        private static readonly ILogger logger = LogFactory.CreateLogger<GameServer>();

        /// <summary>
        ///     Instantiates a new instance of <see cref="Server" /> by initializing the
        ///     <see cref="PacketProcessor" /> for processing packets from the players using
        ///     channels and worker threads. Initializes the TCP server listener.
        /// </summary>
        /// <param name="config">The server's read configuration file</param>
        public GameServer(ServerConfiguration config)
            : base(config.Realm.MaxOnlinePlayers, exchange: true, footerLength: 8)
        {
            processor = new PacketProcessor<Client>(ProcessAsync, config.Realm.Processors);
            _ = processor.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task<Client> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            uint partition = processor.SelectPartition();
            var client = new Client(socket, buffer, partition);

            await client.NdDiffieHellman.ComputePublicKeyAsync();

            await NextBytesAsync(client.NdDiffieHellman.DecryptionIV);
            await NextBytesAsync(client.NdDiffieHellman.EncryptionIV);

            var handshakeRequest = new MsgHandshake(
                client.NdDiffieHellman,
                client.NdDiffieHellman.EncryptionIV,
                client.NdDiffieHellman.DecryptionIV);

            await handshakeRequest.RandomizeAsync();
            await client.SendAsync(handshakeRequest);
            return client;
        }

        /// <summary>
        ///     Invoked by the server listener's Exchanging method to process the client
        ///     response from the Diffie-Hellman Key Exchange. At this point, the raw buffer
        ///     from the client has been decrypted and is ready for direct processing.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        /// <param name="buffer">Packet buffer to be processed</param>
        /// <returns>True if the exchange was successful.</returns>
        protected override bool Exchanged(Client actor, ReadOnlySpan<byte> buffer)
        {
            try
            {
                var msg = new MsgHandshake();
                msg.Decode(buffer.ToArray());

                actor.NdDiffieHellman.ComputePrivateKey(msg.ClientKey);

                actor.Cipher.GenerateKeys(new object[]
                {
                    actor.NdDiffieHellman.ProcessDHSecret(),
                    actor.NdDiffieHellman.EncryptionIV,
                    actor.NdDiffieHellman.DecryptionIV
                });

                actor.ReceiveTimeOutSeconds = 5; // somehow exchange is breaking after this sometimes

                actor.NdDiffieHellman = null;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }

        /// <summary>
        ///     Invoked by the server listener's Receiving method to process a completed packet
        ///     from the actor's socket pipe. At this point, the packet has been assembled and
        ///     split off from the rest of the buffer.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        /// <param name="packet">Packet bytes to be processed</param>
        protected override void Received(Client actor, ReadOnlySpan<byte> packet)
        {
            Kernel.NetworkMonitor.Receive(packet.Length);
            if (actor.ConnectionStage == TcpServerActor.Stage.Exchange)
            {
                actor.ReceiveTimeOutSeconds = 900;
                actor.ConnectionStage = TcpServerActor.Stage.Receiving;
            }

            processor.QueueRead(actor, packet.ToArray());
        }

        public override void Send(Client actor, ReadOnlySpan<byte> packet)
        {
            processor.QueueWrite(actor, packet.ToArray());
        }

        public override void Send(Client actor, ReadOnlySpan<byte> packet, Func<Task> task)
        {
            processor.QueueWrite(actor, packet.ToArray(), task);
        }

        /// <summary>
        ///     Invoked by one of the server's packet processor worker threads to process a
        ///     single packet of work. Allows the server to process packets as individual
        ///     messages on a single channel.
        /// </summary>
        /// <param name="actor">Actor requesting packet processing</param>
        /// <param name="packet">An individual data packet to be processed</param>
        private async Task ProcessAsync(Client actor, byte[] packet)
        {
            // Read in TQ's binary header
            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType)BitConverter.ToUInt16(packet, 2);

            // Validate connection
            if (!actor.Socket.Connected)
            {
                return;
            }

            try
            {
                // Switch on the packet type
                MsgBase<Client> msg = null;
                switch (type)
                {
                    case PacketType.MsgConnect:
                        msg = new MsgConnect();
                        break;

                    case PacketType.MsgRegister:
                        msg = new MsgRegister();
                        break;

                    case PacketType.MsgTalk:
                        msg = new MsgTalk();
                        break;

                    case PacketType.MsgWalk:
                        msg = new MsgWalk();
                        break;

                    case PacketType.MsgItem:
                        msg = new MsgItem();
                        break;

                    case PacketType.MsgAction:
                        msg = new MsgAction();
                        break;

                    case PacketType.MsgName:
                        msg = new MsgName();
                        break;

                    case PacketType.MsgNationality:
                        msg = new MsgNationality();
                        break;

                    case PacketType.MsgPlayerAttribInfo:
                        msg = new MsgPlayerAttribInfo();
                        break;

                    case PacketType.MsgTraining:
                        msg = new MsgTraining();
                        break;

                    case PacketType.MsgPackage:
                        msg = new MsgPackage();
                        break;

                    case PacketType.MsgDataArray:
                        msg = new MsgDataArray();
                        break;

                    case PacketType.MsgEquipLock:
                        msg = new MsgEquipLock();
                        break;

                    case PacketType.MsgGemEmbed:
                        msg = new MsgGemEmbed();
                        break;

                    case PacketType.MsgQuench:
                        msg = new MsgQuench();
                        break;

                    case PacketType.MsgSolidify:
                        msg = new MsgSolidify();
                        break;

                    case PacketType.MsgTaskDialog:
                        msg = new MsgTaskDialog();
                        break;

                    case PacketType.MsgNpc:
                        msg = new MsgNpc();
                        break;

                    case PacketType.MsgInviteTrans:
                        msg = new MsgInviteTrans();
                        break;

                    case PacketType.Msg2ndPsw:
                        msg = new Msg2ndPsw();
                        break;

                    case PacketType.MsgLottery:
                        msg = new MsgLottery();
                        break;

                    case PacketType.MsgPeerage:
                        msg = new MsgPeerage();
                        break;

                    case PacketType.MsgSyndicate:
                        msg = new MsgSyndicate();
                        break;

                    case PacketType.MsgSynpOffer:
                        msg = new MsgSynpOffer();
                        break;

                    case PacketType.MsgSynMemberList:
                        msg = new MsgSynMemberList();
                        break;

                    case PacketType.MsgFactionRankInfo:
                        msg = new MsgFactionRankInfo();
                        break;

                    case PacketType.MsgTotemPole:
                        msg = new MsgTotemPole();
                        break;

                    case PacketType.MsgWeaponsInfo:
                        msg = new MsgWeaponsInfo();
                        break;

                    case PacketType.MsgFamily:
                        msg = new MsgFamily();
                        break;

                    case PacketType.MsgFamilyOccupy:
                        msg = new MsgFamilyOccupy();
                        break;

                    case PacketType.MsgQualifyingInteractive:
                        msg = new MsgQualifyingInteractive();
                        break;

                    case PacketType.MsgQualifyingFightersList:
                        msg = new MsgQualifyingFightersList();
                        break;

                    case PacketType.MsgQualifyingRank:
                        msg = new MsgQualifyingRank();
                        break;

                    case PacketType.MsgQualifyingSeasonRankList:
                        msg = new MsgQualifyingSeasonRankList();
                        break;

                    case PacketType.MsgQualifyingDetailInfo:
                        msg = new MsgQualifyingDetailInfo();
                        break;

                    case PacketType.MsgTeam:
                        msg = new MsgTeam();
                        break;

                    case PacketType.MsgGuide:
                        msg = new MsgGuide();
                        break;

                    case PacketType.MsgGuideContribute:
                        msg = new MsgGuideContribute();
                        break;

                    case PacketType.MsgGodExp:
                        msg = new MsgGodExp();
                        break;

                    case PacketType.MsgFriend:
                        msg = new MsgFriend();
                        break;

                    case PacketType.MsgTradeBuddy:
                        msg = new MsgTradeBuddy();
                        break;

                    case PacketType.MsgInteract:
                        msg = new MsgInteract();
                        break;

                    case PacketType.MsgTrade:
                        msg = new MsgTrade();
                        break;

                    case PacketType.MsgTaskStatus:
                        msg = new MsgTaskStatus();
                        break;

                    case PacketType.MsgTaskDetailInfo:
                        msg = new MsgTaskDetailInfo();
                        break;

                    case PacketType.MsgFlower:
                        msg = new MsgFlower();
                        break;

                    case PacketType.MsgRank:
                        msg = new MsgRank();
                        break;

                    case PacketType.MsgSuitStatus:
                        msg = new MsgSuitStatus();
                        break;

                    case PacketType.MsgPigeon:
                        msg = new MsgPigeon();
                        break;

                    case PacketType.MsgSubPro:
                        msg = new MsgSubPro();
                        break;

                    case PacketType.MsgAuction:
                        msg = new MsgAuction();
                        break;

                    case PacketType.MsgAuctionItem:
                        msg = new MsgAuctionItem();
                        break;

                    case PacketType.MsgAuctionQuery:
                        msg = new MsgAuctionQuery();
                        break;

                    case PacketType.MsgMailList:
                        msg = new MsgMailList();
                        break;

                    case PacketType.MsgMailOperation:
                        msg = new MsgMailOperation();
                        break;

                    case PacketType.MsgTitle:
                        msg = new MsgTitle();
                        break;

                    case PacketType.MsgTrainingVitality:
                        msg = new MsgTrainingVitality();
                        break;

                    case PacketType.MsgOwnKongfuBase:
                        msg = new MsgOwnKongfuBase();
                        break;

                    case PacketType.MsgOwnKongfuImproveFeedback:
                        msg = new MsgOwnKongfuImproveFeedback();
                        break;

                    case PacketType.MsgQuiz:
                        msg = new MsgQuiz();
                        break;

                    case PacketType.MsgMapItem:
                        msg = new MsgMapItem();
                        break;

                    case PacketType.MsgAchievement:
                        msg = new MsgAchievement();
                        break;

                    case PacketType.MsgHangUp:
                        msg = new MsgHangUp();
                        break;

                    case PacketType.MsgWarFlag:
                        msg = new MsgWarFlag();
                        break;

                    case PacketType.MsgSelfSynMemAwardRank:
                        msg = new MsgSelfSynMemAwardRank();
                        break;

                    case PacketType.MsgArenicWitness:
                        msg = new MsgArenicWitness();
                        break;

                    case PacketType.MsgSuperFlag:
                        msg = new MsgSuperFlag();
                        break;

                    case PacketType.MsgTeamArenaHeroData:
                        msg = new MsgTeamArenaHeroData();   
                        break;

                    case PacketType.MsgTeamArenaYTop10List:
                        msg = new MsgTeamArenaYTop10List();
                        break;

                    case PacketType.MsgTeamArenaFightingTeamList:
                        msg = new MsgTeamArenaFightingTeamList();
                        break;

                    case PacketType.MsgTeamArenaInteractive:
                        msg = new MsgTeamArenaInteractive();
                        break;

                    case PacketType.MsgOwnKongRank:
                        msg = new MsgOwnKongRank();
                        break;

                    case PacketType.MsgTeamArenaRank:
                        msg = new MsgTeamArenaRank();
                        break;

                    case PacketType.MsgNpcInfo:
                        msg = new MsgNpcInfo();
                        break;

                    case PacketType.MsgSynRecuitAdvertising:
                        msg = new MsgSynRecuitAdvertising();
                        break;

                    case PacketType.MsgSynRecruitAdvertisingOpt:
                        msg = new MsgSynRecruitAdvertisingOpt();
                        break;

                    case PacketType.MsgSynRecruitAdvertisingList:
                        msg = new MsgSynRecruitAdvertisingList();
                        break;

                    case PacketType.MsgPkStatistic:
                        msg = new MsgPkStatistic();
                        break;

                    case PacketType.MsgVipUserHandle:
                        msg = new MsgVipUserHandle();
                        break;

                    case PacketType.MsgMessageBoard:
                        msg = new MsgMessageBoard();
                        break;

                    case PacketType.MsgGuideInfo:
                        msg = new MsgGuideInfo();
                        break;

                    case PacketType.MsgPkEliteMatchInfo:
                        msg = new MsgPkEliteMatchInfo();
                        break;

                    case PacketType.MsgElitePkGameRankInfo:
                        msg = new MsgElitePKGameRankInfo();
                        break;

                    case PacketType.MsgMentorPlayer:
                        msg = new MsgMentorPlayer();
                        break;

                    case PacketType.MsgTransportor:
                        msg = new MsgTransportor();
                        break;

                    case PacketType.MsgMeteSpecial:
                        msg = new MsgMeteSpecial();
                        break;

                    case PacketType.MsgAllot:
                        msg = new MsgAllot();
                        break;

                    case PacketType.MsgPing:
                    case PacketType.MsgData:
                        return;

                    case PacketType.MsgActivityTaskReward:
                        msg = new MsgActivityTaskReward();
                        break;

                    case PacketType.MsgActivityTask:
                        msg = new MsgActivityTask();
                        break;

                    case PacketType.MsgProcessGoalTaskOpt:
                        msg = new MsgProcessGoalTaskOpt();
                        break;

                    case PacketType.MsgChangeName:
                        msg = new MsgChangeName();
                        break;

                    case PacketType.MsgOwnKongfuPkSetting:
                        msg = new MsgOwnKongfuPKSetting();
                        break;

                    case PacketType.MsgRaceTrackProp:
                        msg = new MsgRaceTrackProp();
                        break;

                    case PacketType.MsgTotemsRegister:
                        msg = new MsgTotemsRegister();
                        break;

                    case PacketType.MsgMagicInfo:
                        msg = new MsgMagicInfo();
                        break;

                    case PacketType.MsgTrainingVitalityProtect:
                        msg = new MsgTrainingVitalityProtect();
                        break;

                    case PacketType.MsgGLRankingList:
                        msg = new MsgGLRankingList();
                        break;

                    case PacketType.MsgAthleteShop:
                        msg = new MsgAthleteShop();
                        break;

                    case PacketType.MsgInnerStrengthOpt:
                        msg = new MsgInnerStrengthOpt();
                        break;

                    case PacketType.MsgSponsor:
                        msg = new MsgSponsor();
                        break;

                    default:
                        {
                            logger.LogWarning($"Missing packet {type}, Length {length}\n{PacketDump.Hex(packet)}");
                            if (actor.Character.IsGm())
                            {
                                await actor.SendAsync(new MsgTalk(actor.Identity, TalkChannel.Service,
                                                                  string.Format("Missing packet {0}, Length {1}",
                                                                                type, length)));
                            }
                            return;
                        }
                }

                // Decode packet bytes into the structure and process
                msg.Decode(packet);
                // Packet has been decrypted and now will be queued in the region processor
                if (actor.Character?.Map != null)
                {
                    Character user = RoleManager.GetUser(actor.Character.Identity);
                    if (user == null || !user.Client.GUID.Equals(actor.GUID))
                    {
                        actor.Disconnect();
                        if (user != null)
                        {
                            await RoleManager.KickOutAsync(actor.Identity);
                        }
                        return;
                    }

                    Kernel.Services.Processor.Queue(actor.Character.Map.Partition, () => msg.ProcessAsync(actor));
                }
                else
                {
                    // we will not send all packets to NO_MAP_GROUP
                    // after this point we are only letting 1052 and first 10010 packet
                    if (type == PacketType.MsgConnect 
                        || type == PacketType.MsgRegister 
                        || type == PacketType.MsgRank
                        || (msg is MsgAction action && action.Action != MsgAction.ActionType.MapJump))
                    {
                        Kernel.Services.Processor.Queue(ServerProcessor.NO_MAP_GROUP, () => msg.ProcessAsync(actor));
                    }
                    else
                    {
                        logger.LogWarning("Message [{}] sent out of map.", type);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "{Message}", e.Message);
            }
        }

        /// <summary>
        ///     Invoked by the server listener's Disconnecting method to dispose of the actor's
        ///     resources. Gives the server an opportunity to cleanup references to the actor
        ///     from other actors and server collections.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        protected override async void Disconnected(Client actor)
        {
            if (actor == null)
            {
                logger.LogError(@"Disconnected with actor null ???");
                return;
            }

            processor.DeselectPartition(actor.Partition);

            var fromCreation = false;
            if (actor.Creation != null)
            {
                Kernel.Registration.Remove(actor.Creation.Token);
                fromCreation = true;
            }

            if (actor.Character != null)
            {
                logger.LogInformation($"{actor.Character.Name} has logged out.");

                await BroadcastNpcMsgAsync(new MsgAiPlayerLogout
                {
                    Timestamp = Environment.TickCount,
                    Id = actor.Character.Identity
                });

                async Task onDisconnectTask()  
                {
                    try
                    {
                        await actor.Character.OnDisconnectAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex, "Error on disconnect!!! {}", ex.Message);
                    }
                    finally
                    {
                        RoleManager.ForceLogoutUser(actor.Character.Identity);
                    }
                };
                Kernel.Services.Processor.Queue(ServerProcessor.NO_MAP_GROUP, onDisconnectTask);
            }
            else
            {
                if (fromCreation)
                {
                    logger.LogInformation($"{actor.AccountIdentity} has created a new character and has logged out.");
                }
                else
                {
                    logger.LogInformation($"[{actor.IpAddress}] {actor.AccountIdentity} has logged out.");
                }
            }
        }
    }
}
