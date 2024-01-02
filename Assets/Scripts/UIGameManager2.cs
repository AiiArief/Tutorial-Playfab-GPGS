using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using Assets.SimpleGoogleSignIn.Scripts;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
#if UNITY_ANDROID
//using GooglePlayGames;
//using GooglePlayGames.BasicApi;
#endif

public class LoginRequest : LoginWithGoogleAccountRequest
{
    public string AccessToken;
}

public class UIGameManager2 : MonoBehaviour, IDetailedStoreListener
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

        m_contentPanel.SetActive(false);
    }

    private void OnSignIn(bool success, string error, TokenResponse tokenResponse)
    {
        if (success)
        {
            Debug.Log("Access token : " + tokenResponse.AccessToken);
            ProcessServerAuthCode(tokenResponse.AccessToken);
        }
        else
        {
            Debug.Log("Error when getting Auth Code : " + error);
            PlayFabError errorPF = new PlayFabError();
            OnPlayfabLoginWithGoogleFailure(errorPF);
        }
    }

    private void ProcessServerAuthCode(string serverAuthCode)
    {
        Debug.Log("Server Auth Code: " + serverAuthCode);

        var request = new LoginRequest
        {
            AccessToken = serverAuthCode,
            CreateAccount = true,
            TitleId = PlayFabSettings.TitleId,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams()
            {
                GetPlayerProfile = true
            }
        };

        PlayFabClientAPI.LoginWithGoogleAccount(request, OnPlayfabLoginWithGoogleSuccess, OnPlayfabLoginWithGoogleFailure);
    }

    private void OnPlayfabLoginWithGoogleSuccess(LoginResult result)
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

    private void OnPlayfabLoginWithGoogleFailure(PlayFabError error)
    {
        m_loginPanel.SetActive(true);
        m_contentPanel.SetActive(false);
        m_connectionLoading.SetActive(false);
        Debug.Log("PF Login Failure LoginWithGooglePlayGamesServices: " + error.GenerateErrorReport());
    }

    // ============================================================================= Purchasing ===============================================

    [Header("Subs")]
    [SerializeField] Button m_subsButton;

    public const string environment = "production";
    public const string subscriptionProductId = "subs_donate";

    IStoreController m_StoreController;

    async void Start()
    {
        try
        {
            var options = new InitializationOptions()
                .SetEnvironmentName(environment);

            await UnityServices.InitializeAsync(options);

            _InitializePurchasing();
        }
        catch (Exception exception)
        {
            Debug.LogError(exception);
        }
    }
    public void BuySubscription()
    {
        m_StoreController.InitiatePurchase(subscriptionProductId);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("In-App Purchasing successfully initialized");
        m_StoreController = controller;

        _UpdateSubButton();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        OnInitializeFailed(error, null);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        var product = args.purchasedProduct;

        Debug.Log($"Purchase Complete - Product: {product.definition.id}");

        _UpdateSubButton();

        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.Log($"Purchase failed - Product: '{product.definition.id}'," +
            $" Purchase failure reason: {failureDescription.reason}," +
            $" Purchase failure details: {failureDescription.message}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        var errorMessage = $"Purchasing failed to initialize. Reason: {error}.";

        if (message != null)
        {
            errorMessage += $" More details: {message}";
        }

        Debug.Log(errorMessage);
    }

    private void _InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        builder.AddProduct(subscriptionProductId, ProductType.Subscription);

        UnityPurchasing.Initialize(this, builder);
    }

    private void _UpdateSubButton()
    {
        var subscriptionProduct = m_StoreController.products.WithID(subscriptionProductId);

        try
        {
            var isSubscribed = _IsSubscribedTo(subscriptionProduct);
            m_subsButton.interactable = !isSubscribed;
            m_subsButton.GetComponentInChildren<Text>().text = isSubscribed ? "Subscribed" : "Subs :\n" + subscriptionProduct.metadata.localizedPriceString;
        }
        catch (StoreSubscriptionInfoNotSupportedException)
        {
            var receipt = (Dictionary<string, object>)MiniJson.JsonDecode(subscriptionProduct.receipt);
            var store = receipt["Store"];
            m_subsButton.GetComponentInChildren<Text>().text =
                "Couldn't retrieve subscription information because your current store is not supported.\n" +
                $"Your store: \"{store}\"\n\n" +
                "You must use the App Store, Google Play Store or Amazon Store to be able to retrieve subscription information.\n\n" +
                "For more information, see README.md";
        }
    }

    private bool _IsSubscribedTo(Product subscription)
    {
        // If the product doesn't have a receipt, then it wasn't purchased and the user is therefore not subscribed.
        if (subscription.receipt == null)
        {
            return false;
        }

        //The intro_json parameter is optional and is only used for the App Store to get introductory information.
        var subscriptionManager = new SubscriptionManager(subscription, null);

        // The SubscriptionInfo contains all of the information about the subscription.
        // Find out more: https://docs.unity3d.com/Packages/com.unity.purchasing@3.1/manual/UnityIAPSubscriptionProducts.html
        var info = subscriptionManager.getSubscriptionInfo();

        return info.isSubscribed() == Result.True;
    }
}
