using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class UIGameManager2 : MonoBehaviour
{
    [SerializeField] GameObject m_loginPanel;
    [SerializeField] Button m_loginButton_mobile;
    [SerializeField] Button m_loginButton_pc;

    [SerializeField] GameObject m_contentPanel;
    [SerializeField] Text m_contentText;

    [SerializeField] GameObject m_connectionLoading;
    [SerializeField] Text m_versionText;

    public void PCSignInButton()
    {
        m_connectionLoading.SetActive(true);
        LaunchBrowserAuth();
        
        //var request = new LoginWithGoogleAccountRequest()
        //{
        //    CreateAccount = true,
        //    TitleId = PlayFabSettings.TitleId,
        //    ServerAuthCode = ""
        //};
        //PlayFabClientAPI.LoginWithGoogleAccount(request, OnLoginWithGooglePlayGamesServicesSuccess, OnLoginWithGooglePlayGamesServicesFailure);
    }

    public void AndroidSignInButton()
    {
#if UNITY_ANDROID
        m_connectionLoading.SetActive(true);

        PlayGamesPlatform.Instance.Authenticate(_ProcessAuthentication);
#endif
    }

    private void Awake()
    {
        m_versionText.text = "v." + Application.version;

        m_connectionLoading.SetActive(false);
        m_loginPanel.SetActive(true);
        //m_loginButton_mobile.interactable = SystemInfo.deviceType == DeviceType.Handheld;
        //m_loginButton_pc.interactable = SystemInfo.deviceType == DeviceType.Desktop;

        m_contentPanel.SetActive(false);

#if UNITY_ANDROID
        PlayGamesPlatform.Activate();
#endif
    }

    private void OnEnable()
    {
        if(SystemInfo.deviceType == DeviceType.Desktop)
        {
            ProcessDeepLinkMngr.Instance.OnDeeplinkLoginSuccess += OnLoginWithGooglePlayGamesServicesSuccess;
            ProcessDeepLinkMngr.Instance.OnDeeplinkLoginFailure += OnLoginWithGooglePlayGamesServicesFailure;
        }
    }

    private void OnDisable()
    {
        if (SystemInfo.deviceType == DeviceType.Desktop)
        {
            ProcessDeepLinkMngr.Instance.OnDeeplinkLoginSuccess -= OnLoginWithGooglePlayGamesServicesSuccess;
            ProcessDeepLinkMngr.Instance.OnDeeplinkLoginFailure -= OnLoginWithGooglePlayGamesServicesFailure;
        }
    }

    private string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private string redirectURI = "https://oauth.playfab.com/oauth2/google";
    private object clientID = "357333282570-ijm1oov722u4pb2rr577dvom1ldijfs6.apps.googleusercontent.com";
    private string scopes = "profile";

    private void LaunchBrowserAuth()
    {
        string authorizationRequest = string.Format("{0}?response_type=code&scope={1}&redirect_uri={2}&client_id={3}",
                authorizationEndpoint,
                scopes,
                Uri.EscapeDataString(redirectURI),
                clientID);

        Application.OpenURL(authorizationRequest);
    }

#if UNITY_ANDROID
    private void _ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, ProcessServerAuthCode);
        }
        else
        {
            m_connectionLoading.SetActive(false);
            m_loginPanel.SetActive(true);
            m_contentPanel.SetActive(false);
            Debug.LogError("// ======================================== Failed google play auth : " + status.ToString());
        }
    }

    private void ProcessServerAuthCode(string serverAuthCode)
    {
        Debug.Log("Server Auth Code: " + serverAuthCode);

        var request = new LoginWithGooglePlayGamesServicesRequest
        {
            ServerAuthCode = serverAuthCode,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId
        };

        PlayFabClientAPI.LoginWithGooglePlayGamesServices(request, OnLoginWithGooglePlayGamesServicesSuccess, OnLoginWithGooglePlayGamesServicesFailure);
    }
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
