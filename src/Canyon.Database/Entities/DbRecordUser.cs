namespace Canyon.Database.Entities
{
    [Table("records_user")]
    public class DbRecordUser
    {
        [Key] public uint Id { get; set; }
        public uint ServerIdentity { get; set; }
        public uint UserIdentity { get; set; }
        public uint AccountIdentity { get; set; }
        public string Name { get; set; }
        public uint MateId { get; set; }
        public byte Level { get; set; }
        public ulong Experience { get; set; }
        public byte Profession { get; set; }
        public byte OldProfession { get; set; }
        public byte NewProfession { get; set; }
        public byte Metempsychosis { get; set; }
        public ushort Strength { get; set; }
        public ushort Agility { get; set; }
        public ushort Vitality { get; set; }
        public ushort Spirit { get; set; }
        public ushort AdditionalPoints { get; set; }
        public uint SyndicateIdentity { get; set; }
        public ushort SyndicatePosition { get; set; }
        public ulong NobilityDonation { get; set; }
        public byte NobilityRank { get; set; }
        public uint SupermanCount { get; set; }
        public DateTime? DeletedAt { get; set; }
        public ulong Money { get; set; }
        public uint WarehouseMoney { get; set; }
        public uint ConquerPoints { get; set; }
        public uint FamilyIdentity { get; set; }
        public ushort FamilyRank { get; set; }
    }
}