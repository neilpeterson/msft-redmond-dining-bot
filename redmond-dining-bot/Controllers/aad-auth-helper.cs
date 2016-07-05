using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;

namespace aadauthhelper
{
    public class authtoken
    {
        public async Task<AuthenticationHeaderValue> GetAuthHeader()
        {
            string clientId = "7c2daad8-1ced-485e-bfdb-eb04627160bd";
            string key = "fQkYK02KyeePuCozpj7hmiB7udHS7tJmFR5x309BdT8=";
            string authorityUri = "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/oauth2/token";
            AuthenticationContext authContext = new AuthenticationContext(authorityUri);
            var credential = new ClientCredential(clientId, key);
            var token = await authContext.AcquireTokenAsync("https://microsoft.onmicrosoft.com/Dining", credential);
            return new AuthenticationHeaderValue(token.AccessToken);
        }
    }    
}