namespace Canyon.Database.Entities
{
    [Table("realms_status")]
    public class DbRealmStatus
    {
        [Key][Column("id")] public virtual uint Identity { get; set; }
        [Column("realm_id")] public virtual uint RealmIdentity { get; set; }
        [Column("realm_name")] public virtual string RealmName { get; set; }
        [Column("old_status")] public virtual DbRealm.RealmStatus OldStatus { get; set; }
        [Column("new_status")] public virtual DbRealm.RealmStatus NewStatus { get; set; }
        [Column("time")] public virtual int Time { get; set; }
        [Column("players_online")] public virtual uint PlayersOnline { get; set; }
        [Column("max_players_online")] public virtual uint MaxPlayersOnline { get; set; }

        [ForeignKey("RealmIdentity")] public virtual DbRealm Realm { get; set; }
    }
}