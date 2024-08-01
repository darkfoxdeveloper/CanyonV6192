namespace Canyon.Database.Entities
{
    [Table("cq_instance_enter_condition")]
    public class DbInstanceEnterCondition
    {
        [Key]
        [Column("instance_id")] public virtual uint InstanceId { get; set; }
        [Column("lev_min")] public virtual uint LevelMin { get; set; }
        [Column("lev_max")] public virtual uint LevelMax { get; set; }
        [Column("player_num_min")] public virtual uint PlayerNumMin { get; set; }
        [Column("player_num_max")] public virtual uint PlayerNumMax { get; set; }
        [Column("battle_effect")] public virtual uint BattleEffect { get; set; }
        [Column("cost_item")] public virtual uint CostItem { get; set; }
        [Column("item_num")] public virtual uint ItemNum { get; set; }
        [Column("req_task")] public virtual uint ReqTask { get; set; }
        [Column("pre_instance")] public virtual uint PreInstance { get; set; }
        [Column("function")] public virtual uint Function { get; set; }
        [Column("start_date")] public virtual uint StartDate { get; set; }
        [Column("end_date")] public virtual uint EndDate { get; set; }
        [Column("open_days")] public virtual uint OpenDays { get; set; }
        [Column("start_time")] public virtual uint StartTime { get;set; }
        [Column("end_time")] public virtual uint EndTime { get; set; }
        [Column("forbidden_map")] public virtual ulong ForbiddenMap { get; set; }
    }
}
