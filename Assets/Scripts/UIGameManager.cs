using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using UnityEditor;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

public class LoginWithGoogleAccountRequestAccessToken : LoginWithGoogleAccountRequest
{
    public string AccessToken;
}

public class UIGameManager : MonoBehaviour
{
    [SerializeField] GameObject m_loginPanel;
    [SerializeField] GameObject m_contentPanel;
    [SerializeField] Text m_versionText;

    [Header("Login with email")]
    [SerializeField] InputField m_email_inputField;
    [SerializeField] InputField m_password_inputField;

    [Header("Register")]
    [SerializeField] InputField m_username_inputField;

    [Header("Content")]
    [SerializeField] Text m_contentText;
    [SerializeField] Button m_requestServerSideAccessButton;

    private void _OnPlayFabLoginSuccess(LoginResult result)
    {
        m_contentPanel.SetActive(true);

        var name = result.NewlyCreated ? "Guest" : result.PlayFabId;
        m_contentText.text = "Hi again,\n" + name;
    }

    private void _OnPlayFabRegisterSuccess(RegisterPlayFabUserResult result)
    {
        PlayFabClientAPI.LinkCustomID(new LinkCustomIDRequest()
        {
            CustomId = m_username_inputField.text
        }, (LinkCustomIDResult linkResult) =>
        {
            m_contentPanel.SetActive(true);
            m_contentText.text = "Welcome,\n" + result.Username;
        }, _OnPlayFabError);
    }

    private void _OnPlayFabError(PlayFabError error)
    {
        m_loginPanel.SetActive(true);
        Debug.LogError(error.GenerateErrorReport());
    }

    private void _GooglePlaySetup()
    {
        Debug.LogWarning("// ================================= Setup google play ...");

        //PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
        //.AddOauthScope("https://www.googleapis.com/auth/userinfo.profile")
        //.AddOauthScope("https://www.googleapis.com/auth/games")
        //.AddOauthScope("https://www.googleapis.com/auth/games_lite")
        //.AddOauthScope("https://www.googleapis.com/auth/drive.appdata")
        //.AddOauthScope("https://www.googleapis.com/auth/userinfo.email")
        //.AddOauthScope("https://www.googleapis.com/auth/androidpublisher")
        //.AddOauthScope("profile")
        //.AddOauthScope("openid")
        //.RequestServerAuthCode(false)
        //.RequestEmail()
        //.RequestIdToken()
        //.Build();
        //PlayGamesPlatform.InitializeInstance(config);

        PlayGamesPlatform.DebugLogEnabled = true;

        PlayGamesPlatform.Activate();
    }

    private void Awake()
    {
        m_versionText.text = "v." + Application.version;

        _GooglePlaySetup();
    }

    public void SilentLoginButton()
    {
        m_loginPanel.SetActive(false);
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        }, _OnPlayFabLoginSuccess, _OnPlayFabError);
    }

    public void LoginWithEmailButton()
    {
        if (string.IsNullOrEmpty(m_email_inputField.text) || string.IsNullOrEmpty(m_password_inputField.text))
            return;

        m_loginPanel.SetActive(false);
        PlayFabClientAPI.LoginWithEmailAddress(new LoginWithEmailAddressRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            Email = m_email_inputField.text,
            Password = m_password_inputField.text
        }, _OnPlayFabLoginSuccess, _OnPlayFabError);
    }

    public void CreateAccountWithEmailButton()
    {
        if (string.IsNullOrEmpty(m_email_inputField.text) || string.IsNullOrEmpty(m_password_inputField.text) || string.IsNullOrEmpty(m_username_inputField.text))
            return;

        m_loginPanel.SetActive(false);
        PlayFabClientAPI.RegisterPlayFabUser(new RegisterPlayFabUserRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            Email = m_email_inputField.text,
            Password = m_password_inputField.text,
            DisplayName = m_username_inputField.text,
            Username = m_username_inputField.text,
            RequireBothUsernameAndEmail = true
        }, _OnPlayFabRegisterSuccess, _OnPlayFabError);
    }

    public void LoginWithGooglePlayButton(bool manual = false)
    {
        Debug.LogWarning("// ================================= Login with Google Play Button() - manual? " + manual);
        m_loginPanel.SetActive(false);
        // cara ke 1 :
        if (!manual)
            PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
        else
            PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);

        // cara ke 2:
        //PlayGamesPlatform.Activate();
        //Social.localUser.Authenticate(ProcessAuthentication);

        //cara ke 3:
        //Social.localUser.Authenticate((bool success) =>
        //{
        //    Debug.LogWarning("// ================================= social local user authenticate() - " + success);

        //    if (success)
        //    {
        //        m_contentPanel.SetActive(true);
        //        m_contentText.text = "Bisa login pake google play cuuuuy\n"+ Social.localUser.userName;
        //        var serverAuthCode = PlayGamesPlatform.Instance.GetServerAuthCode();
        //        Debug.LogWarning("// ================================= Server Auth Code: " + serverAuthCode + " - authenticated : " + PlayGamesPlatform.Instance.IsAuthenticated());
        //        Debug.LogWarning("// ================================= Login with Google done. IdToken: " + ((PlayGamesLocalUser)Social.localUser).GetIdToken());
        //        PlayFabClientAPI.LoginWithGoogleAccount(new LoginWithGoogleAccountRequest()
        //        {
        //            TitleId = PlayFabSettings.TitleId,
        //            CreateAccount = true,
        //            ServerAuthCode = serverAuthCode,
        //        }, _OnPlayFabLoginSuccess, _OnPlayFabError);
        //    }
        //    else
        //    {
        //        m_loginPanel.SetActive(true);
        //        Debug.LogError("// ================================= Failed to google play auth");
        //    }
        //});
    }

    public void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.LogWarning("// ================================= Login with Google done. Id: " + PlayGamesPlatform.Instance.localUser.id);
            m_contentPanel.SetActive(true);
            m_contentText.text = "Bisa login pake google play cuuuuy\n" + PlayGamesPlatform.Instance.localUser.userName;
            RequestServerSideAccessButton();
        }
        else
        {
            m_loginPanel.SetActive(true);
            Debug.LogError("// ======================================== Failed google play auth : " + status.ToString());
        }
    }

    public void RequestServerSideAccessButton()
    {
        m_requestServerSideAccessButton.gameObject.SetActive(false);
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, (serverAuthCode) =>  // masih ke cancel disini??? server auth codenya null
        {
            Debug.LogWarning("// ================================= Server Auth Code: " + serverAuthCode + " - authenticated : " + PlayGamesPlatform.Instance.IsAuthenticated());
            if(!string.IsNullOrEmpty(serverAuthCode))
            {
                PlayFabClientAPI.LoginWithGooglePlayGamesServices(new LoginWithGooglePlayGamesServicesRequest()
                {
                    TitleId = PlayFabSettings.TitleId,
                    CreateAccount = true,
                    ServerAuthCode = serverAuthCode,
                    //EncryptedRequest
                }, _OnPlayFabLoginSuccess, _OnPlayFabError);
            } else
            {
                m_requestServerSideAccessButton.gameObject.SetActive(true);
            }
        });
    }

#if UNITY_EDITOR
    [MenuItem("Nganu/Clear Credentials")]
    static void ClearCredentialsMenu()
    {
        PlayFabClientAPI.ForgetAllCredentials();
        Debug.LogWarning("// ================================= Forget all credentials ... done");
    }
#endif
}
