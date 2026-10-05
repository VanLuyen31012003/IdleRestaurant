using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class BarrierController : MonoBehaviour
{
	[Serializable]
	public struct General
	{
		public GameObject barrierEnabled;

		public GameObject barrierDisable;

		public Sprite buttonEnableSprite;

		public Sprite buttonDisableSprite;
	}

	[Serializable]
	public struct Enable
	{
		public Image unlockCashButton;

		public Text kitchenPriceCashText;

		public Text kitchenPriceDiamondText;

		public GameObject unlockButtonDiamond;
	}

	[Serializable]
	public struct Locked
	{
		public Text processTimeText;

		public Text barrierPriceText;

		public Image unlockButtonImage;

		public GameObject lockedContent;
	}

	[Serializable]
	public struct Process
	{
		public Text diamondText;

		public Text remainingText;

		public Text reduceTimeText;

		public Image processFillBar;

		public GameObject processContent;
	}

	private bool processRunning;

	private int targetRestaurant;

	private Coroutine processing;

	private WaitForSeconds waiting;

	private BarrierData barrierData;

	[SerializeField]
	private BarrierController.General general;

	[SerializeField]
	private BarrierController.Enable enable;

	[SerializeField]
	private BarrierController.Locked locked;

	[SerializeField]
	private BarrierController.Process process;

	[SerializeField]
	private Rating rating;

	[SerializeField]
	private GameObject unlocked;

	[SerializeField]
	private Configuration config;

	[SerializeField]
	private GameObject tutorial_1;

	public void Initialize()
	{
		this.waiting = new WaitForSeconds(1f);
		Singleton<GameManager>.Instance.onCashChange += this.OnCashChange;
		Singleton<GameManager>.Instance.onIdleCashChange += this.OnIdleCashChange;
		this.targetRestaurant = Singleton<GameManager>.Instance.database.targetRestaurant;
		this.barrierData = Singleton<GameManager>.Instance.database.restaurant[this.targetRestaurant].barrier;
		this.OfflineTimeCalculate();
		this.Refresh();
		this.tutorial_1.SetActive(!GameManager.IsDoneTutorial(1));
		if (!GameManager.IsDoneTutorial(1))
		{
			Tracking.instance.Tutorial_Start("Step1");
		}
	}

	public void Refresh()
	{
		int count = Singleton<GameManager>.Instance.kitchenController.Count;
		bool flag = count < this.config.kitchen.maxFloor;
		bool flag2 = count != this.barrierData.nextBarrierFloor;
		bool flag3 = this.barrierData.state != BarrierState.Unlocked;
		this.general.barrierEnabled.SetActive(flag && flag2 && flag3);
		this.general.barrierDisable.SetActive(flag && !this.general.barrierEnabled.activeInHierarchy);
		if (flag && flag2 && flag3)
		{
			double floorPrice = Singleton<GameProcess>.Instance.GetFloorPrice(count);
			double idleCash = Singleton<GameManager>.Instance.database.restaurant[this.targetRestaurant].idleCash;
			GameUtilities.String.ToText(this.enable.kitchenPriceCashText, GameUtilities.Currencies.Convert(floorPrice));
			this.enable.unlockCashButton.sprite = ((Singleton<GameManager>.Instance.database.cash >= floorPrice) ? this.general.buttonEnableSprite : this.general.buttonDisableSprite);
			this.enable.unlockButtonDiamond.SetActive(idleCash > 0.0);
			if (idleCash == 0.0)
			{
				return;
			}
			double num = (double)Singleton<GameProcess>.Instance.GetFloorPrice(count, idleCash);
			GameUtilities.String.ToText(this.enable.kitchenPriceDiamondText, num.ToString());
		}
		else if (flag && !this.general.barrierEnabled.activeInHierarchy)
		{
			BarrierState state = this.barrierData.state;
			if (state != BarrierState.Locked)
			{
				if (state != BarrierState.Process)
				{
					if (state == BarrierState.Unlocked)
					{
						this.locked.lockedContent.SetActive(false);
						this.process.processContent.SetActive(false);
						this.unlocked.SetActive(true);
					}
				}
				else
				{
					this.locked.lockedContent.SetActive(false);
					this.process.processContent.SetActive(true);
					GameUtilities.String.ToText(this.process.reduceTimeText, "-" + GameUtilities.DateTime.Convert(this.config.barrier[count / this.config.kitchen.barrierStep - 1].reduceProcessTime));
					if (!this.processRunning)
					{
						this.processing = base.StartCoroutine(this.Processing());
					}
				}
			}
			else
			{
				this.locked.lockedContent.SetActive(true);
				double num2 = Singleton<GameProcess>.Instance.GetFloorPrice(count) / (double)this.config.kitchen.unlockBarrierFactor;
				GameUtilities.String.ToText(this.locked.barrierPriceText, GameUtilities.Currencies.Convert(num2));
				GameUtilities.String.ToText(this.locked.processTimeText, GameUtilities.DateTime.Convert(this.config.barrier[count / this.config.kitchen.barrierStep - 1].unlockDuration));
				this.locked.unlockButtonImage.sprite = ((Singleton<GameManager>.Instance.database.cash >= num2) ? this.general.buttonEnableSprite : this.general.buttonDisableSprite);
			}
		}
	}

	public void UnlockKitchenWithCoin()
	{
		int count = GameManager.Instance.kitchenController.Count;
		double floorPrice = GameProcess.Instance.GetFloorPrice(count);
		if (GameManager.Instance.database.cash < floorPrice)
		{
			return;
		}
		GameManager.Instance.UnlockKitchen();
		GameManager.Instance.SetCash(-floorPrice);
		if (!GameManager.IsDoneTutorial(1))
		{
			GameManager.TutorialDone(1);
			this.tutorial_1.SetActive(false);
			Tracking.instance.Tutorial_Done("Step1");
		}
	}

	public void UnlockKitchenWithDiamond()
	{
		int count = Singleton<GameManager>.Instance.kitchenController.Count;
		double idleCash = Singleton<GameManager>.Instance.database.restaurant[this.targetRestaurant].idleCash;
		int diamond = Singleton<GameProcess>.Instance.GetFloorPrice(count, idleCash);
		if (Singleton<GameManager>.Instance.database.diamond < diamond)
		{
			Notification.instance.Warning("Not Enough Diamond");
			Singleton<SoundManager>.Instance.Play("Notification");
			return;
		}
		Notification.instance.Confirm(delegate
		{
			Singleton<GameManager>.Instance.UnlockKitchen();
			Singleton<GameManager>.Instance.SetDiamond(-diamond);
		}, "Do you want to unlock kitchen for <color=#00B5FFFF>" + diamond.ToString() + "</color> diamond?");
	}

	public void StartProcess()
	{
		int count = Singleton<GameManager>.Instance.kitchenController.Count;
		double num = Singleton<GameProcess>.Instance.GetFloorPrice(count) / (double)this.config.kitchen.unlockBarrierFactor;
		if (Singleton<GameManager>.Instance.database.cash < num)
		{
			return;
		}
		Singleton<GameManager>.Instance.SetCash(-num);
		this.barrierData.unlockRemaining = this.config.barrier[count / this.config.kitchen.barrierStep - 1].unlockDuration;
		this.barrierData.state = BarrierState.Process;
		this.Refresh();
	}

	public void ReduceProcess()
	{
		if (!AdsControl.Instance.GetRewardAvailable())
		{
			Notification.instance.Warning("No available video at the moment.");
			Singleton<SoundManager>.Instance.Play("Notification");
			return;
		}
		AdsControl.Instance.PlayDelegateRewardVideo(delegate
		{
			int count = Singleton<GameManager>.Instance.kitchenController.Count;
			this.barrierData.unlockRemaining -= this.config.barrier[count / this.config.kitchen.barrierStep - 1].reduceProcessTime;
			if (this.barrierData.unlockRemaining < 0)
			{
				this.barrierData.unlockRemaining = 0;
			}
			GameUtilities.String.ToText(this.process.remainingText, GameUtilities.DateTime.Convert(this.barrierData.unlockRemaining));
			if (this.barrierData.unlockRemaining == 0)
			{
				base.StopCoroutine(this.processing);
				this.ProcessDone();
			}
			Tracking.instance.Ads_Impress("reward", "ReduceBarrierProcess");
		});
	}

	public void UnlockBarrierWithDiamond()
	{
		int count = Singleton<GameManager>.Instance.kitchenController.Count;
		float num = (float)this.barrierData.unlockRemaining / (float)this.config.barrier[count / this.config.kitchen.barrierStep - 1].unlockDuration;
		int diamond = (int)Math.Ceiling((double)((float)this.config.barrier[count / this.config.kitchen.barrierStep - 1].diamondToUnlock * num));
		if (Singleton<GameManager>.Instance.database.diamond < diamond)
		{
			Notification.instance.Warning("Not Enough Diamond");
			Singleton<SoundManager>.Instance.Play("Notification");
			return;
		}
		Notification.instance.Confirm(delegate
		{
			Singleton<GameManager>.Instance.SetDiamond(-diamond);
			this.ProcessDone();
		}, "Do you want to remove barrier for <color=#00B5FFFF>" + diamond.ToString() + "</color> diamond?");
	}

	public void UnlockBarrierFinal()
	{
		this.barrierData.state = BarrierState.Locked;
		this.unlocked.SetActive(false);
		this.Refresh();
		if (this.barrierData.nextBarrierFloor == this.config.kitchen.barrierStep * 2)
		{
			this.rating.Show(true);
		}
	}

	private void OnCashChange(double cash)
	{
		int count = Singleton<GameManager>.Instance.kitchenController.Count;
		if (this.general.barrierEnabled.activeInHierarchy)
		{
			double floorPrice = Singleton<GameProcess>.Instance.GetFloorPrice(count);
			this.enable.unlockCashButton.sprite = ((cash >= floorPrice) ? this.general.buttonEnableSprite : this.general.buttonDisableSprite);
		}
		else if (this.general.barrierDisable.activeInHierarchy)
		{
			if (this.barrierData.state != BarrierState.Locked)
			{
				return;
			}
			double num = Singleton<GameProcess>.Instance.GetFloorPrice(count) / (double)this.config.kitchen.unlockBarrierFactor;
			this.locked.unlockButtonImage.sprite = ((cash >= num) ? this.general.buttonEnableSprite : this.general.buttonDisableSprite);
		}
	}

	private void OnIdleCashChange(double idleCash)
	{
		if (!this.general.barrierEnabled.activeInHierarchy)
		{
			return;
		}
		this.enable.unlockButtonDiamond.SetActive(idleCash > 0.0);
		int count = Singleton<GameManager>.Instance.kitchenController.Count;
		int floorPrice = Singleton<GameProcess>.Instance.GetFloorPrice(count, idleCash);
		GameUtilities.String.ToText(this.enable.kitchenPriceDiamondText, floorPrice.ToString());
	}

	private void ProcessDone()
	{
		this.barrierData.nextBarrierFloor += this.config.kitchen.barrierStep;
		this.barrierData.state = BarrierState.Unlocked;
		this.processRunning = false;
		this.Refresh();
	}

	private IEnumerator Processing()
	{
		this.processRunning = true;
		int kitchenCount = Singleton<GameManager>.Instance.kitchenController.Count;
		int totalDuration = this.config.barrier[kitchenCount / this.config.kitchen.barrierStep - 1].unlockDuration;

		while (this.barrierData.unlockRemaining > 0)
		{
			float percent = (float)this.barrierData.unlockRemaining / (float)totalDuration;
			GameUtilities.String.ToText(this.process.remainingText, GameUtilities.DateTime.Convert(this.barrierData.unlockRemaining));
			GameUtilities.String.ToText(this.process.diamondText, ((int)Math.Ceiling((double)(this.config.barrier[kitchenCount / this.config.kitchen.barrierStep - 1].diamondToUnlock * percent))).ToString());
			this.process.processFillBar.fillAmount = percent;
			this.barrierData.unlockRemaining--;
			yield return this.waiting;
		}

		this.ProcessDone();
	}

	private void OfflineTimeCalculate()
	{
		if (this.barrierData.state != BarrierState.Process)
		{
			return;
		}
		int index = Singleton<DataManager>.Instance.database.targetRestaurant;
		int count = Singleton<GameManager>.Instance.kitchenController.Count;
		int num = GameUtilities.DateTime.Offline(Singleton<DataManager>.Instance.database.restaurant[index].dateTime);
		if (num > this.barrierData.unlockRemaining)
		{
			this.barrierData.unlockRemaining = 0;
		}
		else
		{
			this.barrierData.unlockRemaining -= num;
		}
	}

	private void OnApplicationPause(bool paused)
	{
		if (!paused && this.barrierData != null)
		{
			this.OfflineTimeCalculate();
		}
	}
}
