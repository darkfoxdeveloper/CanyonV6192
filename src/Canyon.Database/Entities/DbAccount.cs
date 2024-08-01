namespace Canyon.Database.Entities
{
    /// <summary>
    ///     Account information for a registered player. The account server uses this information
    ///     to authenticate the player on login, and track permissions and player access to the
    ///     server. Passwords are hashed using a salted SHA-1 for user protection.
    /// </summary>
    [Table("account")]
    public class DbAccount
    {
        // Column Properties
        [Key] public uint AccountID { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public ushort AuthorityID { get; set; }
        public AccountStatus StatusID { get; set; }
        public string IPAddress { get; set; }
        public string MacAddress { get; set; }
        public DateTime Registered { get; set; }
        public int ParentId { get; set; }

        [Flags]
        public enum AccountStatus
        {
            None,
            Banned = 0x1,
            Locked = 0x2,
            NotActivated = 0x4
        }
    }
}