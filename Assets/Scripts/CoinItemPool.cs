using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CoinItemPool : MonoBehaviour
{
	private WaitForSeconds waitForSeconds;

	[SerializeField]
	private int itemCount;

	[SerializeField]
	private float randomRadius;

	[SerializeField]
	private float spawnDeltaTime;

	[SerializeField]
	private Transform coinParent;

	[SerializeField]
	private GameObject coinPrefab;

	private void Start()
	{
		this.waitForSeconds = new WaitForSeconds(this.spawnDeltaTime);
	}

	public void Pool(Transform target, double cash)
	{
		Singleton<SoundManager>.Instance.Play("Collect");
		base.StartCoroutine(this.Spawn(target, cash));
	}

	private IEnumerator Spawn(Transform target, double cash)
	{
		int count = this.itemCount;
		while (count > 0)
		{
			float x = UnityEngine.Random.Range(this.transform.position.x - this.randomRadius, this.transform.position.x + this.randomRadius);
			float y = UnityEngine.Random.Range(this.transform.position.y - this.randomRadius, this.transform.position.y + this.randomRadius);
			GameObject coinItem = ObjectPool.Spawn(this.coinPrefab, this.transform.position, Quaternion.identity);
			coinItem.transform.SetParent(this.coinParent);
			coinItem.transform.localScale = Vector3.one;
			coinItem.GetComponent<CoinItem>().Init(new Vector3[]
			{
				new Vector3(x, y, 0f),
				target.position
			}, Math.Round(cash / (double)this.itemCount));
			count--;
			yield return this.waitForSeconds;
		}
	}
}
