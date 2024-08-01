namespace Canyon.Login.States
{
    public class Player
    {
        public Player()
        {
            LoginTime = DateTime.Now;
        }

        public Guid RealmId { get; set; }
        public uint AccountId { get; set; }
        public DateTime LoginTime { get; init; }
    }
}
