namespace Canyon.Login.States.Requests
{
    public class RealmSyncRequest
    {
        public Guid RealmId { get; set; }
        public int CurrentStatus { get; set; }
        public int CurrentOnlinePlayers { get; set; }
        public int MaxOnlinePlayers { get; set; }
    }
}
