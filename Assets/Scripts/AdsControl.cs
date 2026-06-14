using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SocialPlatforms;
using GoogleMobileAds.Api;
using System;
using UnityEngine.Advertisements;
using UnityEngine.UI;
public enum ShowResult
{
    Finished,
    Skipped,
    Failed
}

public class AdsControl : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{


    protected AdsControl()
    {
    }

    private static AdsControl _instance;
    InterstitialAd interstitial;
    RewardBasedVideoAd rewardBasedVideo;
    BannerView bannerView;
    
    private bool isInterstitialLoaded = false;
    private bool isRewardLoaded = false;
    private Action<ShowResult> activeShowResultCallback;
    private Action<bool> activeBoolCallback;

    public string AdmobID_Android, AdmobID_IOS, BannerID_Android, BannerID_IOS;
    public string UnityID_Android, UnityID_IOS, UnityZoneID;

    public static AdsControl Instance { get { return _instance; } }

    void Awake()
    {
        if (FindObjectsOfType(typeof(AdsControl)).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        MakeNewInterstial();
        RequestBanner();
        if (PlayerPrefs.GetInt("RemoveAds") == 0)
            ShowBanner();
        else
            HideBanner();
        if (Advertisement.isSupported)
        { // If the platform is supported,
            string gameId = "";
#if UNITY_IOS
            gameId = UnityID_IOS;
#elif UNITY_ANDROID
            gameId = UnityID_Android;
#endif
            if (!string.IsNullOrEmpty(gameId))
            {
                Advertisement.Initialize(gameId, false, this);
            }
        }

        DontDestroyOnLoad(gameObject); //Already done by CBManager


    }


    public void HandleInterstialAdClosed(object sender, EventArgs args)
    {

        if (interstitial != null)
            interstitial.Destroy();
        MakeNewInterstial();



    }

    void MakeNewInterstial()
    {


#if UNITY_ANDROID
        interstitial = new InterstitialAd(AdmobID_Android);
#endif
#if UNITY_IPHONE
		interstitial = new InterstitialAd (AdmobID_IOS);
#endif
        interstitial.OnAdClosed += HandleInterstialAdClosed;
        AdRequest request = new AdRequest.Builder().Build();
        interstitial.LoadAd(request);


    }


    public void showAds()
    {
        int adsCounter = PlayerPrefs.GetInt("AdsCounter");

        if (adsCounter >= 2)
        {
            if (PlayerPrefs.GetInt("RemoveAds") == 0)
            {
                if (interstitial.IsLoaded())
                    interstitial.Show();
                else if (isInterstitialLoaded)
                    Advertisement.Show("video", this);  
            }
            adsCounter = 0;
        }
        else
        {
            adsCounter++;
        }

        PlayerPrefs.SetInt("AdsCounter", adsCounter);
    }


    public bool GetRewardAvailable()
    {
        return isRewardLoaded;
    }

    public void ShowRewardVideo()
    {
        activeShowResultCallback = HandleShowResult;
        activeBoolCallback = null;
        Advertisement.Show(UnityZoneID, this);
    }


    private void RequestBanner()
    {
#if UNITY_EDITOR
        string adUnitId = "unused";
#elif UNITY_ANDROID
		string adUnitId = BannerID_Android;
#elif UNITY_IPHONE
		string adUnitId = BannerID_IOS;
#else
		string adUnitId = "unexpected_platform";
#endif

        // Create a 320x50 banner at the top of the screen.
        bannerView = new BannerView(adUnitId, AdSize.SmartBanner, AdPosition.Bottom);

        // Create an empty ad request.
        AdRequest request = new AdRequest.Builder().Build();
        // Load the banner with the request.
        bannerView.LoadAd(request);

    }

    public void ShowBanner()
    {
        bannerView.Show();
    }

    public void HideBanner()
    {
        bannerView.Hide();
    }



    public void ShowFB()
    {
        Application.OpenURL("https://www.facebook.com/PonyStudio2507/?ref=settings");
    }

    public void RateMyGame()
    {
#if UNITY_EDITOR
        Application.OpenURL("https://itunes.apple.com/us/app/color-flow-puzzle/id1436566275?ls=1&mt=8");
#elif UNITY_ANDROID
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.ponygames.MagicBlockPuzzle");
#elif UNITY_IPHONE
        Application.OpenURL("https://itunes.apple.com/us/app/color-flow-puzzle/id1436566275?ls=1&mt=8");
#else
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.ponygames.MagicBlockPuzzle");
#endif


    }

    private void HandleShowResult(ShowResult result)
    {
        switch (result)
        {
            case ShowResult.Finished:
        
                break;
            case ShowResult.Skipped:
                break;
            case ShowResult.Failed:
                break;
        }
    }

    public void PlayCallbackRewardVideo(Action<ShowResult> _action)
    {
        activeShowResultCallback = _action;
        activeBoolCallback = null;
        Advertisement.Show(UnityZoneID, this);
    }

    public void PlayDelegateRewardVideo(Action<bool> onVideoPlayed)
    {
        if (isRewardLoaded)
        {
            activeShowResultCallback = null;
            activeBoolCallback = onVideoPlayed;
            Advertisement.Show(UnityZoneID, this);
        }
        else
        {
            onVideoPlayed(false);
        }
    }

    // IUnityAdsInitializationListener
    public void OnInitializationComplete()
    {
        Advertisement.Load("video", this);
        Advertisement.Load(UnityZoneID, this);
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }

    // IUnityAdsLoadListener
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId == UnityZoneID)
        {
            isRewardLoaded = true;
        }
        else if (adUnitId == "video")
        {
            isInterstitialLoaded = true;
        }
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        if (adUnitId == UnityZoneID)
        {
            isRewardLoaded = false;
        }
        else if (adUnitId == "video")
        {
            isInterstitialLoaded = false;
        }
    }

    // IUnityAdsShowListener
    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        if (adUnitId == UnityZoneID)
        {
            isRewardLoaded = false;
            if (activeShowResultCallback != null) activeShowResultCallback(ShowResult.Failed);
            if (activeBoolCallback != null) activeBoolCallback(false);
            activeShowResultCallback = null;
            activeBoolCallback = null;
        }
        else if (adUnitId == "video")
        {
            isInterstitialLoaded = false;
        }
        Advertisement.Load(adUnitId, this);
    }

    public void OnUnityAdsShowStart(string adUnitId) {}
    public void OnUnityAdsShowClick(string adUnitId) {}

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId == UnityZoneID)
        {
            isRewardLoaded = false;
            if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                if (activeShowResultCallback != null) activeShowResultCallback(ShowResult.Finished);
                if (activeBoolCallback != null) activeBoolCallback(true);
            }
            else if (showCompletionState == UnityAdsShowCompletionState.SKIPPED)
            {
                if (activeShowResultCallback != null) activeShowResultCallback(ShowResult.Skipped);
                if (activeBoolCallback != null) activeBoolCallback(false);
            }
            else
            {
                if (activeShowResultCallback != null) activeShowResultCallback(ShowResult.Failed);
                if (activeBoolCallback != null) activeBoolCallback(false);
            }
            activeShowResultCallback = null;
            activeBoolCallback = null;
        }
        else if (adUnitId == "video")
        {
            isInterstitialLoaded = false;
        }
        Advertisement.Load(adUnitId, this);
    }
}

