namespace Canyon.Database.Entities
{
    [Table("cq_login_rcd")]
    public class DbGameLoginRecord
    {
        [Key][Column("id")] public virtual uint Identity { get; protected set; }
        [Column("account_id")] public virtual uint AccountIdentity { get; set; }
        [Column("user_id")] public virtual uint UserIdentity { get; set; }
        [Column("session_secs")] public virtual uint OnlineTime { get; set; }
        [Column("login_time")] public virtual DateTime LoginTime { get; set; }
        [Column("logout_time")] public virtual DateTime LogoutTime { get; set; }
        [Column("server_version")] public virtual string ServerVersion { get; set; }
        [Column("mac_addr")] public virtual string MacAddress { get; set; }
        [Column("ip_addr")] public virtual string IpAddress { get; set; }
    }
}