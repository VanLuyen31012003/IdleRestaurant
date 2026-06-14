using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GetFreeCoin : MonoBehaviour
{
	private bool cooldown;

	private Coroutine cooldowing;

	private DateTime currentTime;

	private FreeCoinData freeCoinData;

	private WaitForSeconds waitForSeconds;

	[SerializeField]
	private Configuration config;

	[SerializeField]
	private Text watchAdsRemaining;

	[SerializeField]
	private GameObject watchAdsLabel;

	[SerializeField]
	private GameObject freeCashButton;

	[SerializeField]
	private GameObject watchAdsButton;

	[SerializeField]
	private GameObject[] notification;

	private void Start()
	{
		this.waitForSeconds = new WaitForSeconds(1f);
		this.freeCoinData = Singleton<DataManager>.Instance.database.freeCashData;
		base.StartCoroutine(this.Initialize());
	}

	private IEnumerator Initialize()
	{
		using (UnityWebRequest www = UnityWebRequest.Get("https://mega.ikame.vn/index.php?index=get_time"))
		{
			yield return www.SendWebRequest();

			if (www.result == UnityWebRequest.Result.Success)
			{
				try
				{
					this.currentTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					this.currentTime = this.currentTime.AddSeconds(Convert.ToDouble(www.downloadHandler.text)).ToLocalTime();
					DateTime dateTime = Convert.ToDateTime(this.freeCoinData.lastTimeGetFree);
					if ((dateTime.Day != this.currentTime.Day || dateTime.Month != this.currentTime.Month) && !Singleton<DataManager>.Instance.database.freeCashData.free)
					{
						Singleton<DataManager>.Instance.database.freeCashData.free = true;
					}
					if (this.freeCoinData.watchAds == this.config.freeCash.watchAdLimited)
					{
						int num2 = (int)this.currentTime.Subtract(Convert.ToDateTime(this.freeCoinData.lastTimeWatchAd)).TotalSeconds;
						if (num2 >= this.config.freeCash.cooldownPerAds)
						{
							this.freeCoinData.watchAds = 0;
						}
					}
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogError("Error parsing server time: " + ex.Message);
				}
			}
			else
			{
				UnityEngine.Debug.LogWarning("Failed to get server time: " + www.error);
			}
		}
		this.FreeCashValidate();
	}

	private void FreeCashValidate()
	{
		this.freeCashButton.SetActive(this.freeCoinData.free);
		this.watchAdsButton.SetActive(!this.freeCoinData.free);
		for (int i = 0; i < this.notification.Length; i++)
		{
			this.notification[i].SetActive(this.freeCoinData.free);
		}
		bool flag = this.freeCoinData.watchAds == this.config.freeCash.watchAdLimited;
		this.watchAdsLabel.SetActive(!flag);
		this.watchAdsRemaining.gameObject.SetActive(flag);
		if (flag && !this.cooldown)
		{
			this.cooldowing = base.StartCoroutine(this.Cooldown());
		}
	}

	public void GetFreeCash()
	{
		this.freeCoinData.free = false;
		this.freeCoinData.lastTimeGetFree = DateTime.Now.ToString();
		Singleton<GameManager>.Instance.SetDiamond(this.config.freeCash.diamondBonus);
		Notification.instance.Warning("Received <color=#00FFDFFF>" + this.config.freeCash.diamondBonus.ToString() + "</color> diamond");
		Singleton<SoundManager>.Instance.Play("Rewarded");
		this.FreeCashValidate();
	}

	public void WatchAdsFreeCash()
	{
		if (!AdsControl.Instance.GetRewardAvailable())
		{
			Notification.instance.Warning("No available video at the moment.");
			Singleton<SoundManager>.Instance.Play("Notification");
			return;
		}
		AdsControl.Instance.PlayDelegateRewardVideo(delegate
		{
			if (this.freeCoinData.watchAds == this.config.freeCash.watchAdLimited)
			{
				return;
			}
			this.freeCoinData.watchAds++;
			if (this.freeCoinData.watchAds == this.config.freeCash.watchAdLimited)
			{
				this.freeCoinData.lastTimeWatchAd = DateTime.Now.ToString();
			}
			Singleton<GameManager>.Instance.SetDiamond(this.config.freeCash.diamondBonus);
			Notification.instance.Warning("Received <color=#00FFDFFF>" + this.config.freeCash.diamondBonus.ToString() + "</color> diamond");
			Singleton<SoundManager>.Instance.Play("Rewarded");
			this.FreeCashValidate();
			Tracking.instance.Ads_Impress("reward", "GetFreeDiamond");
		});
	}

	private IEnumerator Cooldown()
	{
		this.cooldown = true;
		int duration = (int)DateTime.Now.Subtract(Convert.ToDateTime(this.freeCoinData.lastTimeWatchAd)).TotalSeconds;
		duration = Mathf.Clamp(this.config.freeCash.cooldownPerAds - duration, 0, this.config.freeCash.cooldownPerAds);

		while (duration > 0)
		{
			GameUtilities.String.ToText(this.watchAdsRemaining, GameUtilities.DateTime.Convert(duration));
			yield return this.waitForSeconds;
			duration--;
		}

		this.cooldown = false;
		this.watchAdsLabel.SetActive(true);
		this.watchAdsRemaining.gameObject.SetActive(false);
		this.freeCoinData.watchAds = 0;
	}

	private void OnApplicationPause(bool paused)
	{
		if (paused)
		{
			if (this.cooldown)
			{
				this.cooldown = false;
				base.StopCoroutine(this.cooldowing);
			}
		}
		else
		{
			base.StartCoroutine(this.Initialize());
		}
	}
}
