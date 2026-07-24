using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class WaiterController : MonoBehaviour
{
	private SkeletonGraphic animator;

	private Vector3 gatheringPoint;

	private Vector3 exploitedPoint;

	private Vector3 restingPosition;

	private Transform myselfTransform;

	private Transform animatorTransform;

	private ElevatorController elevatorController;

	private RestaurantController restaurantController;

	private float loadingTime;

	private float walkingSpeed;

	private double totalCashTransfer;

	private double cashAfterTransfer;

	private double cashBeforeTransfer;

	public bool isIdle = true;

	public Text cashText;

	public float movement;

	private void Awake()
	{
		this.myselfTransform = base.transform;
		this.animator = base.GetComponentInChildren<SkeletonGraphic>();
		this.animatorTransform = this.animator.transform;
	}

	public void Initialize(ElevatorController elevatorController, RestaurantController restaurantController)
	{
		this.elevatorController = elevatorController;
		this.restaurantController = restaurantController;
		this.restingPosition = base.transform.localPosition;
		this.gatheringPoint = restaurantController.gatheringPoint.localPosition;
		this.exploitedPoint = restaurantController.exploitedPoint.localPosition;
	}

	public void StartTransport()
	{
		if (!this.isIdle)
		{
			return;
		}
		base.StartCoroutine(this.Transport());
	}

	private IEnumerator Transport()
	{
		this.isIdle = false;
		this.ApplyAnimationSpeed("Run_01", this.restaurantController.boostController.walkingSpeedBoost);
		this.totalCashTransfer = 0.0;

		while (this.myselfTransform.localPosition != this.exploitedPoint)
		{
			this.myselfTransform.localPosition = Vector3.MoveTowards(this.myselfTransform.localPosition, this.exploitedPoint, Time.deltaTime * this.walkingSpeed);
			yield return null;
		}

		this.cashBeforeTransfer = this.elevatorController.elevatorData.cash;
		if (this.cashBeforeTransfer > 0.0)
		{
			this.elevatorController.Process(1);
			this.ApplyAnimationSpeed("Idle_02", 1f);
			this.loadingTime = ((this.cashBeforeTransfer > this.restaurantController.restaurantProperties.loadPerWaiter * (double)this.restaurantController.boostController.loadExpansionBoost) ? ((float)(this.restaurantController.restaurantProperties.loadPerWaiter * (double)this.restaurantController.boostController.loadExpansionBoost / (this.restaurantController.restaurantProperties.loadingSpeed * (double)this.restaurantController.boostController.loadingSpeedBoost))) : ((float)(this.cashBeforeTransfer / (this.restaurantController.restaurantProperties.loadingSpeed * (double)this.restaurantController.boostController.loadingSpeedBoost))));
			yield return new WaitForSeconds(this.loadingTime);

			this.cashAfterTransfer = this.elevatorController.elevatorData.cash;
			if (this.cashAfterTransfer > 0.0)
			{
				this.totalCashTransfer = ((this.cashAfterTransfer <= this.restaurantController.restaurantProperties.loadPerWaiter * (double)this.restaurantController.boostController.loadExpansionBoost) ? this.cashAfterTransfer : (this.restaurantController.restaurantProperties.loadPerWaiter * (double)this.restaurantController.boostController.loadExpansionBoost));
				this.elevatorController.SetCash(-this.totalCashTransfer);
				this.cashText.gameObject.SetActive(true);
				GameUtilities.String.ToText(this.cashText, GameUtilities.Currencies.Convert(this.totalCashTransfer));
			}
			this.elevatorController.Process(-1);
		}

		this.animatorTransform.eulerAngles += Vector3.up * 180f;
		this.ApplyAnimationSpeed((this.totalCashTransfer <= 0.0) ? "Run_01" : "Run_02", this.restaurantController.boostController.walkingSpeedBoost);

		while (this.myselfTransform.localPosition != this.gatheringPoint)
		{
			this.myselfTransform.localPosition = Vector3.MoveTowards(this.myselfTransform.localPosition, this.gatheringPoint, Time.deltaTime * this.walkingSpeed);
			yield return null;
		}

		if (this.totalCashTransfer > 0.0)
		{
			this.ApplyAnimationSpeed("Idle_02", 1f);
			yield return new WaitForSeconds((float)(this.totalCashTransfer / (this.restaurantController.restaurantProperties.loadingSpeed * (double)this.restaurantController.boostController.loadingSpeedBoost)));

			this.restaurantController.SetCash(this.totalCashTransfer);
			GameUtilities.String.ToText(this.cashText, string.Empty);
			this.cashText.gameObject.SetActive(false);
		}

		this.animatorTransform.eulerAngles += Vector3.up * 180f;
		this.ApplyAnimationSpeed("Run_01", this.restaurantController.boostController.walkingSpeedBoost);

		while (this.myselfTransform.localPosition != this.restingPosition)
		{
			this.myselfTransform.localPosition = Vector3.MoveTowards(this.myselfTransform.localPosition, this.restingPosition, Time.deltaTime * this.walkingSpeed);
			yield return null;
		}

		if (!this.restaurantController.managerController.hasManager)
		{
			this.animator.AnimationState.SetAnimation(0, "Idle_01", true);
			this.isIdle = true;
		}
		else
		{
			this.StartCoroutine(this.Transport());
		}
	}

	private void ApplyAnimationSpeed(string clip, float speed = 1f)
	{
		if (clip.Equals("Run_01") || clip.Equals("Run_02"))
		{
			this.walkingSpeed = this.restaurantController.restaurantProperties.walkingSpeed * speed * this.movement;
		}
		this.animator.timeScale = speed;
		this.animator.AnimationState.SetAnimation(0, clip, true);
	}
}
