using System;
using System.Collections;
using UnityEngine;

public class CustomerController : MonoBehaviour
{
	public enum CustomerState
	{
		Entering,
		WaitingForFood,
		Eating,
		Leaving
	}

	[Header("Settings")]
	public float walkSpeed = 80f;
	public float eatDuration = 1.0f;
	public float leaveDistance = 4.0f;

	public CustomerState currentState { get; private set; } = CustomerState.Entering;

	public bool isWaitingForFood => currentState == CustomerState.WaitingForFood;
	public bool isServed => currentState == CustomerState.Eating || currentState == CustomerState.Leaving;

	private Vector3 targetSeatPosition;
	private Action onSeatedCallback;
	private Action onLeftCallback;

	public void Initialize(Vector3 spawnPosition, Vector3 seatPosition, Action onSeatedCallback, Action onLeftCallback)
	{
		this.targetSeatPosition = seatPosition;
		this.onSeatedCallback = onSeatedCallback;
		this.onLeftCallback = onLeftCallback;

		transform.localPosition = spawnPosition;
		StartCoroutine(EnterRoutine());
	}

	public void Initialize(Vector3 seatPosition, Action onSeatedCallback, Action onLeftCallback)
	{
		Initialize(seatPosition + Vector3.left * 2.5f, seatPosition, onSeatedCallback, onLeftCallback);
	}

	private IEnumerator EnterRoutine()
	{
		currentState = CustomerState.Entering;
		while (Vector3.Distance(transform.localPosition, targetSeatPosition) > 0.05f)
		{
			transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetSeatPosition, Time.deltaTime * walkSpeed);
			yield return null;
		}
		transform.localPosition = targetSeatPosition;
		currentState = CustomerState.WaitingForFood;
		onSeatedCallback?.Invoke();
	}

	public void OnServed()
	{
		if (currentState == CustomerState.WaitingForFood)
		{
			StartCoroutine(EatAndLeaveRoutine());
		}
	}

	private IEnumerator EatAndLeaveRoutine()
	{
		currentState = CustomerState.Eating;
		yield return new WaitForSeconds(eatDuration);

		currentState = CustomerState.Leaving;
		Vector3 exitPosition = targetSeatPosition + Vector3.right * leaveDistance;

		while (Vector3.Distance(transform.localPosition, exitPosition) > 0.05f)
		{
			transform.localPosition = Vector3.MoveTowards(transform.localPosition, exitPosition, Time.deltaTime * walkSpeed);
			yield return null;
		}

		onLeftCallback?.Invoke();
		Destroy(gameObject);
	}
}
