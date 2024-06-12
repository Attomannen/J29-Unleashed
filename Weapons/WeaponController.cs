using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
	public enum Weapons { Cannon, Missile, Bomb };
	public Weapons activeWeapon;

	[System.Serializable]
	public class Cannon
	{
		[Header("Cannon Objects")]
		public List<Transform> muzzlePositions = new List<Transform>();
		public AudioClip[] cannonSounds        = null;
		public GameObject projectilePrefab     = null;

		[Header("Cannon Attributes")]
		public int numProjectilesPerMuzzle = 1;
		public float maxSpeed              = 100.0f;
		public float convergenceDistance   = 500.0f;
		public float cannonCooldown        = 1.0f;

		[Header("Burst Settings")]
		public int maxShotsInBurst     = 3;
		public float burstShotCooldown = 0.1f;
		public float burstCooldown     = 1.0f;
		public bool isBurstMode        = true;
	}
	public Cannon cannonConfig         = new Cannon();

	[System.Serializable]
	public class Missile
	{
		public List<Transform> missilePositions  = new List<Transform>();
		public GameObject missilePrefab          = null;
		public float missileCooldown             = 1.0f;
		public float missileThreshold            = 0.4f;
		public List<float> missileCooldownTimers = null;
		public float maxSpeed                    = 10.0f;
	}
	public Missile missileConfig                 = new Missile();

	[System.Serializable]
	public class Napalm
	{
		public Transform bombSpawnPos = null;
		public GameObject bombPrefab  = null;
	}
	public Napalm napalmConfig        = new Napalm();

	private Vector3 convergencePoint  = Vector3.zero;
	private float cannonCooldownTimer = 0.0f;
	private int currentMuzzleIndex    = 0;
	private float isFiring            = 0.0f;

	private AudioSource audioSource     = null;
	private AudioClip activeCannonSound = null;

	private int shotsFiredInBurst        = 0;
	private float burstShotCooldownTimer = 0.0f;
	private bool isBurstFiring           = false;

	private ParticleSystem muzzleFlash = null;

	private IPlayerInputHandler playerInputHandler = null;
	private IAIFlightInputHandler aiInputHandler   = null;

	public float GetMissileSpeed() => missileConfig.maxSpeed;
	public float GetCannonSpeed() => cannonConfig.maxSpeed;
	public Vector3 GetConvergencePoint() => convergencePoint;

	private void Start()
	{
		InitializeWeaponConfigurations();
		audioSource = GetComponent<AudioSource>();
		muzzleFlash = GetComponentInChildren<ParticleSystem>();
	}

	private void Update()
	{
		UpdateConvergencePoint();
		HandleWeaponFire();
	}

	public void InitializePlayer(IPlayerInputHandler playerInputHandler)
	{
		this.playerInputHandler = playerInputHandler;
	}

	public void InitializeAI(IAIFlightInputHandler aiInputHandler)
	{
		this.aiInputHandler = aiInputHandler;
	}

	private void InitializeWeaponConfigurations()
	{
		missileConfig.missileCooldownTimers = new List<float>(new float[missileConfig.missilePositions.Count]);
	}

	private void UpdateConvergencePoint()
	{
		Vector3 rayDirection = transform.forward;
		if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, cannonConfig.convergenceDistance))
		{
			convergencePoint = hit.point;
		}
		else
		{
			convergencePoint = transform.position + rayDirection * cannonConfig.convergenceDistance;
		}
	}

	private void HandleWeaponFire()
	{
		isFiring = GetFiringInput();

		switch (activeWeapon)
		{
			case Weapons.Cannon:
				HandleCannon();
				break;
			case Weapons.Missile:
				HandleMissile();
				break;
			case Weapons.Bomb:
				HandleBomb();
				break;
		}
	}

	private float GetFiringInput()
	{
		if (playerInputHandler != null)
		{
			return playerInputHandler.GetFire();
		}

		if (aiInputHandler != null)
		{
			return aiInputHandler.GetFire();
		}

		return 0.0f;
	}

	private void HandleCannon()
	{
		if (isFiring >= 0.2f) FireCannon();
		if (cannonCooldownTimer > 0) cannonCooldownTimer -= Time.deltaTime;
	}

	private void HandleMissile()
	{
		if (isFiring >= 0.2f && missileConfig.missileThreshold <= 0.0f)
		{
			FireMissile(gameObject);
			missileConfig.missileThreshold = 0.5f;
		}
		else
		{
			missileConfig.missileThreshold -= Time.deltaTime;
		}

		UpdateMissileCooldownTimers();
	}

	private void UpdateMissileCooldownTimers()
	{
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

	private void HandleBomb()
	{
		if (isFiring >= 0.2f) FireBomb();
		if (cannonCooldownTimer > 0) cannonCooldownTimer -= Time.deltaTime;
	}

	private void FireCannon()
	{
		if (cannonConfig.projectilePrefab == null)
		{
			Debug.LogWarning("Projectile prefab is not assigned.");
			return;
		}

		if (cannonConfig.isBurstMode)
		{
			HandleBurstFire();
		}
		else
		{
			HandleSingleFire();
		}
	}

	private void HandleBurstFire()
	{
		if (isBurstFiring)
		{
			if (burstShotCooldownTimer > 0)
			{
				burstShotCooldownTimer -= Time.deltaTime;
				return;
			}

			if (shotsFiredInBurst >= cannonConfig.maxShotsInBurst)
			{
				isBurstFiring = false;
				shotsFiredInBurst = 0;
				cannonCooldownTimer = cannonConfig.burstCooldown;
				return;
			}

			FireCannonProjectile();
			shotsFiredInBurst++;
			burstShotCooldownTimer = cannonConfig.burstShotCooldown;
		}
		else
		{
			if (cannonCooldownTimer > 0) return;

			isBurstFiring = true;
			shotsFiredInBurst = 0;
			burstShotCooldownTimer = 0.0f;
		}
	}

	private void HandleSingleFire()
	{
		if (cannonCooldownTimer > 0) return;

		FireCannonProjectile();
		cannonCooldownTimer = cannonConfig.cannonCooldown;
	}

	private void FireCannonProjectile()
	{
		Transform muzzle = cannonConfig.muzzlePositions[currentMuzzleIndex];
		GameObject projectile = Instantiate(cannonConfig.projectilePrefab, muzzle.position, muzzle.rotation);

		if (muzzleFlash)
		{
			muzzleFlash.Play();
		}

		Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();
		if (projectileRB != null)
		{
			Rigidbody rb = GetComponent<Rigidbody>();
			if(rb)
				projectileRB.velocity = muzzle.forward * cannonConfig.maxSpeed + rb.velocity;
			else
				projectileRB.velocity = muzzle.forward * cannonConfig.maxSpeed;
		}

		CameraShake.Instance?.Shake(0.1f, 15f, 0.75f, true, transform);

		activeCannonSound = cannonConfig.cannonSounds[Random.Range(0, cannonConfig.cannonSounds.Length)];
		audioSource.clip = activeCannonSound;
		audioSource.Play();

		currentMuzzleIndex = (currentMuzzleIndex + 1) % cannonConfig.muzzlePositions.Count;
	}

	private void FireMissile(GameObject instigator)
	{
		if (missileConfig.missilePrefab == null)
		{
			Debug.LogWarning("Missile Not Assigned");
			return;
		}

		Transform availableMuzzle = GetAvailableMissileMuzzle(out int availableMuzzleIndex);
		if (availableMuzzle == null)
		{
			Debug.LogWarning("No Active Missiles");
			return;
		}

		GameObject missileInstance = Instantiate(missileConfig.missilePrefab, availableMuzzle.position, availableMuzzle.rotation);
		missileInstance.GetComponent<MissileController>()?.SetInstigator(instigator);

		availableMuzzle.gameObject.SetActive(false);
		missileConfig.missileCooldownTimers[availableMuzzleIndex] = missileConfig.missileCooldown;
	}

	private Transform GetAvailableMissileMuzzle(out int availableMuzzleIndex)
	{
		availableMuzzleIndex = -1;
		for (int i = 0; i < missileConfig.missilePositions.Count; i++)
		{
			if (missileConfig.missileCooldownTimers[i] <= 0)
			{
				availableMuzzleIndex = i;
				return missileConfig.missilePositions[i];
			}
		}
		return null;
	}

	private void FireBomb()
	{
		if (cannonCooldownTimer > 0) return;

		cannonCooldownTimer = cannonConfig.cannonCooldown * 10.0f;
		GameObject bomb = Instantiate(napalmConfig.bombPrefab, napalmConfig.bombSpawnPos.position, napalmConfig.bombSpawnPos.rotation);
		Rigidbody rb = bomb.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.velocity = GetComponent<Rigidbody>().velocity;
		}
	}

}
