namespace Canyon.Database.Entities
{
    [Table("cq_instancetype")]
    public class DbInstanceType
    {
        [Key]
        [Column("id")] public virtual uint Id { get; protected set; }
        [Column("name")] public virtual string Name { get; set; }
        [Column("mapid")] public virtual uint MapId { get; set; }
        [Column("type")] public virtual byte Type { get; set; }
        //[Column("base_id")] public virtual uint BaseId { get; set; }
        [Column("lev_min")] public virtual uint LevelMin { get; set; }
        [Column("lev_max")] public virtual uint LevelMax { get; set; }
        [Column("battle_min")] public virtual ushort BattleMin { get; set; }
        [Column("time_limit")] public virtual ushort TimeLimit { get; set; }
        [Column("action")] public virtual uint Action { get; set; }
        [Column("mapid_return")] public virtual uint ReturnMapId { get; set; }
        [Column("posx_return")] public virtual ushort ReturnMapX { get; set; }
        [Column("posy_return")] public virtual ushort ReturnMapY { get; set; }
        //[Column("difficulty")] public virtual byte Difficulty { get; set; }
        //[Column("normal_times")] public virtual ushort NormalTimes { get; set; }
        //[Column("extra_times")] public virtual ushort ExtraTimes { get; set; }
        //[Column("buy_condition_type")] public virtual byte BuyConditionType { get; set; }
        //[Column("buy_condition_data")] public virtual uint BuyConditionData { get; set; }
        //[Column("buy_emoney")] public virtual uint BuyEmoney { get; set; }
        //[Column("emoney_mono")] public virtual bool EmoneyMono { get; set; }
        //[Column("buy_itemtype")] public virtual uint BuyItemtype { get; set; }
        //[Column("sweep_star")] public virtual byte SweepStar { get; set; }
        //[Column("sweep_cost_type")] public virtual byte SweepCostType { get; set; }
        //[Column("sweep_cost_value")] public virtual uint SweepCostValue { get; set; }
        //[Column("attribute")] public virtual uint Attribute { get; set; }
        //[Column("complete_type")] public virtual uint CompleteType { get; set; }
        //[Column("complete_data1")] public virtual uint CompleteData1 { get; set; }
        //[Column("complete_data2")] public virtual uint CompleteData2 { get; set; }
        //[Column("fail_die_times")] public virtual uint FailDieTimes { get; set; }

        [ForeignKey("Id")]
        public virtual DbInstanceEnterCondition EnterCondition { get; set; }
    }
}
