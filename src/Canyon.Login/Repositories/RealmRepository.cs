using Canyon.Login.Database;
using Canyon.Login.Database.Entities;
using Canyon.Login.States;
using Canyon.Login.States.Requests;
using Canyon.Login.States.Responses;
using Canyon.Network.Security;
using Canyon.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Canyon.Login.Repositories
{
    /// <summary>
    ///     Repository for defining data access layer (DAL) logic for the realm table. Realm
    ///     connection details are loaded into server memory at server startup, and may be
    ///     modified once loaded.
    /// </summary>
    public class RealmsRepository
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<RealmsRepository>();

        private static readonly byte[] AES_KEY;

        static RealmsRepository()
        {
            string strKey = Encoding.UTF8.GetString(Convert.FromBase64String("OTEzMTFhMjFlMDI4NWQ0NjY3N2FhNzVkNDRjNzI3YWM="));
            AES_KEY = new byte[strKey.Length / 2];
            for (var index = 0; index < AES_KEY.Length; index++)
            {
                string byteValue = strKey.Substring(index * 2, 2);
                AES_KEY[index] = Convert.ToByte(byteValue, 16);
            }
        }

        /// <summary>
        ///     Loads realm connection details and security information to the server's pool
        ///     of known realm routes. Should be invoked at server startup before the server
        ///     listener has been started.
        /// </summary>
        public static Task<RealmDataResponse> FindAsync(string realmName)
        {
            // Load realm connection information
            return Kernel.RestClient.GetAsync<RealmDataResponse>($"{Kernel.ServerConfiguration.Realm.Url}/api/realms/find-by-name/{realmName}");
        }

        public static async Task<RealmDataResponse> FindByIdAsync(string uuid)
        {
#if !USE_MYSQL_DB
            try
            {
                // Load realm connection information
                return await Kernel.RestClient.GetAsync<RealmDataResponse>($"{Kernel.ServerConfiguration.Realm.Url}/api/realms/find-by-id/{uuid}");
            }
            catch
            {
                return null;
            }
#else
            await using var ctx = new ServerDbContext();
            var realm = await ctx.RealmDatas.FirstOrDefaultAsync(x => x.RealmID == Guid.Parse(uuid));
            if (realm == null)
            {
                return null;
            }
            return new RealmDataResponse
            {
                Active = realm.Active,
                GameIPAddress = realm.GameIPAddress,
                GamePort = realm.GamePort,
                RpcIPAddress = realm.RpcIPAddress,
                RpcPort = realm.RpcPort,
                RealmID = realm.RealmID,
                RealmName = realm.Name
            };
#endif
        }

        public static Task<List<RealmDataResponse>> QueryRealmsAsync()
        {
            return Kernel.RestClient.GetAsync<List<RealmDataResponse>>($"{Kernel.ServerConfiguration.Realm.Url}/api/realms");
        }

        public static Task SyncRealmAsync(RealmSyncRequest body)
        {
            return Kernel.RestClient.PutAsync<object>($"{Kernel.ServerConfiguration.Realm.Url}/api/realms/status/{body.RealmId}", body);
        }

        public static async Task<bool> ValidateRealmAsync(Guid realmId, string userName, string password)
        {
#if !USE_MYSQL_DB
            try
            {
                logger.LogInformation("Validating realm [{}]", realmId);
                await Kernel.RestClient.PostAsync($"{Kernel.ServerConfiguration.Realm.Url}/api/realms/validate", new
                {
                    RealmId = realmId,
                    UserName = userName,
                    Password = password
                });
                return true;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Could not validate realm [{}] information. {}", realmId, ex.Message);
                return false;
            }
#else
            using var ctx = new ServerDbContext();
            var realm = GetById(realmId);
            if (realm == null)
            { 
                return false;
            }
            string encUsername = AesCipherHelper.Encrypt(AES_KEY, userName);
            logger.LogDebug("Encrypted username: {}", encUsername);
            string encPassword = AesCipherHelper.Encrypt(AES_KEY, password);
            logger.LogDebug("Encrypted password: {}", encPassword);
            return realm.Username.Equals(encUsername) && realm.Password.Equals(encPassword);
#endif
        }

        public static RealmData GetById(Guid realmId)
        {
            using var context = new ServerDbContext();
            return context.RealmDatas.FirstOrDefault(x => x.RealmID == realmId);
        }
    }
}
