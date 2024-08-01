namespace Canyon.Login.States.Responses
{
    public class GameAccountResponse
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int Authority { get; set; }
        public int Flag { get; set; }
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
    }
}
