using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseFloorController : MonoBehaviour
{
	[SerializeField] protected Button btnTransporter;
	[SerializeField] protected Button btnManager;
	
	public Text cashText;

	public Text levelText;

	public Text floorText;

	public GameObject[] levelUp;

	public Transform kitchenGroup;

	public Transform exploitedPoint;

	public Transform gatheringPoint;

	public BoostController boostController;

	public ManagerController managerController;

	[NonSerialized]
	public KitchenData kitchenData;

	[NonSerialized]
	public KitchenProperties kitchenProperties;

	public float[] floorIncomeMultipliers = new float[] { 1.0f, 1.25f, 1.5f };

	protected virtual void Awake()
	{
		if (btnTransporter) btnTransporter.onClick.AddListener(BtnTransporter_OnClick);
		if (btnManager) btnManager.onClick.AddListener(BtnManager_OnClick);
	}
	
	public virtual float IncomeMultiplier
	{
		get
		{
			if (this.floorIncomeMultipliers != null && this.kitchenData != null && this.kitchenData.floorType >= 0 && this.kitchenData.floorType < this.floorIncomeMultipliers.Length)
			{
				return this.floorIncomeMultipliers[this.kitchenData.floorType];
			}
			return 1f;
		}
	}

	public abstract void Initialize();

	public abstract void Upgrade();

	public abstract void SetCash(double cash);

	public abstract void ShowManagerProfile();

	public abstract void ShowKitchenProperties();
	
	public abstract void BtnManager_OnClick();
	public abstract void BtnTransporter_OnClick();

}
