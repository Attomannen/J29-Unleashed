using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class WeaponController : MonoBehaviour
{
	public enum Weapons { Cannon, Missile, Napalm, Weapon_4 };
	public Weapons activeWeapon;

	[System.Serializable]
	public class Cannon
	{
		public List<Transform> muzzlePositions = new List<Transform>();
		public GameObject projectilePrefab     = null;
		public int numProjectilesPerMuzzle     = 1;
		public float projectileSpeed           = 100.0f;
		public float convergenceDistance       = 500.0f;
		public float cannonCooldown            = 1.0f;
	}
	public Cannon cannonConfig = null;

	[System.Serializable]
	public class Missile
	{
		public List<Transform> missilePositions  = new List<Transform>();
		public GameObject missilePrefab = null;
		public float missileCooldown             = 1.0f;
		public float missileThreshold            = 0.4f;
		public List<float> missileCooldownTimers = null;
		public float maxSpeed                    = 10.0f;

	}
	public Missile missileConfig = null;

	[System.Serializable]
	public class Napalm
	{
		public Transform bombSpawnPos = null;
		public GameObject bombPrefab  = null;
	}
	public Napalm napalmConfig = null;

	Vector3 convergencePoint  = Vector3.zero;
	float cannonCooldownTimer = 0.0f;
	int currentMuzzleIndex    = 0;
	float isFiring            = 0.0f;
	public float GetMissileSpeed() { return missileConfig.maxSpeed; }
	public Vector3 GetConvergencePoint() { return convergencePoint; }
	private void Start()
	{
		UpdateConvergencePoint();
		missileConfig.missileCooldownTimers = new List<float>(new float[missileConfig.missilePositions.Count]);
	}

	private void Update()
	{

		UpdateConvergencePoint();

		switch (activeWeapon)
		{
			case Weapons.Cannon:
				HandleCannon();
				break;
			case Weapons.Missile:
				HandleMissile();
				break;
			case Weapons.Napalm:
				HandleBomb();
				break;
		}


	}

	private void HandleCannon()
	{
		if (isFiring >= 0.2f)
		{
			FireCannon();
		}

		if (cannonCooldownTimer > 0)
		{
			cannonCooldownTimer -= Time.deltaTime;
		}
	}
	private void HandleBomb()
	{
		if (isFiring >= 0.2f)
		{
			FireBomb();
		}

		if (cannonCooldownTimer > 0)
		{
			cannonCooldownTimer -= Time.deltaTime;
		}
	}
	private void HandleMissile()
	{
		if (isFiring >= 0.2f && missileConfig.missileThreshold <= 0.0f)
		{
			FireMissile();
			missileConfig.missileThreshold = 0.5f;
		}
		else
		{
			missileConfig.missileThreshold -= Time.deltaTime;
		}

		for (int i = 0; i < missileConfig.missileCooldownTimers.Count; i++)
		{
			if (missileConfig.missileCooldownTimers[i] > 0)
			{
				missileConfig.missileCooldownTimers[i] -= Time.deltaTime;
				if (missileConfig.missileCooldownTimers[i] <= 0)
				{
					missileConfig.missilePositions[i].gameObject.SetActive(true);
				}
			}
		}
	}

	private void UpdateConvergencePoint()
	{
		RaycastHit hit;
		Vector3 rayDirection = transform.forward;

		if (Physics.Raycast(transform.position, rayDirection, out hit, cannonConfig.convergenceDistance))
		{
			convergencePoint = hit.point;
		}
		else
		{
			convergencePoint = transform.position + rayDirection * cannonConfig.convergenceDistance;
		}
	}

	private void FireMissile()
	{
		if (missileConfig.missilePrefab == null)
		{
			Debug.LogWarning("Missile Not Assigned");
			return;
		}

		Transform availableMuzzle = null;
		int availableMuzzleIndex = -1;

		for (int i = 0; i < missileConfig.missilePositions.Count; i++)
		{
			if (missileConfig.missileCooldownTimers[i] <= 0)
			{
				availableMuzzle = missileConfig.missilePositions[i];
				availableMuzzleIndex = i;
				break;
			}
		}

		if (availableMuzzle == null)
		{
			Debug.LogWarning("No Active Missiles");
			return;
		}

		Instantiate(missileConfig.missilePrefab, availableMuzzle.position, availableMuzzle.rotation);
		availableMuzzle.gameObject.SetActive(false);
		missileConfig.missileCooldownTimers[availableMuzzleIndex] = missileConfig.missileCooldown;
	}

	private void FireCannon()
	{
		if (cannonConfig.projectilePrefab == null)
		{
			Debug.LogWarning("Projectile prefab is not assigned.");
			return;
		}

		if (cannonCooldownTimer > 0)
		{
			return;
		}

		cannonCooldownTimer = cannonConfig.cannonCooldown;

		if (cannonConfig.muzzlePositions.Count == 0)
		{
			Debug.LogWarning("No muzzle positions assigned.");
			return;
		}

		Transform muzzle = cannonConfig.muzzlePositions[currentMuzzleIndex];

		for (int i = 0; i < cannonConfig.numProjectilesPerMuzzle; i++)
		{
			GameObject projectile = Instantiate(cannonConfig.projectilePrefab, muzzle.position, muzzle.rotation);
			Rigidbody rb = projectile.GetComponent<Rigidbody>();

			if (rb != null)
			{
				rb.velocity = (muzzle.forward * cannonConfig.projectileSpeed) + gameObject.GetComponent<Rigidbody>().velocity;
			}
		}

		currentMuzzleIndex = (currentMuzzleIndex + 1) % cannonConfig.muzzlePositions.Count;
	}

	private void FireBomb()
	{
		if (cannonCooldownTimer > 0)
		{
			return;
		}

		cannonCooldownTimer = cannonConfig.cannonCooldown;

		GameObject projectile = Instantiate(napalmConfig.bombPrefab, napalmConfig.bombSpawnPos.position, napalmConfig.bombSpawnPos.rotation);
		Rigidbody rb = projectile.GetComponent<Rigidbody>();

		if (rb != null)
		{
			rb.velocity = gameObject.GetComponent<Rigidbody>().velocity;
		}
	}

	public void OnShootAction(InputAction.CallbackContext context)
	{
		isFiring = context.ReadValue<float>();
	}

	public void OnWeaponSwitch(InputAction.CallbackContext context)
	{
		activeWeapon = activeWeapon == Weapons.Cannon ? Weapons.Missile : Weapons.Cannon;
	}
}
