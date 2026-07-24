using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class RandomChef : MonoBehaviour
{
	private int lastIndex;

	private SkeletonGraphic animator;

	private WaitForSeconds waitForSeconds;

	[SerializeField]
	private int changeTime;

	private string[] animations = new string[]
	{
		"Making_01",
		"Making_02",
		"Making_03",
		"Making_04"
	};

	private void Start()
	{
		this.animator = base.GetComponent<SkeletonGraphic>();
		this.waitForSeconds = new WaitForSeconds((float)this.changeTime);
		base.StartCoroutine(this.PlayAnimation());
	}

	public void SetSpeed(int value)
	{
		this.animator.timeScale = (float)value;
	}

	private IEnumerator PlayAnimation()
	{
		int randomId;
		do
		{
			randomId = UnityEngine.Random.Range(0, this.animations.Length);
		}
		while (randomId == this.lastIndex);
		this.lastIndex = randomId;
		this.animator.AnimationState.SetAnimation(0, this.animations[randomId], true);
		yield return this.waitForSeconds;
		this.StartCoroutine(this.PlayAnimation());
	}
}
