using GooglePlayGames;
using GooglePlayGames.BasicApi;
using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ProcessDeepLinkMngr : MonoBehaviour
{
    public static ProcessDeepLinkMngr Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Application.deepLinkActivated += onDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                // Cold start and Application.absoluteURL not null so process Deep Link.
                onDeepLinkActivated(Application.absoluteURL);
            }

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void onDeepLinkActivated(string url)
    {
        Debug.Log("Got Deeplink Activated: " + url);

        // Decode the URL to extract auth code. 
        string queryParams = url.Split("?"[0])[1];
        string[] keyValuePairs = queryParams.Split("&");
        string authCode = string.Empty;

        foreach (string s in keyValuePairs)
        {
            if (s.StartsWith("code"))
            {
                authCode = s.Split("=")[1];
                break;
            }
        }

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
                TitleId = PlayFabSettings.TitleId
            };

            PlayFabClientAPI.LoginWithGoogleAccount(request,
                (LoginResult result) => {
                    Debug.Log("PlayFab LoginWithGoogleAccount Success.");
                },
                (PlayFabError error) => {
                    Debug.Log("PlayFab LoginWithGoogleAccount Failure: " + error.GenerateErrorReport());
                }
            );
        }
        else
        {
            Debug.Log("Error when getting Auth Code.");
        }
    }
}