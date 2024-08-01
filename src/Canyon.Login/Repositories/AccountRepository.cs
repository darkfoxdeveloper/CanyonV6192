using Canyon.Login.Database;
using Canyon.Login.Database.Entities;
using Canyon.Login.States.Responses;

namespace Canyon.Login.Repositories
{
    public class AccountRepository
    {
        private AccountRepository() { }

        public static Task<GameAccountResponse> FindAsync(uint accountId)
        {
            // Load realm connection information
            return Kernel.RestClient.GetAsync<GameAccountResponse>($"{Kernel.ServerConfiguration.Account.Url}/api/conquer/account/{accountId}");
        }

        public static GameAccount GetByUsername(string username)
        {
            using var context = new ServerDbContext();
            return context.GameAccounts.FirstOrDefault(x => x.UserName == username);
        }
    }
}
