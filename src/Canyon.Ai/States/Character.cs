using Canyon.Ai.Managers;
using Canyon.Ai.Sockets.Packets;

namespace Canyon.Ai.States
{
    public sealed class Character : Role
    {
        public Character(uint identity)
        {
            Identity = identity;
        }

        private int mBattlePower;

        public int Metempsychosis { get; set; }
        public override int BattlePower => mBattlePower;
        public override uint MaxLife { get; }
        public override bool IsAlive => QueryStatus(StatusSet.GHOST) == null;
        public int Silvers { get; set; }
        public int ConquerPoints { get; set; }
        public int Nobility { get; set; }
        public int Syndicate { get; set; }
        public int SyndicatePosition { get; set; }
        public int Family { get; set; }
        public int FamilyPosition { get; set; }

        /// <inheritdoc />
        public override bool IsAttackable(Role attacker)
        {
            if (!IsAlive)
                return false;

            if (protectSecs.IsActive() || !protectSecs.IsTimeOut())
                return false;

            return true;
        }

        public async Task<bool> InitializeAsync(MsgAiPlayerLogin msg)
        {
            Name = msg.Name;
            Level = (byte)msg.Level;
            Metempsychosis = msg.Metempsychosis;
            StatusFlag1 = msg.Flag1;
            mBattlePower = msg.BattlePower;
            Life = (uint)msg.Life;
            Silvers = msg.Money;
            ConquerPoints = msg.ConquerPoints;
            Nobility = msg.Nobility;
            Syndicate = msg.Syndicate;
            SyndicatePosition = msg.SyndicatePosition;
            Family = msg.Family;
            FamilyPosition = msg.FamilyPosition;
            MapIdentity = msg.MapId;
            X = msg.X;
            Y = msg.Y;

            if ((Map = MapManager.GetMap(msg.MapId)) == null)
                return false;

            await EnterMapAsync(false);
            return true;
        }

        public async Task<bool> LogoutAsync()
        {
            await LeaveMapAsync(false);
            return true;
        }

        #region Protect

        private TimeOut protectSecs = new(10);

        public void SetProtection()
        {
            protectSecs.Startup(10);
        }

        public void ClearProtection()
        {
            protectSecs.Clear();
        }

        #endregion
    }
}
