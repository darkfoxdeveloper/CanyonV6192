namespace Canyon.Database
{
    /// <summary>
    ///     Encapsulates database configuration for Entity Framework.
    /// </summary>
    public class DatabaseConfiguration
    {
        public string Hostname { get; set; }
        public string Schema { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
    }
}