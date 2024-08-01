namespace Canyon.Database.Entities
{
    [Table("cq_peerage")]
    public class DbPeerage
    {
        [Key][Column("id")] public virtual uint Identity { get; set; }

        [Column("user_id")] public virtual uint UserIdentity { get; set; }

        [Column("user_name")] public virtual string Name { get; set; }

        [Column("donation")] public virtual ulong Donation { get; set; }

        [Column("first_donation")] public virtual DateTime FirstDonation { get; set; }
    }
}