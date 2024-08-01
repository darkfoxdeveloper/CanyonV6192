namespace Canyon.Login.States.Responses
{
    public class RealmDataResponse
    {
        public Guid RealmID { get; set; }
        public string RealmName { get; set; }
        public int RealmStatus { get; set; }

        public string GameIPAddress { get; set; }
        public string RpcIPAddress { get; set; }
        public uint GamePort { get; set; }
        public uint RpcPort { get; set; }

        public string RealmUsername { get; set; }
        public string DatabaseHostname { get; set; }
        public string DatabaseUsername { get; set; }
        public string DatabaseSchema { get; set; }
        public int DatabasePort { get; set; }

        public bool Active { get; set; }
    }
}
