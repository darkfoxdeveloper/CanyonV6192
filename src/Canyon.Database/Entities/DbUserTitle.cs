namespace Canyon.Database.Entities
{
    [Table("cq_user_title")]
    public class DbUserTitle
    {
        [Key][Column("id")] public uint Identity { get; set; }

        [Column("player_id")] public uint PlayerId { get; set; }
        [Column("type")] public uint Type { get; set; }
        [Column("title_id")] public uint TitleId { get; set; }
        [Column("status")] public uint Status { get; set; }
        [Column("del_time")] public DateTime DelTime { get; set; }
    }
}