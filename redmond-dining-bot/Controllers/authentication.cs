using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;

namespace msftbot
{
    public class Authentication
    {
        const string clientId = "";
        const string key = "";
        const string authorityUri = "";

        public async Task<string> GetAuthHeader()
        {
            AuthenticationContext authContext = new AuthenticationContext(authorityUri);
            var credential = new ClientCredential(clientId, key);
            var token = await authContext.AcquireTokenAsync("https://microsoft.onmicrosoft.com/Dining", credential);
            return token.AccessToken;
        }
    }
}
