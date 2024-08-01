using Canyon.Game.States.User;
using Canyon.Network.Packets.Ai;

namespace Canyon.Game.Sockets.Ai.Packets
{
    public sealed class MsgAiPlayerLogin : MsgAiPlayerLogin<AiClient>
    {
        public MsgAiPlayerLogin(Character user)
        {
            Id = user.Identity;
            Name = user.Name;
            BattlePower = user.BattlePower;
            Level = user.Level;
            Metempsychosis = user.Metempsychosis;
            Flag1 = user.StatusFlag1;
            Flag2 = user.StatusFlag2;
            Flag3 = user.StatusFlag3;
            Life = (int)Math.Max(1, user.Life);
            MaxLife = (int)user.MaxLife;
            Money = (int)user.Silvers;
            ConquerPoints = (int)user.ConquerPoints;
            Syndicate = user.SyndicateIdentity;
            SyndicatePosition = (int)user.SyndicateRank;
            Family = (int)user.FamilyIdentity;
            FamilyPosition = (int)user.FamilyPosition;
            MapId = user.MapIdentity;
            X = user.X;
            Y = user.Y;
            Nobility = (int)user.NobilityRank;
        }
    }
}