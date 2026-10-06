using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseFloorController : MonoBehaviour
{
	[SerializeField] protected Button btnTransporter;
	[SerializeField] protected Button btnManager;
	public Transform startPointCustomer;
	
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

	public GameObject customerPrefab;

	public CustomerController CurrentCustomer { get; protected set; }

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

	protected void StartCustomerSpawning()
	{
		StartCoroutine(SpawnCustomerRoutine());
	}

	protected System.Collections.IEnumerator SpawnCustomerRoutine()
	{
		while (true)
		{
			float waitTime = UnityEngine.Random.Range(2.0f, 5.0f);
			yield return new WaitForSeconds(waitTime);

			if (CurrentCustomer == null && exploitedPoint != null)
			{
				GameObject customerGo = null;
				if (customerPrefab != null)
				{
					customerGo = UnityEngine.Object.Instantiate(customerPrefab, kitchenGroup != null ? kitchenGroup : transform);
				}
				else
				{
					customerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
					customerGo.name = "Customer_Placeholder";
					if (kitchenGroup != null) customerGo.transform.SetParent(kitchenGroup, false);
					customerGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
				}

				CustomerController customer = customerGo.GetComponent<CustomerController>();
				if (customer == null)
				{
					customer = customerGo.AddComponent<CustomerController>();
				}

				CurrentCustomer = customer;
				Vector3 spawnPos = (this.startPointCustomer != null) ? this.startPointCustomer.localPosition : (this.exploitedPoint.localPosition + Vector3.left * 2.5f);

				customer.Initialize(
					spawnPos,
					this.exploitedPoint.localPosition,
					onSeatedCallback: () => {
						if (this.managerController != null && this.managerController.hasManager)
						{
							this.StartWorking();
						}
					},
					onLeftCallback: () => {
						if (CurrentCustomer == customer)
						{
							CurrentCustomer = null;
						}
					}
				);
			}
		}
	}

	public abstract void Initialize();

	public abstract void Upgrade();

	public abstract void SetCash(double cash);

	public abstract void ShowManagerProfile();

	public abstract void ShowKitchenProperties();
	
	public abstract void BtnManager_OnClick();
	public abstract void BtnTransporter_OnClick();
	public abstract void StartWorking();
}
