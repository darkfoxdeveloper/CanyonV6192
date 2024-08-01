using Canyon.Game.States.MessageBoxes;
using Canyon.Game.States.User;

namespace Canyon.Game.States.Syndicates
{
    public sealed class SyndicateRelationBox : MessageBox
    {
        private Character senderUser;
        private Character targetUser;

        private Syndicate senderSyndicate;
        private Syndicate targetSyndicate;

        public SyndicateRelationBox(Character owner)
            : base(owner)
        {
        }

        public async Task<bool> CreateAsync(Character sender, Character target, RelationType type)
        {
            if (sender?.Syndicate == null || target?.Syndicate == null)
            {
                return false;
            }

            if (sender.SyndicateIdentity == target.SyndicateIdentity)
            {
                return false;
            }

            if (sender.Syndicate.Deleted || target.Syndicate.Deleted)
            {
                return false;
            }

            if (!sender.Syndicate.Leader.IsOnline || !target.Syndicate.Leader.IsOnline)
            {
                return false;
            }

            senderSyndicate = sender.Syndicate;
            targetSyndicate = target.Syndicate;

            senderUser = sender;
            targetUser = target;

            Message = string.Format(Language.StrSyndicateAllianceRequest, sender.Name, sender.SyndicateName);
            await SendAsync();
            return true;
        }

        public override async Task OnAcceptAsync()
        {
            await senderSyndicate.CreateAllianceAsync(senderUser, targetSyndicate);
        }

        public override async Task OnCancelAsync()
        {
            await senderSyndicate.SendAsync(string.Format(Language.StrSyndicateAllianceDeny, targetSyndicate.Name));
        }

        public enum RelationType
        {
            Ally
        }
    }
}
