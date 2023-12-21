using System;
using UnityEngine;
using UnityEngine.UI;
using Assets.SimpleGoogleSignIn.Scripts;

namespace Assets.SimpleGoogleSignIn
{
    public class Example : MonoBehaviour
    {
        public GoogleAuth GoogleAuth;
        public Text Log;
        public Text Output;
        
        public void Start()
        {
            Application.logMessageReceived += (condition, _, _) => Log.text += condition + '\n';
            GoogleAuth = new GoogleAuth();
        }

        public void SignIn()
        {
            GoogleAuth.SignIn(OnSignIn, caching: true);
        }

        public void SignOut()
        {
            GoogleAuth.SignOut(revokeAccessToken: true);
            Output.text = "Not signed in";
        }

        public void GetAccessToken()
        {
            GoogleAuth.GetAccessToken(OnGetAccessToken);
        }

        private void OnSignIn(bool success, string error, UserInfo userInfo)
        {
            Output.text = success ? $"Hello, {userInfo.name}!" : error;
        }

        private void OnGetAccessToken(bool success, string error, TokenResponse tokenResponse)
        {
            Output.text = success ? $"Access token: {tokenResponse.AccessToken}" : error;

            if (!success) return;

            var jwt = new JWT(tokenResponse.IdToken);

            Debug.Log($"JSON Web Token (JWT) Payload: {jwt.Payload}");
            
            jwt.ValidateSignature(GoogleAuth.ClientId, OnValidateSignature);
        }

        private void OnValidateSignature(bool success, string error)
        {
            Output.text += Environment.NewLine;
            Output.text += success ? "JWT signature validated" : error;
        }

        public void Navigate(string url)
        {
            Application.OpenURL(url);
        }
    }
}