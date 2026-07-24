using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class ManagerController : MonoBehaviour
{
	public Text timeText;

	public Image skillSprite;

	public GameObject activeButton;

	public GameObject emptyManager;

	public GameObject targetManager;

	public GameObject boostEffect;

	public BoostController boostController;

	public SkeletonGraphic skeletonGraphic;

	[HideInInspector]
	public ManagerProfile managerProfile;

	public bool hasManager;

	public Action managerAssign;

	private Coroutine cooldown;

	private Coroutine boosting;

	private WaitForSeconds waitForSeconds = new WaitForSeconds(1f);

	public void ManagerAssign()
	{
		this.hasManager = true;
		this.emptyManager.SetActive(false);
		this.targetManager.SetActive(true);
		Experience experience = this.managerProfile.experience;
		if (experience != Experience.Junior)
		{
			if (experience != Experience.Senior)
			{
				if (experience == Experience.Expert)
				{
					this.skeletonGraphic.Skeleton.SetSkin("Manager03");
				}
			}
			else
			{
				this.skeletonGraphic.Skeleton.SetSkin("Manager02");
			}
		}
		else
		{
			this.skeletonGraphic.Skeleton.SetSkin("Manager01");
		}
		this.skeletonGraphic.Skeleton.SetToSetupPose();
		this.skillSprite.sprite = Singleton<GameProcess>.Instance.GetManagerSkillSprite(this.managerProfile.skill, true);
		int num = GameUtilities.DateTime.Offline(this.managerProfile.lastActive);
		if (this.managerProfile.state == ManagerState.Cooldown && this.managerProfile.remainingTime > 0)
		{
			int managerSkillCooldown = Singleton<GameProcess>.Instance.GetManagerSkillCooldown(this.managerProfile.experience, this.managerProfile.skill);
			this.managerProfile.remainingTime = managerSkillCooldown - num;
			if (this.managerProfile.remainingTime <= 0)
			{
				this.managerProfile.remainingTime = 0;
				this.managerProfile.state = ManagerState.Ready;
			}
			else
			{
				this.cooldown = base.StartCoroutine(this.Cooldown());
			}
		}
		if (this.managerProfile.state == ManagerState.Boosting && this.managerProfile.remainingTime > 0)
		{
			int managerSkillDuration = Singleton<GameProcess>.Instance.GetManagerSkillDuration(this.managerProfile.experience, this.managerProfile.skill);
			this.managerProfile.remainingTime = managerSkillDuration - num;
			if (this.managerProfile.remainingTime <= 0)
			{
				this.managerProfile.remainingTime = Singleton<GameProcess>.Instance.GetManagerSkillCooldown(this.managerProfile.experience, this.managerProfile.skill);
				this.managerProfile.state = ManagerState.Cooldown;
				this.managerProfile.remainingTime += managerSkillDuration - num;
				if (this.managerProfile.remainingTime <= 0)
				{
					this.managerProfile.remainingTime = 0;
					this.managerProfile.state = ManagerState.Ready;
				}
				else
				{
					this.cooldown = base.StartCoroutine(this.Cooldown());
				}
			}
			else
			{
				this.ActiveSkill();
				this.boosting = base.StartCoroutine(this.Boosting());
			}
		}
		this.activeButton.SetActive(this.managerProfile.state == ManagerState.Ready);
		if (this.managerAssign != null)
		{
			this.managerAssign();
		}
	}

	public void ManagerUnassign()
	{
		this.hasManager = false;
		this.boostController.Refresh();
		this.emptyManager.SetActive(true);
		this.boostEffect.SetActive(false);
		this.targetManager.SetActive(false);
		if (this.cooldown != null)
		{
			base.StopCoroutine(this.cooldown);
		}
		if (this.boosting != null)
		{
			base.StopCoroutine(this.boosting);
		}
		if (this.managerProfile.state == ManagerState.Boosting)
		{
			this.managerProfile.state = ManagerState.Cooldown;
			this.managerProfile.lastActive = DateTime.Now.ToString();
			this.managerProfile.remainingTime = Singleton<GameProcess>.Instance.GetManagerSkillCooldown(this.managerProfile.experience, this.managerProfile.skill);
		}
		if (this.managerProfile.skill == ManagerSkill.UpgradeCost)
		{
			Singleton<GameManager>.Instance.onCashChange(Singleton<GameManager>.Instance.database.cash);
		}
		GameUtilities.String.ToText(this.timeText, string.Empty);
		this.managerProfile = null;
	}

	public void ManagerActivate()
	{
		this.activeButton.SetActive(false);
		this.managerProfile.state = ManagerState.Boosting;
		this.managerProfile.lastActive = DateTime.Now.ToString();
		this.managerProfile.remainingTime = Singleton<GameProcess>.Instance.GetManagerSkillDuration(this.managerProfile.experience, this.managerProfile.skill);
		this.ActiveSkill();
		Singleton<SoundManager>.Instance.Play("Boost");
		this.boosting = base.StartCoroutine(this.Boosting());
	}

	private void ActiveSkill()
	{
		int managerSkillEffective = Singleton<GameProcess>.Instance.GetManagerSkillEffective(this.managerProfile.experience, this.managerProfile.skill);
		switch (this.managerProfile.skill)
		{
		case ManagerSkill.UpgradeCost:
			this.boostController.upgradeCostReduced = managerSkillEffective;
			Singleton<GameManager>.Instance.onCashChange(Singleton<GameManager>.Instance.database.cash);
			break;
		case ManagerSkill.WalkingSpeed:
			this.boostController.walkingSpeedBoost = (float)managerSkillEffective;
			break;
		case ManagerSkill.CookingSpeed:
			this.boostController.cookingSpeedBoost = (float)managerSkillEffective;
			break;
		case ManagerSkill.LoadingSpeed:
			this.boostController.loadingSpeedBoost = (float)managerSkillEffective;
			break;
		case ManagerSkill.MovementSpeed:
			this.boostController.movementSpeedBoost = (float)managerSkillEffective;
			break;
		case ManagerSkill.LoadExpansion:
			this.boostController.loadExpansionBoost = (float)managerSkillEffective;
			break;
		}
		if (this.managerProfile.skill == ManagerSkill.UpgradeCost)
		{
			Singleton<GameManager>.Instance.onCashChange(Singleton<GameManager>.Instance.database.cash);
		}
	}

	private IEnumerator Cooldown()
	{
		this.boostEffect.SetActive(false);
		while (this.managerProfile.remainingTime > 0)
		{
			GameUtilities.String.ToText(this.timeText, GameUtilities.DateTime.Convert(this.managerProfile.remainingTime));
			yield return this.waitForSeconds;
			this.managerProfile.remainingTime--;
		}
		this.activeButton.SetActive(true);
		this.managerProfile.state = ManagerState.Ready;
		GameUtilities.String.ToText(this.timeText, string.Empty);
	}

	private IEnumerator Boosting()
	{
		this.boostEffect.SetActive(true);
		while (this.managerProfile.remainingTime > 0)
		{
			GameUtilities.String.ToText(this.timeText, GameUtilities.DateTime.Convert(this.managerProfile.remainingTime));
			yield return this.waitForSeconds;
			this.managerProfile.remainingTime--;
		}
		this.boostController.Refresh();
		this.managerProfile.state = ManagerState.Cooldown;
		this.managerProfile.lastActive = DateTime.Now.ToString();
		this.managerProfile.remainingTime = Singleton<GameProcess>.Instance.GetManagerSkillCooldown(this.managerProfile.experience, this.managerProfile.skill);
		if (this.managerProfile.skill == ManagerSkill.UpgradeCost)
		{
			Singleton<GameManager>.Instance.onCashChange(Singleton<GameManager>.Instance.database.cash);
		}
		this.cooldown = this.StartCoroutine(this.Cooldown());
	}
}
