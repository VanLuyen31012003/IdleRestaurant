using System;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
	[NonSerialized]
	public Database database;

	[SerializeField]
	private Configuration configuration;

	protected override void Awake()
	{
		base.Awake();
		this.LoadDatabase();
	}

	private void LoadDatabase()
	{
		string text = "";
		if (!PlayerPrefs.HasKey(this.configuration.general.dataName))
		{
			text = JsonUtility.ToJson(new Database(this.configuration));
			PlayerPrefs.SetString(this.configuration.general.dataName, text);
		}
		text = PlayerPrefs.GetString(this.configuration.general.dataName);
		this.database = JsonUtility.FromJson<Database>(text);
		this.database.diamond = 10000;
		this.database.cash = 1000000000000;
	}

	private void SaveDatabase()
	{
		this.database.restaurant[this.database.targetRestaurant].dateTime = DateTime.Now.ToString();
		string value = JsonUtility.ToJson(this.database);
		PlayerPrefs.SetString(this.configuration.general.dataName, value);
	}

	private void OnApplicationQuit()
	{
		this.SaveDatabase();
	}

	private void OnApplicationPause(bool paused)
	{
		if (paused)
		{
			this.SaveDatabase();
		}
	}
}
