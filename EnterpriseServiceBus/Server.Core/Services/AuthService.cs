using System;
using System.Security.Cryptography;
using GeoDecisions.Esb.Common.Services;

namespace GeoDecisions.Esb.Server.Core.Services
{
    internal class AuthService : IAuthService
    {
        public string Authenticate(string username, string password)
        {
            //todo: authenticate user/caller

            string token = GenerateToken();

            // add to headers
            BusContext.Current.AuthToken = token;

            return token;
        }

        public string Authenticate(string infoHash)
        {
            //todo: authenticate user/caller

            string token = GenerateToken();

            // add to headers
            BusContext.Current.AuthToken = token;

            throw new NotImplementedException();
        }


        private string GenerateToken()
        {
            RandomNumberGenerator rng = new RNGCryptoServiceProvider();
            var tokenData = new byte[32];
            rng.GetBytes(tokenData);
            string token = Convert.ToBase64String(tokenData);
            return token;
        }
    }
}