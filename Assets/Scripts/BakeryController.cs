using System;
using System.Collections.Generic;
using UnityEngine;

public class BakeryController : BaseFloorController
{
	public GameObject transporterPrefab;

	public GameObject tutorial_2;

	public GameObject tutorial_5;

	private float distance;

	private List<TransporterController> transporterController;

	public override void Initialize()
	{
		this.managerController.managerAssign = this.StartWorking;
		Singleton<GameManager>.Instance.onCashChange += this.OnCashChange;
		this.transporterController = new List<TransporterController>();
		this.distance = Vector3.Distance(this.gatheringPoint.position, this.exploitedPoint.position);
		this.kitchenProperties = Singleton<GameProcess>.Instance.GetKitchenProperties(this.distance, this.kitchenData.floor, this.kitchenData.level, this.IncomeMultiplier);
		
		if (this.floorText != null) GameUtilities.String.ToText(this.floorText, (this.kitchenData.floor + 1).ToString());
		if (this.cashText != null) GameUtilities.String.ToText(this.cashText, GameUtilities.Currencies.Convert(this.kitchenData.cash));
		if (this.levelText != null) GameUtilities.String.ToText(this.levelText, "Level \n" + this.kitchenData.level.ToString());
		
		if (this.boostController != null) this.boostController.Refresh();
		this.SetTransporter(this.kitchenProperties.transporter);
		
		if (this.tutorial_2 != null) this.tutorial_2.SetActive(!GameManager.IsDoneTutorial(2));
		if (this.tutorial_5 != null) this.tutorial_5.SetActive(!GameManager.IsDoneTutorial(5) && GameManager.IsDoneTutorial(4));

		this.StartCustomerSpawning();
	}

	public override void ShowManagerProfile()
	{
		if (this.tutorial_5 != null && !GameManager.IsDoneTutorial(5) && GameManager.IsDoneTutorial(4))
		{
			GameManager.TutorialDone(5);
			this.tutorial_5.SetActive(false);
			Tracking.instance.Tutorial_Done("Step5");
		}
		Singleton<ManagerPopup>.Instance.Show(this);
	}

	public override void ShowKitchenProperties()
	{
		Singleton<KitchenPopup>.Instance.Show(this);
	}

	public override void SetCash(double cash)
	{
		this.kitchenData.cash += cash;
		if (this.cashText != null) GameUtilities.String.ToText(this.cashText, GameUtilities.Currencies.Convert(this.kitchenData.cash));
		if (!Singleton<DataManager>.Instance.database.tutorialCompleted.Contains(3))
		{
			Singleton<GameManager>.Instance.elevator.Tutorial_3();
		}
	}

	public override void BtnManager_OnClick()
	{
		ShowManagerProfile();
	}

	public override void BtnTransporter_OnClick()
	{
		StartWorking();
	}

	public override void StartWorking()
	{
		if (this.transporterController == null) return;
		if (this.CurrentCustomer == null || !this.CurrentCustomer.isWaitingForFood) return;

		for (int i = 0; i < this.transporterController.Count; i++)
		{
			if (this.transporterController[i] != null && this.transporterController[i].gameObject.activeInHierarchy && this.transporterController[i].isIdle)
			{
				this.transporterController[i].StartWorking();
			}
		}
		if (this.tutorial_2 != null && !GameManager.IsDoneTutorial(2))
		{
			GameManager.TutorialDone(2);
			this.tutorial_2.SetActive(false);
			Tracking.instance.Tutorial_Done("Step2");
		}
	}

	public override void Upgrade()
	{
		this.kitchenProperties = Singleton<GameProcess>.Instance.GetKitchenProperties(this.distance, this.kitchenData.floor, this.kitchenData.level, this.IncomeMultiplier);
		if (this.kitchenProperties.transporter > this.transporterController.Count)
		{
			this.SetTransporter(this.kitchenProperties.transporter);
		}
		if (this.levelText != null) GameUtilities.String.ToText(this.levelText, "Level \n" + this.kitchenData.level.ToString());
	}

	private void SetTransporter(int count)
	{
		if (this.transporterPrefab == null || this.kitchenGroup == null)
		{
			Debug.LogWarning($"[BakeryController] Không thể tạo Transporter! transporterPrefab: {(this.transporterPrefab == null ? "THIẾU (NULL)" : "OK")}, kitchenGroup: {(this.kitchenGroup == null ? "THIẾU (NULL)" : "OK")}");
			return;
		}
		Debug.Log($"[BakeryController] Đang sinh {count} Transporter...");
		for (int i = this.transporterController.Count; i < count; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.transporterPrefab, this.kitchenGroup);
			gameObject.transform.localScale = Vector3.one;
			gameObject.transform.position = this.gatheringPoint.position + Vector3.right * 0.2f * (float)i;
			TransporterController component = gameObject.GetComponentInChildren<TransporterController>();
			if (component != null)
			{
				this.transporterController.Add(component);
				component.Initialize(this);
				Debug.Log($"[BakeryController] Transporter #1 đã tạo thành công tại vị trí: {gameObject.transform.position}, active: {gameObject.activeInHierarchy}");
			}
			else
			{
				Debug.LogWarning("[BakeryController] Không tìm thấy TransporterController trên transporterPrefab!");
			}
		}
		if (this.managerController != null && this.managerController.hasManager)
		{
			this.StartWorking();
		}
	}

	private void OnCashChange(double cash)
	{
		if (this.levelUp == null || this.levelUp.Length < 3) return;
		int maxUpgradeLevel = Singleton<GameProcess>.Instance.GetMaxUpgradeLevel(cash, this.boostController.upgradeCostReduced, this.kitchenData.level, Location.Kitchen, this.kitchenData.floor);
		if (this.levelUp[0] != null) this.levelUp[0].SetActive(maxUpgradeLevel > 0);
		if (this.levelUp[1] != null) this.levelUp[1].SetActive(maxUpgradeLevel > 9);
		if (this.levelUp[2] != null) this.levelUp[2].SetActive(maxUpgradeLevel >= 50);
	}
}
