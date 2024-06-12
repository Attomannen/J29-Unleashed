using System.Net;
using UnityEngine;

public class CrosshairController : MonoBehaviour
{
	WeaponController weaponController = null;
	RectTransform crosshair = null;

	[SerializeField] private float viewportThreshold = 0.1f;
	[SerializeField] private bool leadingReticle = false;
	[SerializeField] private float lerpSpeed = 5.0f;

	private void Start()
	{
		crosshair = GetComponent<RectTransform>();
		weaponController = InputHandler.Instance.GetComponent<WeaponController>();
	}

	void FixedUpdate()
	{
		if (weaponController != null)
		{
			if (weaponController.activeWeapon == WeaponController.Weapons.Cannon)
				CannonCrosshair();
			else if (weaponController.activeWeapon == WeaponController.Weapons.Missile)
				MissileCrosshair();
			else if (weaponController.activeWeapon == WeaponController.Weapons.Bomb)
				BombingCrosshair();
		}
	}
	private void CannonCrosshair()
	{
		if (weaponController == null)
		{
			Debug.LogWarning("WeaponController is not assigned to CrosshairController.");
			return;
		}

		Vector3 convergencePoint = weaponController.GetConvergencePoint();
		Vector3 screenPoint = Camera.main.WorldToScreenPoint(convergencePoint);
		screenPoint.z = Mathf.Clamp(screenPoint.z, 0.0f, 1000.0f);

		Vector3 targetPosition = screenPoint;

		if (leadingReticle && TargetingController.Instance != null)
		{
			GameObject target = TargetingController.Instance.GetTargetInRange(weaponController.transform.position, weaponController.cannonConfig.convergenceDistance, viewportThreshold);
			if (target != null)
			{
				Vector3 targetPos = target.transform.position;
				Vector3 viewportPoint = Camera.main.WorldToViewportPoint(targetPos);

				Rigidbody targetRigidbody = target.GetComponent<Rigidbody>();

				if (TargetingController.Instance.IsInViewport(viewportPoint, viewportThreshold))
				{

					if (!targetRigidbody)
					{
						targetPosition = Camera.main.WorldToScreenPoint(targetPos);
					}
					else
					{
						Vector3 leadPos = TargetingController.InterceptLead(
							weaponController.transform.position,
							weaponController.GetComponent<Rigidbody>().velocity,
							weaponController.cannonConfig.maxSpeed,
							targetPos,
							targetRigidbody.velocity
						);

						targetPosition = Camera.main.WorldToScreenPoint(leadPos);
					}

					targetPosition.z = Mathf.Clamp(targetPosition.z, 0.0f, 1000.0f);
				}
			}
		}


			crosshair.position = Vector3.Lerp(crosshair.position, targetPosition, Time.deltaTime * lerpSpeed);
		

	}

	private void MissileCrosshair()
	{
		if (weaponController == null)
		{
			Debug.LogWarning("WeaponController is not assigned to CrosshairController.");
			return;
		}

		Vector3 convergencePoint = weaponController.GetConvergencePoint();
		Vector3 screenPoint = Camera.main.WorldToScreenPoint(convergencePoint);
		screenPoint.z = Mathf.Clamp(screenPoint.z, 0.0f, 1000.0f);

		Vector3 targetPosition = screenPoint;

		if (leadingReticle && TargetingController.Instance != null)
		{
			GameObject target = TargetingController.Instance.GetTargetInRange(weaponController.transform.position, weaponController.cannonConfig.convergenceDistance * 2.0f, viewportThreshold);
			if (target != null)
			{
				Vector3 targetPos = target.transform.position;
				Vector3 viewportPoint = Camera.main.WorldToViewportPoint(targetPos);

				if (TargetingController.Instance.IsInViewport(viewportPoint, viewportThreshold))
				{
					targetPosition = Camera.main.WorldToScreenPoint(targetPos);

					targetPosition.z = Mathf.Clamp(targetPosition.z, 0.0f, 1000.0f);
				}
			}
		}

		crosshair.position = Vector3.Lerp(crosshair.position, targetPosition, Time.deltaTime * lerpSpeed);
	}


	private void BombingCrosshair()
	{
		Vector3 convergencePoint = weaponController.GetConvergencePoint();
		Vector3 screenPoint = Camera.main.WorldToScreenPoint(convergencePoint);
		screenPoint.z = Mathf.Clamp(screenPoint.z, 0.0f, 1000.0f);

		crosshair.position = Vector3.Lerp(crosshair.position, screenPoint, Time.deltaTime * lerpSpeed);
	}
}
