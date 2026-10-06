using System;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T instance;

	private static bool onApplicationQuitting;

	private static object deadLock = new object();

	public static T Instance
	{
		get
		{
			object obj = Singleton<T>.deadLock;
			lock (obj)
			{
				if (Singleton<T>.instance == null)
				{
					Singleton<T>.instance = UnityEngine.Object.FindObjectOfType<T>();
				}
				if (Singleton<T>.instance == null && !onApplicationQuitting)
				{
					GameObject gameObject = new GameObject(typeof(T).ToString());
					Singleton<T>.instance = gameObject.AddComponent<T>();
				}
			}
			return Singleton<T>.instance;
		}
	}

	protected virtual void Awake()
	{
		if (Singleton<T>.instance == null)
		{
			Singleton<T>.instance = this as T;
		}
		else if (Singleton<T>.instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	protected virtual void OnApplicationQuit()
	{
		onApplicationQuitting = true;
	}

	protected virtual void OnDestroy()
	{
		if (Singleton<T>.instance == this)
		{
			Singleton<T>.instance = null;
		}
	}
}
