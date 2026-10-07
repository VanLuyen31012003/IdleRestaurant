using Firebase.Analytics;
using System;
using UnityEngine;

public class Tracking : MonoBehaviour
{
	public static Tracking instance;

	private bool first;

	private static bool analyticsBroken;

	private void Awake()
	{
		if (Tracking.instance == null)
		{
			Tracking.instance = this;
		}
		else if (Tracking.instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Start()
	{
		this.first = !PlayerPrefs.HasKey("first_session");
		if (this.first)
		{
			PlayerPrefs.SetInt("first_session", 1);
		}
	}

	// Firebase can fail to initialize on devices without Google Play Services (e.g. emulators).
	// Analytics must never break gameplay, so swallow the error and stop trying.
	private static void Log(string eventName, Func<Parameter[]> buildParameters)
	{
		if (Tracking.analyticsBroken)
		{
			return;
		}
		try
		{
			FirebaseAnalytics.LogEvent(eventName, buildParameters());
		}
		catch (Exception ex)
		{
			Tracking.analyticsBroken = true;
			UnityEngine.Debug.LogWarning("[Tracking] Firebase Analytics disabled: " + ex.Message);
		}
	}

	public void Tutorial_Start(string step)
	{
		Tracking.Log("tutorial", () => new Parameter[]
		{
			new Parameter("action_name", step),
			new Parameter("action_type", "game"),
			new Parameter("value", string.Empty)
		});
	}

	public void Tutorial_Done(string step)
	{
		Tracking.Log("tutorial", () => new Parameter[]
		{
			new Parameter("action_name", step),
			new Parameter("action_type", "user"),
			new Parameter("value", "completed")
		});
	}

	public void UI_Interaction(string position, string action)
	{
		Tracking.Log("ui_interaction", () => new Parameter[]
		{
			new Parameter("action_name", position),
			new Parameter("action_type", "user"),
			new Parameter("value", action),
			new Parameter("status_user_first_session", (!this.first) ? "0" : "1")
		});
	}

	public void Ads_Impress(string adsTYPE, string position)
	{
		Tracking.Log("ads_impress", () => new Parameter[]
		{
			new Parameter("action_name", adsTYPE),
			new Parameter("action_type", "game"),
			new Parameter("value", string.Empty),
			new Parameter("status_game_Ad_position", position)
		});
	}

	public void Ads_Status(string adsTYPE, string action, string position, string status)
	{
		Tracking.Log("ads_status", () => new Parameter[]
		{
			new Parameter("action_name", adsTYPE),
			new Parameter("action_type", action),
			new Parameter("status_game_Ad_position", position),
			new Parameter("status_ads", status)
		});
	}

	public void IAP(string product)
	{
		Tracking.Log("iap", () => new Parameter[]
		{
			new Parameter("action_name", product),
			new Parameter("action_type", "user"),
			new Parameter("value", "purchase")
		});
	}

	public void Rate_Show()
	{
		Tracking.Log("rate", () => new Parameter[]
		{
			new Parameter("action_name", "show"),
			new Parameter("action_type", "game"),
			new Parameter("value", string.Empty)
		});
	}

	public void Rate_Action(string action)
	{
		Tracking.Log("rate", () => new Parameter[]
		{
			new Parameter("action_name", "show"),
			new Parameter("action_type", "user"),
			new Parameter("value", action)
		});
	}
}
