namespace Canyon.Database.Entities
{
    [Table("cq_friend")]
    public class DbFriend
    {
        [Key][Column("id")] public virtual uint Identity { get; set; }

        [Column("userid")] public virtual uint UserIdentity { get; set; }

        [Column("friend")] public virtual uint TargetIdentity { get; set; }
    }
}