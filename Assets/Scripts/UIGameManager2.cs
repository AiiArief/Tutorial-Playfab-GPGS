using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using Assets.SimpleGoogleSignIn.Scripts;
#if UNITY_ANDROID
//using GooglePlayGames;
//using GooglePlayGames.BasicApi;
#endif

public class LoginRequest : LoginWithGoogleAccountRequest {
    public string AccessToken;
}

public class UIGameManager2 : MonoBehaviour
{
    [SerializeField] GameObject m_loginPanel;
    [SerializeField] Button m_loginButton_mobile;
    [SerializeField] Button m_loginButton_pc;

    [SerializeField] GameObject m_contentPanel;
    [SerializeField] Text m_contentText;

    [SerializeField] GameObject m_connectionLoading;
    [SerializeField] Text m_versionText;

    public GoogleAuth GoogleAuth;

    public void PCSignInButton()
    {
        m_connectionLoading.SetActive(true);

        GoogleAuth.GetAccessToken(OnSignIn);
    }

    public void AndroidSignInButton()
    {
#if UNITY_ANDROID
        m_connectionLoading.SetActive(true);

        //PlayGamesPlatform.Instance.Authenticate(_ProcessAuthentication);
#endif
    }

    public void LogoutButton()
    {
        GoogleAuth.SignOut();
    }

    private void Awake()
    {
        GoogleAuth = new GoogleAuth();
        m_versionText.text = "v." + Application.version;

        m_connectionLoading.SetActive(false);
        m_loginPanel.SetActive(true);
        //m_loginButton_mobile.interactable = SystemInfo.deviceType == DeviceType.Handheld;
        //m_loginButton_pc.interactable = SystemInfo.deviceType == DeviceType.Desktop;

        m_contentPanel.SetActive(false);

#if UNITY_ANDROID
        //PlayGamesPlatform.Activate();
#endif
    }

    private void OnEnable()
    {
        //GoogleAuth.OnObtainedDeepLinkURL += ProcessServerAuthCode;
    }

    private void OnDisable()
    {
        //GoogleAuth.OnObtainedDeepLinkURL -= ProcessServerAuthCode;
    }

    //private string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    //private string redirectURI = "https://oauth.playfab.com/oauth2/google";
    //private object clientID = "357333282570-ijm1oov722u4pb2rr577dvom1ldijfs6.apps.googleusercontent.com";
    //private string scopes = "profile";

    //private void LaunchBrowserAuth()
    //{
    //    string authorizationRequest = string.Format("{0}?response_type=code&scope={1}&redirect_uri={2}&client_id={3}",
    //            authorizationEndpoint,
    //            scopes,
    //            Uri.EscapeDataString(redirectURI),
    //            clientID);

    //    Application.OpenURL(authorizationRequest);
    //}

    private void OnSignIn(bool success, string error, TokenResponse tokenResponse)
    {
        if(success)
        {
            Debug.Log("Access token : " + tokenResponse.AccessToken);
            ProcessServerAuthCode(tokenResponse.AccessToken);
        } else
        {
            Debug.Log("Error when getting Auth Code : " + error);
            PlayFabError errorPF = new PlayFabError();
            OnLoginWithGooglePlayGamesServicesFailure(errorPF);
        }
    }

    private void ProcessServerAuthCode(string serverAuthCode)
    {
        Debug.Log("Server Auth Code: " + serverAuthCode);

        var request = new LoginRequest
        {
            AccessToken = serverAuthCode,
            //ServerAuthCode = serverAuthCode,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams()
            {
                GetPlayerProfile = true
            }
        };

        PlayFabClientAPI.LoginWithGoogleAccount(request, OnLoginWithGooglePlayGamesServicesSuccess, OnLoginWithGooglePlayGamesServicesFailure);
    }

#if UNITY_ANDROID
    //private void _ProcessAuthentication(SignInStatus status)
    //{
    //    if (status == SignInStatus.Success)
    //    {
    //        PlayGamesPlatform.Instance.RequestServerSideAccess(false, ProcessServerAuthCode);
    //    }
    //    else
    //    {
    //        m_connectionLoading.SetActive(false);
    //        m_loginPanel.SetActive(true);
    //        m_contentPanel.SetActive(false);
    //        Debug.LogError("// ======================================== Failed google play auth : " + status.ToString());
    //    }
    //}
#endif

    private void OnLoginWithGooglePlayGamesServicesSuccess(LoginResult result)
    {
        m_connectionLoading.SetActive(false);
        m_loginPanel.SetActive(false);
        m_contentPanel.SetActive(true);
        Debug.Log("PF Login Success LoginWithGooglePlayGamesServices");

        m_contentText.text = 
            result?.PlayFabId + "\n" +
            "newly created : " + result?.NewlyCreated + "\n" +
            "last login time : " + result?.LastLoginTime + "\n" +
            "display name : " + result?.InfoResultPayload?.PlayerProfile?.DisplayName;
    }

    private void OnLoginWithGooglePlayGamesServicesFailure(PlayFabError error)
    {
        m_loginPanel.SetActive(true);
        m_contentPanel.SetActive(false);
        m_connectionLoading.SetActive(false);
        Debug.Log("PF Login Failure LoginWithGooglePlayGamesServices: " + error.GenerateErrorReport());
    }
}
