using UnityEngine;

public class CrosshairController : MonoBehaviour
{
	TargetingController targetFinder         = null;
	RectTransform crosshair                  = null;

	[SerializeField] float viewportThreshold = 0.1f;
	[SerializeField] bool leadingReticle     = false;
	[SerializeField] float bulletSpeed       = 5f;
	WeaponController weaponController        = null;

	private void Start()
	{
		crosshair = GetComponent<RectTransform>();
	}

	void FixedUpdate()
	{
		if( weaponController != null )
		{
		if (weaponController.activeWeapon == WeaponController.Weapons.Cannon)
			CannonCrosshair();
		else if (weaponController.activeWeapon == WeaponController.Weapons.Missile)
			MissileCrosshair();
		}
		else
			weaponController = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<WeaponController>();
		if(targetFinder == null )
		{
			targetFinder = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<TargetingController>();

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

		if (!leadingReticle)
		{
			crosshair.position = screenPoint;
			return;
		}
		if (targetFinder != null)
		{
			GameObject target = targetFinder.GetTargetInRange(weaponController.cannonConfig.convergenceDistance, viewportThreshold);

			if (target == null)
			{
				crosshair.position = screenPoint;
				return;
			}
			else
			{

				Vector3 targetPos = target.transform.position;
				Vector3 viewportPoint = Camera.main.WorldToViewportPoint(targetPos);

				if (targetFinder.IsInViewport(viewportPoint, viewportThreshold))
				{
					var leadPos = TargetingController.InterceptLead(
						weaponController.gameObject.transform.position,
						weaponController.gameObject.GetComponent<Rigidbody>().velocity,
						bulletSpeed,
						targetPos,
						target.GetComponent<Rigidbody>().velocity
					);
					Vector3 leadPoint = Camera.main.WorldToScreenPoint(leadPos);
					leadPoint.z = Mathf.Clamp(leadPoint.z, 0.0f, 1000.0f);
					crosshair.position = leadPoint;
				}
				else
				{
					crosshair.position = screenPoint;
				}
			}
		}


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

		if (!leadingReticle)
		{
			crosshair.position = screenPoint;
			return;
		}
		if (targetFinder != null)
		{

			GameObject target = targetFinder.GetTargetInRange(weaponController.cannonConfig.convergenceDistance * 2.0f, viewportThreshold);
			if (target == null)
			{
				crosshair.position = screenPoint;
				return;
			}

			Vector3 targetPos = target.transform.position;
			Vector3 viewportPoint = Camera.main.WorldToViewportPoint(targetPos);

			if (targetFinder.IsInViewport(viewportPoint, viewportThreshold))
			{

				Vector3 leadPoint = Camera.main.WorldToScreenPoint(targetPos);
				leadPoint.z = Mathf.Clamp(leadPoint.z, 0.0f, 1000.0f);
				crosshair.position = leadPoint;
			}
			else
			{
				crosshair.position = screenPoint;
			}
		}
	}
}
