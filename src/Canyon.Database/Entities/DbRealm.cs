namespace Canyon.Database.Entities
{
    /// <summary>
    ///     Realms are configured instances of the game server. This class defines routing
    ///     details for authenticated clients to be redirected to. Redirection involves
    ///     access token leasing, provided by the game server via RPC. Security for RPC stream
    ///     encryption is defined in this class.
    /// </summary>
    [Table("realm")]
    public class DbRealm
    {
        [NotMapped] public object Server;

        [Key] public virtual uint RealmID { get; set; }

        public virtual string Name { get; set; }
        public virtual ushort AuthorityID { get; set; }
        public virtual string GameIPAddress { get; set; }
        public virtual string RpcIPAddress { get; set; }
        public virtual uint GamePort { get; set; }
        public virtual uint RpcPort { get; set; }
        public virtual RealmStatus Status { get; set; }
        public virtual string Username { get; set; }
        public virtual string Password { get; set; }
        public virtual int LastPing { get; set; }
        public virtual string DatabaseHost { get; set; }
        public virtual string DatabaseUser { get; set; }
        public virtual string DatabasePass { get; set; }
        public virtual string DatabaseSchema { get; set; }
        public virtual string DatabasePort { get; set; }

        public T GetServer<T>()
        {
            return (T)Server;
        }

        public enum RealmStatus : byte
        {
            Offline,
            Busy,
            Full,
            Online
        }
    }
}