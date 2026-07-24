using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoostManager : MonoBehaviour
{
	[Serializable]
	public struct SpriteBoost
	{
		public Sprite sprite;

		public int effective;
	}

	public static BoostManager instance;

	private bool boosting;

	private int totalRemaining;

	[NonSerialized]
	public int totalEffective;

	[NonSerialized]
	public BoostData boostData;

	[SerializeField]
	public Configuration configuration;

	[SerializeField]
	private Image boostBorder;

	[SerializeField]
	private Image nextBoostFill;

	[SerializeField]
	private Image currentBoostFill;

	[SerializeField]
	private Image[] boostItem;

	[SerializeField]
	private BoostManager.SpriteBoost[] spriteBoost;

	[SerializeField]
	private Text boostDescription;

	[SerializeField]
	private Text inventoryEffective;

	[SerializeField]
	private Text inventoryRemaining;

	[SerializeField]
	private Text mainScreenEffective;

	[SerializeField]
	private Text mainScreenRemaining;

	[SerializeField]
	private GameObject boostVFX;

	[SerializeField]
	private GameObject targetPopup;

	[SerializeField]
	private GameObject enablePanel;

	[SerializeField]
	private GameObject disablePanel;

	private WaitForSeconds waitForSeconds = new WaitForSeconds(1f);

	private void Awake()
	{
		if (BoostManager.instance == null)
		{
			BoostManager.instance = this;
		}
		else if (BoostManager.instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public void Initialize()
	{
		int targetRestaurant = Singleton<DataManager>.Instance.database.targetRestaurant;
		this.boostData = Singleton<DataManager>.Instance.database.restaurant[targetRestaurant].boost;
		this.OfflineTimeCalculate();
		this.TotalEffectiveCompute();
		this.AdBoostPopupDisplay();
	}

	public void TotalEffectiveCompute()
	{
		int num = 0;
		for (int k = 0; k < this.boostData.boosts.Count; k++)
		{
			num += this.boostData.boosts[k].effective;
		}
		if (num == 0)
		{
			num = 1;
		}
		int i;
		for (i = 0; i < this.boostItem.Length; i++)
		{
			this.boostItem[i].transform.gameObject.SetActive(i < this.boostData.boosts.Count);
			if (i < this.boostData.boosts.Count)
			{
				this.boostItem[i].transform.GetChild(1).GetComponent<Image>().sprite = Array.Find<BoostManager.SpriteBoost>(this.spriteBoost, (BoostManager.SpriteBoost target) => target.effective == this.boostData.boosts[i].effective).sprite;
				this.boostItem[i].transform.GetChild(1).GetComponent<Image>().SetNativeSize();
			}
		}
		int num2 = (!Singleton<DataManager>.Instance.database.nonConsume.Contains("onlinepack")) ? 1 : this.configuration.boost.onlineBoostEffective;
		int num3 = (this.boostData.boostRemaining <= 0) ? 1 : this.configuration.boost.boostIncomeEffective;
		this.boostBorder.enabled = (this.boostData.boostRemaining > 0);
		this.boostVFX.SetActive(this.boostData.boostRemaining > 0);
		this.totalEffective = num * num3 * num2;
		this.mainScreenEffective.transform.parent.gameObject.SetActive(this.totalEffective > 1);
		GameUtilities.String.ToText(this.inventoryEffective, "x" + this.totalEffective.ToString());
		GameUtilities.String.ToText(this.mainScreenEffective, "x" + this.totalEffective.ToString());
		if (this.boostData.boosts.Count > 0)
		{
			this.totalRemaining = this.boostData.boosts[0].remaining;
			for (int j = 0; j < this.boostData.boosts.Count; j++)
			{
				if (this.boostData.boosts[j].remaining < this.totalRemaining)
				{
					this.totalRemaining = this.boostData.boosts[j].remaining;
				}
			}
		}
		if (this.totalRemaining > 0)
		{
			if (this.boostData.boostRemaining > 0 && this.boostData.boostRemaining < this.totalRemaining)
			{
				this.totalRemaining = this.boostData.boostRemaining;
			}
		}
		else
		{
			this.totalRemaining = this.boostData.boostRemaining;
		}
		if (this.totalRemaining == 0)
		{
			GameUtilities.String.ToText(this.mainScreenRemaining, "BOOST");
			if (Singleton<DataManager>.Instance.database.nonConsume.Contains("onlinepack"))
			{
				GameUtilities.String.ToText(this.inventoryRemaining, "Unlimited");
			}
		}
		if (this.totalRemaining > 0 && !this.boosting)
		{
			base.StartCoroutine(this.Boosting());
		}
		this.enablePanel.SetActive(this.totalRemaining > 0 || Singleton<DataManager>.Instance.database.nonConsume.Contains("onlinepack"));
		this.disablePanel.SetActive(this.totalRemaining == 0 && !Singleton<DataManager>.Instance.database.nonConsume.Contains("onlinepack"));
	}

	public void AddBoostItem(Boost boost)
	{
		for (int i = 0; i < this.boostData.boosts.Count; i++)
		{
			if (this.boostData.boosts[i].effective == boost.effective)
			{
				this.boostData.boosts[i].remaining += boost.remaining;
				this.TotalEffectiveCompute();
				return;
			}
		}
		this.boostData.boosts.Add(boost);
		this.TotalEffectiveCompute();
	}

	public void WatchAdBoost()
	{
		if (!AdsControl.Instance.GetRewardAvailable())
		{
			Notification.instance.Warning("No available video at the moment.");
			Singleton<SoundManager>.Instance.Play("Notification");
			return;
		}
		if (AdsControl.Instance.GetRewardAvailable())
		{
			AdsControl.Instance.PlayDelegateRewardVideo(delegate
			{
				this.boostData.boostRemaining += this.configuration.boost.boostIncomeDuration;
				if (this.boostData.boostRemaining > this.configuration.boost.boostIncomeMaxDuration)
				{
					this.boostData.boostRemaining = this.configuration.boost.boostIncomeMaxDuration;
				}
				this.TotalEffectiveCompute();
				this.AdBoostPopupDisplay();
				Tracking.instance.Ads_Impress("reward", "BoostIncome");
			});
			Tracking.instance.UI_Interaction("BoostPopup", "WatchAdsBoost");
		}
	}

	public void ShowPopup(bool value)
	{
		if (value)
		{
			Singleton<SoundManager>.Instance.Play("Popup");
		}
		this.targetPopup.SetActive(value);
		if (!value)
		{
			Tracking.instance.UI_Interaction("BoostPopup", "ClosePopup");
		}
	}

	private IEnumerator Boosting()
	{
		this.boosting = true;
		while (this.totalRemaining > 0)
		{
			if (this.boostData.boostRemaining > 0)
			{
				this.boostData.boostRemaining--;
			}
			for (int i = 0; i < this.boostData.boosts.Count; i++)
			{
				this.boostData.boosts[i].remaining--;
			}
			this.totalRemaining--;
			this.AdBoostPopupDisplay();
			GameUtilities.String.ToText(this.inventoryRemaining, GameUtilities.DateTime.Convert(this.totalRemaining));
			GameUtilities.String.ToText(this.mainScreenRemaining, GameUtilities.DateTime.Convert(this.totalRemaining));
			yield return this.waitForSeconds;

			if (this.totalRemaining == 0)
			{
				this.boostData.boosts.RemoveAll((Boost item) => item.remaining == 0);
				this.TotalEffectiveCompute();
			}
		}
		this.boosting = false;
	}

	private void AdBoostPopupDisplay()
	{
		float num = (float)this.configuration.boost.boostIncomeEffective;
		int num2 = this.configuration.boost.boostIncomeMaxDuration - this.boostData.boostRemaining;
		if (num2 > this.configuration.boost.boostIncomeDuration)
		{
			num2 = this.configuration.boost.boostIncomeDuration;
		}
		GameUtilities.String.ToText(this.boostDescription, string.Concat(new object[]
		{
			"<color=#009FD6FF>x",
			num,
			" income</color> for additional ",
			GameUtilities.DateTime.Convert(num2)
		}));
		this.nextBoostFill.fillAmount = (float)(this.boostData.boostRemaining + this.configuration.boost.boostIncomeDuration) / (float)this.configuration.boost.boostIncomeMaxDuration;
		this.currentBoostFill.fillAmount = (float)this.boostData.boostRemaining / (float)this.configuration.boost.boostIncomeMaxDuration;
	}

	private void OfflineTimeCalculate()
	{
		int targetRestaurant = Singleton<DataManager>.Instance.database.targetRestaurant;
		int num = GameUtilities.DateTime.Offline(Singleton<DataManager>.Instance.database.restaurant[targetRestaurant].dateTime);
		if (this.boostData.boostRemaining > num)
		{
			this.boostData.boostRemaining -= num;
		}
		else
		{
			this.boostData.boostRemaining = 0;
		}
		for (int i = 0; i < this.boostData.boosts.Count; i++)
		{
			if (this.boostData.boosts[i].remaining > num)
			{
				this.boostData.boosts[i].remaining -= num;
			}
			else
			{
				this.boostData.boosts[i].remaining = 0;
			}
		}
		this.boostData.boosts.RemoveAll((Boost item) => item.remaining == 0);
	}

	private void OnApplicationPause(bool paused)
	{
		if (!paused && this.boostData != null)
		{
			this.OfflineTimeCalculate();
			this.TotalEffectiveCompute();
		}
	}
}
