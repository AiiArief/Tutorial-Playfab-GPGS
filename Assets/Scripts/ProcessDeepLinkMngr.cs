//using ImaginationOverflow.UniversalDeepLinking;
using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine;

public class ProcessDeepLinkMngr : MonoBehaviour
{
    public static ProcessDeepLinkMngr Instance { get; private set; }

    public Action<LoginResult> OnDeeplinkLoginSuccess;
    public Action<PlayFabError> OnDeeplinkLoginFailure;

    public void ManualInputAuth(UnityEngine.UI.InputField inputField)
    {
        var authCode = inputField.text;
        _LoginWithGoogleAccount(authCode);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DeepLinkManager.Instance.LinkActivated += _OnDeepLinkActivated;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        //DeepLinkManager.Instance.LinkActivated -= _OnDeepLinkActivated;
    }

    //private void _OnDeepLinkActivated(LinkActivation la)
    //{
    //    var url = la.Uri;
    //    Debug.Log("Got Deeplink Activated: " + url);

    //    // Decode the URL to extract auth code. 
    //    string queryParams = url.Split("?"[0])[1];
    //    string[] keyValuePairs = queryParams.Split("&");
    //    string authCode = string.Empty;

    //    foreach (string s in keyValuePairs)
    //    {
    //        if (s.StartsWith("code"))
    //        {
    //            authCode = s.Split("=")[1];
    //            break;
    //        }
    //    }

    //    _LoginWithGoogleAccount(authCode);
    //}

    private void _LoginWithGoogleAccount(string authCode)
    {
        if (!string.IsNullOrEmpty(authCode))
        {
            // Call the LoginWithGoogleAccount using the auth code
            Debug.Log("Authenticating to PlayFab using LoginWithGoogleAccount...");

            // Make sure to unescape string
            authCode = Uri.UnescapeDataString(authCode);

            var request = new LoginWithGoogleAccountRequest
            {
                ServerAuthCode = authCode,
                CreateAccount = true,
                TitleId = PlayFabSettings.TitleId,
                SetEmail = true,
            };

            PlayFabClientAPI.LoginWithGoogleAccount(request,
                (LoginResult result) => {
                    Debug.Log("PlayFab LoginWithGoogleAccount Success.");
                    OnDeeplinkLoginSuccess(result);
                },
                (PlayFabError error) => {
                    Debug.Log("PlayFab LoginWithGoogleAccount Failure: " + error.GenerateErrorReport());
                    OnDeeplinkLoginFailure(error);
                }
            );
        }
        else
        {
            Debug.Log("Error when getting Auth Code.");
            PlayFabError error = new PlayFabError();
            OnDeeplinkLoginFailure(error);
        }
    }
}