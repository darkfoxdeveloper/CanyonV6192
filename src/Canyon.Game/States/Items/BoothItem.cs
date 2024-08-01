namespace Canyon.Game.States.Items
{
    public class BoothItem
    {
        public Item Item { get; private set; }
        public uint Identity => Item?.Identity ?? 0;
        public uint Value { get; private set; }
        public bool IsSilver { get; private set; }

        public bool Create(Item item, uint dwMoney, bool bSilver)
        {
            Item = item;
            Value = dwMoney;
            IsSilver = bSilver;

            return Value > 0;
        }
    }
}
