using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.SimpleGoogleSignIn.Scripts
{
    /// <summary>
    /// API specification: https://developers.google.com/identity/protocols/oauth2/native-app
    /// </summary>
    public partial class GoogleAuth
    {
        public SavedAuth SavedAuth { get; private set; }
        public TokenResponse TokenResponse { get; private set; }
        public string ClientId => _settings.ClientId;
        public bool DebugLog = true;

        /// <summary>
        /// OpenID configuration: https://accounts.google.com/.well-known/openid-configuration
        /// </summary>
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
        private const string RevocationEndpoint = "https://oauth2.googleapis.com/revoke";

        private readonly GoogleAuthSettings _settings;
        private Implementation _implementation;
        private string _redirectUri, _state, _codeVerifier;
        private Action<bool, string, UserInfo> _callbackU;
        private Action<bool, string, TokenResponse> _callbackT;

        public GoogleAuth(GoogleAuthSettings settings = null)
        {
            _settings = settings == null ? Resources.Load<GoogleAuthSettings>("GoogleAuthSettings") : settings;

            if (_settings == null) throw new NullReferenceException(nameof(_settings));

            SavedAuth = SavedAuth.GetInstance(_settings.ClientId);
            Application.deepLinkActivated += OnDeepLinkActivated;

            #if UNITY_IOS && !UNITY_EDITOR

            SafariViewController.DidCompleteInitialLoad += DidCompleteInitialLoad;
            SafariViewController.DidFinish += UserCancelledHook;

            #endif

            #if UNITY_STANDALONE_WIN && !UNITY_EDITOR

            WindowsDeepLinking.Initialize(_settings.CustomUriScheme, OnDeepLinkActivated);

            #endif
        }

        ~GoogleAuth()
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;

            #if UNITY_IOS && !UNITY_EDITOR

            SafariViewController.DidCompleteInitialLoad -= DidCompleteInitialLoad;
            SafariViewController.DidFinish -= UserCancelledHook;

            #endif
        }

        public void SignIn(Action<bool, string, UserInfo> callback, bool caching = true)
        {
            _callbackU = callback;
            _callbackT = null;

            Initialize();

            if (SavedAuth == null)
            {
                Auth();
            }
            else if (caching && SavedAuth.UserInfo != null)
            {
                callback(true, null, SavedAuth.UserInfo);
            }
            else
            {
                UseSavedToken();
            }
        }

        public void GetAccessToken(Action<bool, string, TokenResponse> callback)
        {
            _callbackU = null;
            _callbackT = callback;

            Initialize();

            if (SavedAuth == null)
            {
                Auth();
            }
            else
            {
                if (SavedAuth.TokenResponse.Expired)
                {
                    Log("Refreshing expired access token...");
                    RefreshAccessToken(callback);
                }
                else
                {
                    callback(true, null, SavedAuth.TokenResponse);
                }
            }
        }

        public void SignOut(bool revokeAccessToken = false)
        {
            TokenResponse = null;

            if (SavedAuth != null)
            {
                if (revokeAccessToken && SavedAuth.TokenResponse != null)
                {
                    RevokeAccessToken(SavedAuth.TokenResponse.AccessToken);
                }

                SavedAuth.Delete();
                SavedAuth = null;
            }
        }

        public void Cancel()
        {
            _redirectUri = _state = _codeVerifier = null;
            _callbackU = null;
            _callbackT = null;
            ApplicationFocusHook.Cancel();
        }

        private void Initialize()
        {
            #if UNITY_EDITOR

            _implementation = Implementation.LoopbackFlow;
            _redirectUri = $"http://localhost:{Helpers.GetRandomUnusedPort()}/";
            
            #elif UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_WSA

            _implementation = Implementation.DeepLinking;
            _redirectUri = $"{_settings.CustomUriScheme}:/oauth2/google";

            #elif UNITY_STANDALONE_WIN

            _implementation = _settings.CustomUriScheme == "" ? Implementation.LoopbackFlow : Implementation.DeepLinking;
            _redirectUri = _settings.CustomUriScheme == "" ? $"http://localhost:{Helpers.GetRandomUnusedPort()}/" : $"{_settings.CustomUriScheme}:/oauth2/google";

            #elif UNITY_WEBGL

            _implementation = Implementation.AuthorizationMiddleware;
            _redirectUri = "";

            #endif

            if (SavedAuth != null && SavedAuth.ClientId != _settings.ClientId)
            {
                SavedAuth.Delete();
                SavedAuth = null;
            }
        }

        private void Auth()
        {
            _state = Guid.NewGuid().ToString();
            _codeVerifier = Guid.NewGuid().ToString();

            if (!_settings.ManualCancellation)
            {
                #if UNITY_IOS && !UNITY_EDITOR

                if (!_settings.UseSafariViewController) ApplicationFocusHook.Create(UserCancelledHook);

                #else

                ApplicationFocusHook.Create(UserCancelledHook);

                #endif
            }

            var codeChallenge = Helpers.CreateCodeChallenge(_codeVerifier);
            var redirectUri = _implementation == Implementation.AuthorizationMiddleware ? AuthorizationMiddleware.Endpoint + "/redirect" : _redirectUri;
            var authorizationRequest = $"{AuthorizationEndpoint}?response_type=code&scope={Uri.EscapeDataString(string.Join(" ", _settings.AccessScopes))}&redirect_uri={Uri.EscapeDataString(redirectUri)}&client_id={_settings.ClientId}&state={_state}&code_challenge={codeChallenge}&code_challenge_method=S256";

            if (_implementation == Implementation.AuthorizationMiddleware)
            {
                AuthorizationMiddleware.Auth(_redirectUri, _state, authorizationRequest, (success, error, code) =>
                {
                    if (success)
                    {
                        PerformCodeExchange(code);
                    }
                    else
                    {
                        _callbackU?.Invoke(false, error, null);
                        _callbackT?.Invoke(false, error, null);
                    }
                });
            }
            else
            {
                AuthorizationRequest(authorizationRequest);

                switch (_implementation)
                {
                    case Implementation.LoopbackFlow:
                        LoopbackFlow.Initialize(_redirectUri, OnDeepLinkActivated);
                        break;
                }
            }
        }

        private void AuthorizationRequest(string url)
        {
            Log($"Authorization: {url}");

            #if UNITY_IOS && !UNITY_EDITOR

            if (_settings.UseSafariViewController)
            {
                SafariViewController.OpenURL(url);
            }
            else
            {
                Application.OpenURL(url);
            }

            #else

            Application.OpenURL(url);

            #endif
        }

        private void DidCompleteInitialLoad(bool loaded)
        {
            if (loaded) return;

            const string error = "Failed to load auth screen.";

            _callbackT?.Invoke(false, error, null);
            _callbackU?.Invoke(false, error, null);
        }

        private async void UserCancelledHook()
        {
            if (_settings.ManualCancellation) return;

            var time = Time.time;

            while (Time.time < time + 1)
            {
                await Task.Yield();
            }

            if (_codeVerifier == null) return;

            _codeVerifier = null;

            const string error = "User cancelled.";

            _callbackT?.Invoke(false, error, null);
            _callbackU?.Invoke(false, error, null);
        }

        private void UseSavedToken()
        {
            if (SavedAuth == null || SavedAuth.ClientId != _settings.ClientId)
            {
                SignOut();
                SignIn(_callbackU);
            }
            else if (!SavedAuth.TokenResponse.Expired)
            {
                Log("Using saved access token...");
                RequestUserInfo(SavedAuth.TokenResponse.AccessToken, (success, _, userInfo) =>
                {
                    if (success)
                    {
                        _callbackU(true, null, userInfo);
                    }
                    else
                    {
                        SignOut();
                        SignIn(_callbackU);
                    }
                });
            }
            else
            {
                Log("Refreshing expired access token...");
                RefreshAccessToken((success, _, _) =>
                {
                    if (success)
                    {
                        RequestUserInfo(SavedAuth.TokenResponse.AccessToken, _callbackU);
                    }
                    else
                    {
                        SignOut();
                        SignIn(_callbackU);
                    }
                });
            }
        }

        private void OnDeepLinkActivated(string deepLink)
        {
            Log($"Deep link activated: {deepLink}");

            deepLink = deepLink.Replace(":///", ":/"); // Some browsers may add extra slashes.

            if (_redirectUri == null || !deepLink.StartsWith(_redirectUri) || _codeVerifier == null)
            {
                Log("Unexpected deep link.");
                return;
            }

            #if UNITY_IOS && !UNITY_EDITOR

            if (_settings.UseSafariViewController)
            {
                Log($"Closing SafariViewController");
                SafariViewController.Close();
            }
            
            #endif

            var parameters = Helpers.ParseQueryString(deepLink);
            var error = parameters.Get("error");

            if (error != null)
            {
                _callbackU?.Invoke(false, error, null);
                _callbackT?.Invoke(false, error, null);
                return;
            }

            var state = parameters.Get("state");
            var code = parameters.Get("code");

            if (state == null || code == null) return;

            if (state == _state)
            {
                PerformCodeExchange(code);
            }
            else
            {
                Log("Unexpected response.");
            }
        }

        private void PerformCodeExchange(string code)
        {
            var redirectUri = _implementation == Implementation.AuthorizationMiddleware ? AuthorizationMiddleware.Endpoint + "/redirect" : _redirectUri;
            var formFields = new Dictionary<string, string>
            {
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", _settings.ClientId },
                { "code_verifier", _codeVerifier },
                { "scope", string.Join(" ", _settings.AccessScopes) },
                { "grant_type", "authorization_code" }
            };

            if (_implementation == Implementation.LoopbackFlow || _implementation == Implementation.AuthorizationMiddleware)
            {
                formFields.Add("client_secret", _settings.ClientSecret);
            }

            _codeVerifier = null;

            var request = UnityWebRequest.Post(TokenEndpoint, formFields);

            Log($"Exchanging code for access token: {request.url}");

            request.SendWebRequest().completed += _ =>
            {
                if (request.error == null)
                {
                    Log($"TokenExchangeResponse={request.downloadHandler.text}");

                    TokenResponse = TokenResponse.Parse(request.downloadHandler.text);
                    SavedAuth = new SavedAuth(_settings.ClientId, TokenResponse);
                    SavedAuth.Save();

                    if (_callbackT != null)
                    {
                        _callbackT(true, null, TokenResponse);
                    }

                    if (_callbackU != null)
                    {
                        RequestUserInfo(TokenResponse.AccessToken, _callbackU);
                    }
                }
                else
                {
                    _callbackU?.Invoke(false, request.GetError(), null);
                    _callbackT?.Invoke(false, request.GetError(), null);
                }
            };
        }

        /// <summary>
        /// You can move this function to your backend for more security.
        /// </summary>
        public void RequestUserInfo(string accessToken, Action<bool, string, UserInfo> callback)
        {
            var request = UnityWebRequest.Get(UserInfoEndpoint);

            Log($"Requesting user info: {request.url}");

            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SendWebRequest().completed += _ =>
            {
                if (request.error == null)
                {
                    Log($"UserInfo={request.downloadHandler.text}");
                    SavedAuth.UserInfo = JsonUtility.FromJson<UserInfo>(request.downloadHandler.text);
                    SavedAuth.Save();
                    callback(true, null, SavedAuth.UserInfo);
                }
                else
                {
                    callback(false, request.GetError(), null);
                }
            };
        }

        /// <summary>
        /// https://developers.google.com/identity/protocols/oauth2/native-app#offline
        /// </summary>
        public void RefreshAccessToken(Action<bool, string, TokenResponse> callback)
        {
            if (SavedAuth == null) throw new Exception("Initial authorization is required.");

            var refreshToken = SavedAuth.TokenResponse.RefreshToken;
            var formFields = new Dictionary<string, string>
            {
                { "client_id", _settings.ClientId },
                { "refresh_token", refreshToken },
                { "grant_type", "refresh_token" }
            };

            if (_implementation == Implementation.LoopbackFlow || _implementation == Implementation.AuthorizationMiddleware)
            {
                formFields.Add("client_secret", _settings.ClientSecret);
            }

            var request = UnityWebRequest.Post(TokenEndpoint, formFields);

            Log($"Access token refresh: {request.url}");

            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Log($"TokenExchangeResponse={request.downloadHandler.text}");

                    TokenResponse = TokenResponse.Parse(request.downloadHandler.text);
                    TokenResponse.RefreshToken = refreshToken;
                    SavedAuth.TokenResponse = TokenResponse;
                    SavedAuth.Save();
                    callback(true, null, TokenResponse);
                }
                else
                {
                    Debug.LogError(request.GetError());
                    callback(false, request.GetError(), null);
                }

                request.Dispose();
            };
        }

        private void RevokeAccessToken(string accessToken)
        {
            var request = UnityWebRequest.Post($"{RevocationEndpoint}?token={accessToken}", "");

            Log($"Revoking access token: {request.url}");

            request.SendWebRequest().completed += _ => Log(request.error ?? "Access token revoked!");
        }

        private void Log(string message)
        {
            if (DebugLog)
            {
                Debug.Log(message); // TODO: Remove in Release.
            }
        }
    }
}