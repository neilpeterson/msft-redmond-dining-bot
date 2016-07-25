using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using msftbot.Support;
using Newtonsoft.Json;

namespace msftbot
{
    internal class ShuttleActions
    {
        internal ShuttleActions ()
        { }

        internal async Task<bool> SetShuttleRequest(string destination, string origin)
        {
            //Do the request for a shuttle here. Return true if shuttle booked.
            try
            {
                //Filler Code, Does nothing.
                // Get authentication token from authentication.cs
                Authentication auth = new Authentication();
                string authtoken = await auth.GetAuthHeader();

                // Get JSON – List of all Cafes
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Constants.AuthHeaderValueScheme, authtoken);
                
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}