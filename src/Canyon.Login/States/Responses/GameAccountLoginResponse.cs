namespace Canyon.Login.States.Responses
{
    public class GameAccountLoginResponse
    {
        public bool Success { get; set; }
        public int AccountId { get; set; }

        public int AccountAuthority { get; set; }
        public bool IsBanned { get; set; }
        public bool IsPermanentlyBanned { get; set; }
        public bool IsLocked { get; set; }

        public int VIPLevel { get; set; }
    }
}
