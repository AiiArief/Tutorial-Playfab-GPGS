using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Assets.SimpleGoogleSignIn.Scripts
{
    public partial class GoogleAuth
    {
        /// <summary>
        /// Returns an access token async.
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            var completed = false;
            string accessToken = null, error = null;

            GetAccessToken((success, e, result) =>
            {
                if (success)
                {
                    accessToken = result?.AccessToken;
                }
                else
                {
                    error = e;
                }

                completed = true;
            });

            while (!completed)
            {
                await Task.Yield();
            }

            if (accessToken == null) throw new Exception(error);

            Log($"accessToken={accessToken}");

            return accessToken;
        }

        /// <summary>
        /// Returns an access token async.
        /// </summary>
        public async Task<UserInfo> SignInAsync()
        {
            var completed = false;
            string error = null;
            UserInfo userInfo = null;

            SignIn((success, e, result) =>
            {
                if (success)
                {
                    userInfo = result;
                }
                else
                {
                    error = e;
                }

                completed = true;
            }, caching: true);

            while (!completed)
            {
                await Task.Yield();
            }

            if (userInfo == null) throw new Exception(error);

            Log($"userInfo={JsonConvert.SerializeObject(userInfo)}");

            return userInfo;
        }
    }
}