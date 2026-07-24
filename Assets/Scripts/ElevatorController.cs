using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ElevatorController : MonoBehaviour
{
	public Text cashText;

	public Text cabinText;

	public Text levelText;

	public Image processFill;

	public Transform cabinPoint;

	public Transform targetCabin;

	public GameObject productFX;

	public GameObject progress;

	public GameObject[] levelUp;

	public RectTransform productFill;

	public ElevatorPopup elevatorPopup;

	public BoostController boostController;

	public ManagerController managerController;

	public GameObject tutorial_3;

	public GameObject tutorial_4;

	[HideInInspector]
	public ElevatorData elevatorData;

	[HideInInspector]
	public ElevatorProperties elevatorProperties;

	[HideInInspector]
	public List<KitchenController> kitchenController;

	private int processCount;

	private bool transporting;

	private float movementSpeed;

	private float transferDuration;

	private double cashAfterTransfer;

	private double cashBeforeTransfer;

	private double totalCashTransport;

	public void Initialize()
	{
		this.managerController.managerAssign = this.StartTransport;
		Singleton<GameManager>.Instance.onCashChange += this.OnCashChange;
		float distance = 2f * (float)((this.kitchenController.Count <= 0) ? 1 : this.kitchenController.Count);
		this.elevatorProperties = Singleton<GameProcess>.Instance.GetElevatorProperties(distance, this.elevatorData.level);
		GameUtilities.String.ToText(this.cashText, GameUtilities.Currencies.Convert(this.elevatorData.cash));
		GameUtilities.String.ToText(this.levelText, "Level \n" + this.elevatorData.level.ToString());
		this.boostController.Refresh();
		this.FillProduct(0f);
		this.tutorial_3.SetActive(!GameManager.IsDoneTutorial(3) && GameManager.IsDoneTutorial(2));
		this.tutorial_4.SetActive(!GameManager.IsDoneTutorial(4) && GameManager.IsDoneTutorial(3));
	}

	public void ShowManagerProfile()
	{
		Singleton<ManagerPopup>.Instance.Show(this);
	}

	public void ShowElevatorProperties()
	{
		this.elevatorPopup.Show();
	}

	public void SetCash(double cash)
	{
		this.elevatorData.cash += cash;
		GameUtilities.String.ToText(this.cashText, GameUtilities.Currencies.Convert(this.elevatorData.cash));
		if (!GameManager.IsDoneTutorial(4) && cash > 0.0)
		{
			this.tutorial_4.SetActive(true);
			Tracking.instance.Tutorial_Start("Step4");
		}
	}

	public void Upgrade()
	{
		float distance = 2f * (float)((this.kitchenController.Count <= 0) ? 1 : this.kitchenController.Count);
		this.elevatorProperties = Singleton<GameProcess>.Instance.GetElevatorProperties(distance, this.elevatorData.level);
		this.levelText.text = "Level \n" + this.elevatorData.level.ToString();
	}

	public void StartTransport()
	{
		if (this.kitchenController.Count == 0 || this.transporting)
		{
			return;
		}
		this.transporting = true;
		base.StartCoroutine(this.Transporting());
		if (!GameManager.IsDoneTutorial(3) && GameManager.IsDoneTutorial(2))
		{
			GameManager.TutorialDone(3);
			this.tutorial_3.SetActive(false);
			Tracking.instance.Tutorial_Done("Step3");
		}
	}

	public void Tutorial_3()
	{
		this.tutorial_3.SetActive(true);
		Tracking.instance.Tutorial_Start("Step3");
	}

	public void Process(int value)
	{
		this.processCount += value;
		if (this.processCount < 0)
		{
			this.processCount = 0;
		}
		if (this.processCount > 0)
		{
			this.productFX.SetActive(true);
		}
		else
		{
			this.productFX.SetActive(false);
		}
	}

	private IEnumerator Transporting()
	{
		this.RefreshTransportData();
		double maxLoad = this.elevatorProperties.load * (double)this.boostController.loadExpansionBoost;
		double loadingSpeed = this.elevatorProperties.loadingSpeed * (double)this.boostController.loadingSpeedBoost;
		this.movementSpeed = this.elevatorProperties.movementSpeed * this.boostController.movementSpeedBoost;

		for (int i = 0; i < this.kitchenController.Count; i++)
		{
			// Move to kitchen floor
			while (this.targetCabin.position.y != this.kitchenController[i].transform.position.y)
			{
				Vector3 target = new Vector3(this.targetCabin.position.x, this.kitchenController[i].transform.position.y, this.targetCabin.position.z);
				this.targetCabin.position = Vector3.MoveTowards(this.targetCabin.position, target, Time.deltaTime * this.movementSpeed);
				yield return null;
			}

			// Load cash from kitchen
			this.cashBeforeTransfer = this.kitchenController[i].kitchenData.cash;
			if (this.cashBeforeTransfer == 0.0)
			{
				continue;
			}

			if (this.totalCashTransport + this.cashBeforeTransfer <= maxLoad)
			{
				this.transferDuration = (float)(this.cashBeforeTransfer / loadingSpeed);
			}
			else
			{
				this.transferDuration = (float)((maxLoad - this.totalCashTransport) / loadingSpeed);
			}

			float timing = 0f;
			this.progress.SetActive(true);
			while (timing < this.transferDuration)
			{
				this.processFill.fillAmount = timing / this.transferDuration;
				timing += Time.deltaTime;
				yield return null;
			}
			this.progress.SetActive(false);

			this.cashAfterTransfer = this.kitchenController[i].kitchenData.cash;
			if (this.totalCashTransport + this.cashAfterTransfer <= maxLoad)
			{
				this.kitchenController[i].SetCash(-this.cashAfterTransfer);
			}
			else
			{
				this.kitchenController[i].SetCash(-(maxLoad - this.totalCashTransport));
			}

			if (this.totalCashTransport + this.cashAfterTransfer > maxLoad)
			{
				this.totalCashTransport = maxLoad;
			}
			else
			{
				this.totalCashTransport += this.cashAfterTransfer;
			}

			this.FillProduct((float)(this.totalCashTransport / maxLoad));
			GameUtilities.String.ToText(this.cabinText, GameUtilities.Currencies.Convert(this.totalCashTransport));

			if (this.totalCashTransport == maxLoad)
			{
				break;
			}
		}

		// Move back to cabinPoint
		this.movementSpeed = this.elevatorProperties.movementSpeed * this.boostController.movementSpeedBoost;
		while (this.targetCabin.position != this.cabinPoint.position)
		{
			this.targetCabin.position = Vector3.MoveTowards(this.targetCabin.position, this.cabinPoint.position, Time.deltaTime * this.movementSpeed);
			yield return null;
		}

		// Unload cash at cabinPoint
		if (this.totalCashTransport > 0.0)
		{
			this.transferDuration = (float)(this.totalCashTransport / loadingSpeed);
			float timing = 0f;
			this.progress.SetActive(true);
			while (timing < this.transferDuration)
			{
				this.processFill.fillAmount = timing / this.transferDuration;
				timing += Time.deltaTime;
				yield return null;
			}
			this.progress.SetActive(false);
			this.SetCash(this.totalCashTransport);
			this.RefreshTransportData();
		}

		if (!this.managerController.hasManager)
		{
			this.transporting = false;
		}
		else
		{
			this.StartCoroutine(this.Transporting());
		}
	}

	private void RefreshTransportData()
	{
		this.cashAfterTransfer = 0.0;
		this.cashBeforeTransfer = 0.0;
		this.totalCashTransport = 0.0;
		this.FillProduct(0f);
		GameUtilities.String.ToText(this.cabinText, "0");
	}

	private void OnCashChange(double cash)
	{
		int maxUpgradeLevel = Singleton<GameProcess>.Instance.GetMaxUpgradeLevel(cash, this.boostController.upgradeCostReduced, this.elevatorData.level, Location.Elevator, 0);
		this.levelUp[0].SetActive(maxUpgradeLevel > 0);
		this.levelUp[1].SetActive(maxUpgradeLevel > 9);
		this.levelUp[2].SetActive(maxUpgradeLevel >= 50);
	}

	private void FillProduct(float value)
	{
		float num = 116f * value;
		if (num > 0f)
		{
			num = Mathf.Clamp(num, 30f, 116f);
		}
		this.productFill.sizeDelta = new Vector2(this.productFill.sizeDelta.x, num);
	}
}
