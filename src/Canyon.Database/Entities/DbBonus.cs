namespace Canyon.Database.Entities
{
    [Table("cq_bonus")]
    public class DbBonus
    {
        [Key][Column("id")] public virtual uint Identity { get; set; }
        [Column("action")] public virtual uint Action { get; set; }
        [Column("id_account")] public virtual uint AccountIdentity { get; set; }
        [Column("flag")] public virtual byte Flag { get; set; }
        [Column("ref_id")] public virtual ushort ReferenceCode { get; set; }
        [Column("time")] public virtual DateTime? Time { get; set; }
    }
}