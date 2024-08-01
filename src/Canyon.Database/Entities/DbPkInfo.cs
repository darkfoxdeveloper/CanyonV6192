namespace Canyon.Database.Entities
{
    [Table("cq_pk_info")]
    public class DbPkInfo
    {
        [Key]
        [Column("id")]
        public virtual uint Id { get; set; }
        [Column("type")]
        public virtual ushort Type { get; set; }
        [Column("subtype")]
        public virtual ushort Subtype { get; set; }
        [Column("time")]
        public virtual uint Time { get; set; }
        [Column("pk1")]
        public virtual uint Pk1 { get; set; }
        [Column("pk1_name")]
        public virtual string Pk1Name { get; set; }
        [Column("pk2")]
        public virtual uint Pk2 { get; set; }
        [Column("pk2_name")]
        public virtual string Pk2Name { get; set; }
        [Column("pk3")]
        public virtual uint Pk3 { get; set; }
        [Column("pk3_name")]
        public virtual string Pk3Name { get; set; }
        [Column("pk4")]
        public virtual uint Pk4 { get; set; }
        [Column("pk4_name")]
        public virtual string Pk4Name { get; set; }
        [Column("pk5")]
        public virtual uint Pk5 { get; set; }
        [Column("pk5_name")]
        public virtual string Pk5Name { get; set; }
        [Column("pk6")]
        public virtual uint Pk6 { get; set; }
        [Column("pk6_name")]
        public virtual string Pk6Name { get; set; }
        [Column("pk7")]
        public virtual uint Pk7 { get; set; }
        [Column("pk7_name")]
        public virtual string Pk7Name { get; set; }
        [Column("pk8")]
        public virtual uint Pk8 { get; set; }
        [Column("pk8_name")]
        public virtual string Pk8Name { get; set; }
    }
}
