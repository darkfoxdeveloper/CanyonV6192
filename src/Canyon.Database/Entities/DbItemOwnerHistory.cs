namespace Canyon.Database.Entities
{
    [Table("cq_item_owner_history")]
    public class DbItemOwnerHistory
    {
        [Key][Column("id")] public virtual uint Identity { get; set; }
        [Column("item_id")] public virtual uint ItemIdentity { get; set; }
        [Column("old_owner_id")] public virtual uint OldOwnerIdentity { get; set; }
        [Column("new_owner_id")] public virtual uint NewOwnerIdentity { get; set; }
        [Column("change_time")] public virtual DateTime Time { get; set; }
        [Column("operation")] public virtual byte Operation { get; set; }
    }
}