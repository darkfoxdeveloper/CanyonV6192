namespace Canyon.Database.Entities
{
    [Table("cq_card")]
    public class DbCard
    {
        [Key][Column("id")] public virtual uint Identity { get; set; }
        [Column("ref_id")] public virtual uint ReferenceId { get; set; }
        [Column("account_id")] public virtual uint AccountId { get; set; }
        [Column("itemtype")] public virtual uint ItemType { get; set; }
        [Column("money")] public virtual uint Money { get; set; }
        [Column("emoney")] public virtual uint ConquerPoints { get; set; }
        [Column("emoney_mono")] public virtual uint ConquerPointsMono { get; set; }
        [Column("flag")] public virtual uint Flag { get; set; }
        [Column("timestamp")] public virtual DateTime? Timestamp { get; set; }
    }
}