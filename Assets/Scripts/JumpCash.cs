using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class JumpCash : MonoBehaviour
{
	[SerializeField]
	private Text cashText;

	[SerializeField]
	private Animation anim;

	public void Init(double cash)
	{
		GameUtilities.String.ToText(this.cashText, GameUtilities.Currencies.Convert(cash));
		base.StartCoroutine(this.DestroySelf(this.anim.clip.length));
	}

	private IEnumerator DestroySelf(float time)
	{
		yield return new WaitForSeconds(time);
		ObjectPool.Despawn(this.gameObject);
	}
}
