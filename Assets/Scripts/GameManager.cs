using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
	[SerializeField]
	private Text idleCashText;

	[SerializeField]
	private Text mainScreenCash;

	[SerializeField]
	private Text shopScreenCash;

	[SerializeField]
	private Text mainScreenDiamond;

	[SerializeField]
	private Text shopScreenDiamond;

	[SerializeField]
	private GameObject kitchenPrefab;

	[SerializeField]
	private GameObject[] floorPrefabs;

	[SerializeField]
	private Transform kitchenContent;

	[SerializeField]
	private BarrierController barrier;

	[SerializeField]
	private BoostManager boostManager;

	[SerializeField]
	private Configuration configuration;

	private const float floorDistance = 2f;

	public Database database;

	[HideInInspector]
	public List<BaseFloorController> kitchenController;

	public ElevatorController elevator;

	public RestaurantController restaurant;

	public Action<double> onCashChange;

	public Action<double> onIdleCashChange;

	private List<string> debugLogs = new List<string>();

	public void AddDebugLog(string msg)
	{
		if (debugLogs == null) debugLogs = new List<string>();
		debugLogs.Add(msg);
		if (debugLogs.Count > 10) debugLogs.RemoveAt(0);
	}

	private void OnGUI()
	{
		GUI.color = Color.yellow;
		GUI.skin.label.fontSize = 24;
		GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, 350));
		GUILayout.Label("=== DEBUG FLOORS INFO ===");
		if (kitchenController != null)
		{
			GUILayout.Label($"So Tang hien tai: {kitchenController.Count}");
			for (int i = 0; i < kitchenController.Count; i++)
			{
				if (kitchenController[i] != null)
				{
					int fType = (kitchenController[i].kitchenData != null) ? kitchenController[i].kitchenData.floorType : -1;
					GUILayout.Label($"Tang {i + 1}: Name={kitchenController[i].gameObject.name}, Script={kitchenController[i].GetType().Name}, floorType={fType}");
				}
			}
		}
		if (debugLogs != null)
		{
			for (int i = 0; i < debugLogs.Count; i++)
			{
				GUILayout.Label(debugLogs[i]);
			}
		}
		GUILayout.EndArea();
	}

    protected override void Awake()
    {
        base.Awake();
        Application.targetFrameRate = 60;
    }

    private void Start()
	{
		this.database = Singleton<DataManager>.Instance.database;
		GameUtilities.String.ToText(this.mainScreenDiamond, this.database.diamond.ToString());
		GameUtilities.String.ToText(this.shopScreenDiamond, this.database.diamond.ToString());
		GameUtilities.String.ToText(this.mainScreenCash, GameUtilities.Currencies.Convert(this.database.cash));
		GameUtilities.String.ToText(this.shopScreenCash, GameUtilities.Currencies.Convert(this.database.cash));
		GameUtilities.String.ToText(this.idleCashText, GameUtilities.Currencies.Convert(this.database.restaurant[this.database.targetRestaurant].idleCash) + "/s");
		this.InitKitchen();
		this.InitElevator();
		this.InitRestaurant();
		this.InitManager();
		this.barrier.Initialize();
		this.boostManager.Initialize();
		this.onCashChange(this.database.cash);
        //Singleton<GameManager>.Instance.SetDiamond(10000000);
    }

	public GameObject GetFloorPrefab(int floorType)
	{
		Type[] expectedTypes = new Type[] { typeof(KitchenController), typeof(CafeController), typeof(BakeryController) };
		string[] prefabNames = new string[] { "Kitchen", "Cafe", "Bakery" };

		if (this.floorPrefabs != null && floorType >= 0 && floorType < this.floorPrefabs.Length && this.floorPrefabs[floorType] != null)
		{
			if (floorType < expectedTypes.Length && this.floorPrefabs[floorType].GetComponentInChildren(expectedTypes[floorType]) != null)
			{
				return this.floorPrefabs[floorType];
			}
		}

		if (floorType >= 0 && floorType < prefabNames.Length)
		{
			GameObject resPrefab = Resources.Load<GameObject>("Prefab/" + prefabNames[floorType]);
			if (resPrefab != null)
			{
				return resPrefab;
			}
		}

		if (this.floorPrefabs != null && floorType >= 0 && floorType < this.floorPrefabs.Length && this.floorPrefabs[floorType] != null)
		{
			return this.floorPrefabs[floorType];
		}

		return this.kitchenPrefab;
	}

	private void InitKitchen()
	{
		this.kitchenController = new List<BaseFloorController>();
		for (int i = 0; i < this.database.restaurant[this.database.targetRestaurant].kitchen.Count; i++)
		{
			KitchenData data = this.database.restaurant[this.database.targetRestaurant].kitchen[i];
			GameObject prefab = this.GetFloorPrefab(data.floorType);
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, this.kitchenContent);
			gameObject.transform.SetSiblingIndex(1);
			BaseFloorController component = gameObject.GetComponentInChildren<BaseFloorController>();
			if (component != null)
			{
				component.kitchenData = data;
				component.Initialize();
				this.kitchenController.Add(component);
			}
			else
			{
				Debug.LogWarning($"[GameManager] Clear Floor #{i}: Cannot find BaseFloorController in prefab {prefab?.name}!");
			}
		}
	}

	private void InitElevator()
	{
		this.elevator.elevatorData = this.database.restaurant[this.database.targetRestaurant].elevator;
		this.elevator.kitchenController = this.kitchenController;
		this.elevator.Initialize();
	}

	private void InitRestaurant()
	{
		this.restaurant.restaurantData = this.database.restaurant[this.database.targetRestaurant].restaurant;
		this.restaurant.Initialize();
	}

	private ManagerProfile CreateDefaultKitchenManager(int floor)
	{
		ManagerProfile managerProfile = new ManagerProfile
		{
			assign = true,
			location = Location.Kitchen,
			kitchenFloor = floor,
			experience = Experience.Junior,
			state = ManagerState.Ready,
			skill = ManagerSkill.CookingSpeed,
			price = 0
		};
		this.database.restaurant[this.database.targetRestaurant].profile.Add(managerProfile);
		return managerProfile;
	}

	private void InitManager()
	{
		for (int i = 0; i < this.database.restaurant[this.database.targetRestaurant].profile.Count; i++)
		{
			if (this.database.restaurant[this.database.targetRestaurant].profile[i].assign)
			{
				Location location = this.database.restaurant[this.database.targetRestaurant].profile[i].location;
				if (location != Location.Elevator)
				{
					if (location != Location.Restaurant)
					{
						if (location == Location.Kitchen)
						{
							int kitchenFloor = this.database.restaurant[this.database.targetRestaurant].profile[i].kitchenFloor;
							if (kitchenFloor >= 0 && kitchenFloor < this.kitchenController.Count)
							{
								this.kitchenController[kitchenFloor].managerController.managerProfile = this.database.restaurant[this.database.targetRestaurant].profile[i];
								this.kitchenController[kitchenFloor].managerController.ManagerAssign();
							}
						}
					}
					else
					{
						this.restaurant.managerController.managerProfile = this.database.restaurant[this.database.targetRestaurant].profile[i];
						this.restaurant.managerController.ManagerAssign();
					}
				}
				else
				{
					this.elevator.managerController.managerProfile = this.database.restaurant[this.database.targetRestaurant].profile[i];
					this.elevator.managerController.ManagerAssign();
				}
			}
		}

		for (int j = 0; j < this.kitchenController.Count; j++)
		{
			if (!this.kitchenController[j].managerController.hasManager)
			{
				ManagerProfile defaultProfile = this.CreateDefaultKitchenManager(j);
				this.kitchenController[j].managerController.managerProfile = defaultProfile;
				this.kitchenController[j].managerController.ManagerAssign();
			}
		}
	}

	public void IdleCashCompute()
	{
		if (this.elevator.managerController.hasManager && this.restaurant.managerController.hasManager)
		{
			double num = 0.0;
			for (int i = 0; i < this.kitchenController.Count; i++)
			{
				if (this.kitchenController[i].managerController.hasManager)
				{
					num += this.kitchenController[i].kitchenProperties.totalExtraction;
				}
			}
			double num2 = num;
			if (num2 > this.elevator.elevatorProperties.transportation)
			{
				num2 = this.elevator.elevatorProperties.transportation;
			}
			if (num2 > this.restaurant.restaurantProperties.transportation)
			{
				num2 = this.restaurant.restaurantProperties.transportation;
			}
			this.database.restaurant[this.database.targetRestaurant].idleCash = Math.Round(num2 / 100.0 * (double)this.configuration.general.idleCashRate);
		}
		else
		{
			this.database.restaurant[this.database.targetRestaurant].idleCash = 0.0;
		}
		this.onIdleCashChange(this.database.restaurant[this.database.targetRestaurant].idleCash);
		GameUtilities.String.ToText(this.idleCashText, GameUtilities.Currencies.Convert(this.database.restaurant[this.database.targetRestaurant].idleCash) + "/s");
	}

	public void UnlockKitchen()
	{
		int currentFloorCount = this.kitchenController.Count;
		int totalTypes = (this.floorPrefabs != null && this.floorPrefabs.Length > 0) ? this.floorPrefabs.Length : 3;
		int typeIndex = currentFloorCount % totalTypes;
		GameObject prefab = this.GetFloorPrefab(typeIndex);
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab, this.kitchenContent);
		gameObject.transform.SetSiblingIndex(1);
		BaseFloorController component = gameObject.GetComponentInChildren<BaseFloorController>();
		if (component == null)
		{
			Debug.LogWarning($"[GameManager] UnlockKitchen: Cannot find BaseFloorController in prefab {prefab?.name}!");
			return;
		}
		KitchenData kitchenData = new KitchenData();
		kitchenData.floor = currentFloorCount;
		kitchenData.level = 1;
		kitchenData.floorType = typeIndex;
		component.kitchenData = kitchenData;
		component.Initialize();
		this.database.restaurant[this.database.targetRestaurant].kitchen.Add(kitchenData);
		this.kitchenController.Add(component);

		AddDebugLog($"[UNLOCK OK] Tang #{currentFloorCount + 1}, typeIndex={typeIndex}, Prefab={prefab?.name}, Script={component?.GetType()?.Name}");

		ManagerProfile defaultProfile = this.CreateDefaultKitchenManager(currentFloorCount);
		component.managerController.managerProfile = defaultProfile;
		component.managerController.ManagerAssign();

		this.barrier.Refresh();
		if (this.kitchenController.Count == 1 && this.elevator.managerController.hasManager)
		{
			this.elevator.StartTransport();
		}
	}

	public void SetCash(double cash)
	{
		this.database.cash += cash;
		GameUtilities.String.ToText(this.mainScreenCash, GameUtilities.Currencies.Convert(this.database.cash));
		GameUtilities.String.ToText(this.shopScreenCash, GameUtilities.Currencies.Convert(this.database.cash));
		this.onCashChange(this.database.cash);
	}

	public void SetDiamond(int diamond)
	{
		this.database.diamond += diamond;
		GameUtilities.String.ToText(this.mainScreenDiamond, this.database.diamond.ToString());
		GameUtilities.String.ToText(this.shopScreenDiamond, this.database.diamond.ToString());
	}

	public static bool IsDoneTutorial(int index)
	{
		return Singleton<DataManager>.Instance.database.tutorialCompleted.Contains(index);
	}

	public static void TutorialDone(int index)
	{
		Singleton<DataManager>.Instance.database.tutorialCompleted.Add(index);
	}
}
