namespace Canyon.Database.Entities
{
    [Table("cq_petpoint")]
    public class DbPetPoint
    {
        [Key]
        [Column("id")] public virtual uint Id { get; set; }
        [Column("mapid")] public virtual uint MapId { get; set; }
        [Column("rank")] public virtual ushort Rank { get; set; }
        [Column("ridepet_point")] public virtual ushort Points { get; set; }
    }
}
