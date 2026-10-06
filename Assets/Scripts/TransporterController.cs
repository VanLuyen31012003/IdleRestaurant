using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class TransporterController : MonoBehaviour
{
	private SkeletonGraphic animator;

	private Vector3 gatheringPoint;

	private Vector3 exploitedPoint;

	private Vector3 restingPosition;

	private Transform myselfTransform;

	private Transform animatorTransform;

	private BaseFloorController kitchenController;

	private float cookingTime;

	private float walkingSpeed;

	public float movement;

	public bool isIdle = true;

	private void Awake()
	{
		this.myselfTransform = base.transform;
		this.animator = base.GetComponentInChildren<SkeletonGraphic>();
		this.animatorTransform = this.animator.transform;
	}

	public void Initialize(BaseFloorController kitchenController)
	{
		this.kitchenController = kitchenController;
		this.restingPosition = base.transform.localPosition;
		this.exploitedPoint = kitchenController.exploitedPoint.localPosition;
		this.gatheringPoint = kitchenController.gatheringPoint.localPosition;
	}

	public void StartWorking()
	{
		if (!this.isIdle || !gameObject.activeInHierarchy)
		{
			return;
		}
		if (this.kitchenController != null && (this.kitchenController.CurrentCustomer == null || !this.kitchenController.CurrentCustomer.isWaitingForFood))
		{
			return;
		}
		base.StartCoroutine(this.Working());
	}

	private IEnumerator Working()
	{
		this.isIdle = false;
		this.ApplyAnimationSpeed("Run_01", this.kitchenController.boostController.walkingSpeedBoost);
		
		while (this.myselfTransform.localPosition != this.exploitedPoint)
		{
			this.myselfTransform.localPosition = Vector3.MoveTowards(this.myselfTransform.localPosition, this.exploitedPoint, Time.deltaTime * this.walkingSpeed);
			yield return null;
		}
		
		this.ApplyAnimationSpeed("Idle_02", this.kitchenController.boostController.cookingSpeedBoost);
		yield return new WaitForSeconds(this.cookingTime);
		
		if (this.kitchenController != null && this.kitchenController.CurrentCustomer != null)
		{
			this.kitchenController.CurrentCustomer.OnServed();
		}

		this.animatorTransform.eulerAngles += Vector3.up * 180f;
		this.ApplyAnimationSpeed("Run_02", this.kitchenController.boostController.walkingSpeedBoost);

		while (this.myselfTransform.localPosition != this.gatheringPoint)
		{
			this.myselfTransform.localPosition = Vector3.MoveTowards(this.myselfTransform.localPosition, this.gatheringPoint, Time.deltaTime * this.walkingSpeed);
			yield return null;
		}

		this.animatorTransform.eulerAngles += Vector3.up * 180f;
		this.kitchenController.SetCash(this.kitchenController.kitchenProperties.transporterCapacity);
		this.ApplyAnimationSpeed("Run_01", this.kitchenController.boostController.walkingSpeedBoost);

		while (this.myselfTransform.localPosition != this.restingPosition)
		{
			this.myselfTransform.localPosition = Vector3.MoveTowards(this.myselfTransform.localPosition, this.restingPosition, Time.deltaTime * this.walkingSpeed);
			yield return null;
		}

		if (!this.kitchenController.managerController.hasManager)
		{
			this.ApplyAnimationSpeed("Idle_01", 1f);
			this.isIdle = true;
		}
		else
		{
			if (this.kitchenController != null && (this.kitchenController.CurrentCustomer == null || !this.kitchenController.CurrentCustomer.isWaitingForFood))
			{
				this.ApplyAnimationSpeed("Idle_01", 1f);
				this.isIdle = true;
			}
			else
			{
				this.StartCoroutine(this.Working());
			}
		}
	}

	private void ApplyAnimationSpeed(string clip, float speed = 1f)
	{
		if (clip != null)
		{
			if (!(clip == "Run_01") && !(clip == "Run_02"))
			{
				if (clip == "Idle_02")
				{
					this.cookingTime = (float)(this.kitchenController.kitchenProperties.transporterCapacity / (this.kitchenController.kitchenProperties.workingSpeed * (double)speed));
				}
			}
			else
			{
				this.walkingSpeed = this.kitchenController.kitchenProperties.walkingSpeed * speed * this.movement;
			}
		}
		if (this.animator != null)
		{
			this.animator.timeScale = speed;
			if (this.animator.AnimationState != null)
			{
				this.animator.AnimationState.SetAnimation(0, clip, true);
			}
		}
	}
}
