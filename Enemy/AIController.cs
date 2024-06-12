using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
	private FlightController flightController = null;
	private WeaponController weaponController = null;
	private AIInputHandler inputHandler       = null;
	private Transform playerTarget            = null;

	private float minY = 200f;
	private float maxY = 700f;

	private float timer         = 0.0f;
	private float throttleTimer = 0.0f;

	private Vector3 randomTarget      = Vector3.zero;
	private Vector3 targetDirection   = Vector3.zero;
	private Quaternion targetRotation = Quaternion.identity;

	[SerializeField] private bool isPlane = false;

	private GameObject target = null;

	[SerializeField] private LayerMask collisionLayer;

	[System.Serializable]
	public class Plane
	{
		[Header("Random Target Parameters")]
		public float maxDistanceFromStart = 300f;
		public float minDistanceFromStart = 100f;
		public float switchTargetInterval = 5.0f;

		[Header("Ground Check")]
		public float groundCheckDistance = 10f;
		public float minAltitude         = 50f;

		[Header("Stabilization Parameters")]
		public float rollStabilizationThreshold = 45f;
		public float rollStabilizationSpeed     = 2.0f;

		[Header("Turning Parameters")]
		public float sharpTurnThreshold = 45f;
		public float sharpTurnYawFactor = 2.0f;

		[Header("Control Sensitivity")]
		public float yawSensitivity   = 1.5f;
		public float pitchSensitivity = 1.0f;
		public float rollSensitivity  = 1.0f;

		[Header("Throttle Parameters")]
		public float minThrottle            = 0.5f;
		public float maxThrottle            = 1.5f;
		public float throttleChangeInterval = 3.0f;

		public bool shouldTargetPlayer = false;
	}
	public Plane planeConfig           = new Plane();

	[System.Serializable]
	public class Stationary
	{
		public List<Transform> turretPos              = new List<Transform>();
		public Transform rotationBase                 = null;
		public float range                            = 3500.0f;
		public float fireDelay                        = 1.0f;
		[HideInInspector] public float turretCooldown = 1.0f;
		public float baseCooldown                     = 1.0f;
	}
	public Stationary stationaryConfig                = new Stationary();

	private float time = 0.0f;

	private void Awake()
	{
		InitializeComponents();
	}

	private void Start()
	{
		if (isPlane)
		{
			SwitchTarget();
		}

		if (InputHandler.Instance)
		{
			playerTarget = InputHandler.Instance.transform;
		}

		if (!isPlane)
		{
			float randomCooldown = Random.Range(stationaryConfig.baseCooldown - 0.5f, stationaryConfig.baseCooldown + 0.5f);
			stationaryConfig.turretCooldown = randomCooldown;
		}
	}

	private void Update()
	{
		if (isPlane)
		{
			HandlePlane();
		}
		else
		{
			HandleTurret();
		}
	}

	private void InitializeComponents()
	{
		weaponController = GetComponent<WeaponController>();
		flightController = GetComponent<FlightController>();
		inputHandler = GetComponent<AIInputHandler>();

		if (flightController != null)
		{
			flightController.InitializeAI(inputHandler);
		}

		if (weaponController != null)
		{
			weaponController.InitializeAI(inputHandler);
		}
	}

	#region Plane

	private void HandlePlane()
	{
		timer += Time.deltaTime;
		throttleTimer += Time.deltaTime;

		if (timer >= planeConfig.switchTargetInterval)
		{
			SwitchTarget();
			timer = 0;
		}

		if (throttleTimer >= planeConfig.throttleChangeInterval)
		{
			SetThrottle(Random.Range(planeConfig.minThrottle, planeConfig.maxThrottle));
			throttleTimer = 0;
		}

		if ((transform.position - randomTarget).magnitude <= 20.0f)
		{
			SwitchTarget();
		}

		targetDirection = (randomTarget - transform.position).normalized;
		targetRotation = Quaternion.LookRotation(targetDirection);

		if (playerTarget && planeConfig.shouldTargetPlayer)
		{
			Vector3 offset = CalculateOffsetFromPlayer();
			Vector3 desiredDirection = (playerTarget.position + offset - transform.position).normalized;
			targetRotation = Quaternion.LookRotation(desiredDirection);
		}

		AlignToTarget(targetRotation);
	}

	private void SwitchTarget()
	{
		if (playerTarget != null && planeConfig.shouldTargetPlayer)
		{
			Vector3 offset = CalculateOffsetFromPlayer();
			randomTarget = playerTarget.position + offset;
		}
		else
		{
			Vector3 randomDirection = Random.onUnitSphere;
			float distance = Random.Range(planeConfig.minDistanceFromStart, planeConfig.maxDistanceFromStart);
			randomDirection *= distance;
			randomDirection.y = Mathf.Clamp(randomDirection.y, minY, maxY);
			randomTarget = randomDirection;
		}
	}

	private Vector3 CalculateOffsetFromPlayer()
	{
		if (playerTarget == null) return Vector3.zero;

		Vector3 offset = new Vector3(
			Random.Range(-planeConfig.maxDistanceFromStart, planeConfig.maxDistanceFromStart),
			Random.Range(-planeConfig.minDistanceFromStart, planeConfig.minDistanceFromStart),
			Random.Range(-planeConfig.minDistanceFromStart, planeConfig.minDistanceFromStart)
		);

		return offset;
	}

	private void AlignToTarget(Quaternion targetRotation)
	{
		Vector3 targetEulerAngles = targetRotation.eulerAngles;
		Vector3 currentEulerAngles = transform.eulerAngles;
		float yDistance = Mathf.Abs(transform.position.y - randomTarget.y);

		float yawError = Mathf.DeltaAngle(currentEulerAngles.y, targetEulerAngles.y);
		float rollError = Mathf.DeltaAngle(currentEulerAngles.z, targetEulerAngles.z);
		float pitchError = Mathf.DeltaAngle(currentEulerAngles.x, targetEulerAngles.x);

		float yawControl = Mathf.Clamp(yawError / 180f, -1f, 1f) * planeConfig.yawSensitivity;
		float pitchControl = Mathf.Clamp(pitchError / 180f, -1f, 1f) * planeConfig.pitchSensitivity * yDistance;
		float rollControl = Mathf.Clamp(rollError / 180f, -1f, 1f) * planeConfig.rollSensitivity;

		if (Mathf.Abs(currentEulerAngles.z) > planeConfig.rollStabilizationThreshold)
		{
			rollControl = -Mathf.Sign(currentEulerAngles.z) * planeConfig.rollStabilizationSpeed;
		}

		Quaternion finalRotation = Quaternion.LookRotation(randomTarget - transform.position);
		transform.rotation = Quaternion.Lerp(transform.rotation, finalRotation, 0.75f * Time.deltaTime);
		SetPitch(-pitchControl);
		SetYaw(yawControl);
		SetRoll(rollControl);
	}

	private void SetThrottle(float throttle)
	{
		inputHandler.SetThrottle(throttle);
	}

	private void SetPitch(float pitch)
	{
		inputHandler.SetPitch(pitch);
	}

	private void SetYaw(float yaw)
	{
		inputHandler.SetYaw(yaw);
	}

	private void SetRoll(float roll)
	{
		inputHandler.SetRoll(roll);
	}

	#endregion

	#region Turret

	private void HandleTurret()
	{
		FindTarget();
		FireWeapon();
	}

	private void FindTarget()
	{
		target = TargetingController.Instance.GetTargetInRange(transform.position, stationaryConfig.range, gameObject);

		if (!target) return;

		Vector3 relativePos = target.transform.position - stationaryConfig.rotationBase.position;
		Quaternion lookAtRotation = Quaternion.LookRotation(relativePos);

		Quaternion baseRotation = Quaternion.Euler(stationaryConfig.rotationBase.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, stationaryConfig.rotationBase.rotation.eulerAngles.z);
		stationaryConfig.rotationBase.rotation = Quaternion.Lerp(stationaryConfig.rotationBase.rotation, baseRotation, 8.0f * Time.deltaTime);

		foreach (Transform t in stationaryConfig.turretPos)
		{
			var leadPos = TargetingController.InterceptLead(t.position, Vector3.zero, weaponController.GetCannonSpeed(), target.transform.position, target.GetComponentInParent<Rigidbody>().velocity);
			Quaternion targetRotation = Quaternion.LookRotation(leadPos - t.position);
			Debug.DrawLine(t.position, leadPos);

			t.rotation = Quaternion.Lerp(t.rotation, targetRotation, 8.0f * Time.deltaTime);
		}
	}

	private void FireWeapon()
	{
		if (!target)
		{
			SetFire(0.0f);
			return;
		}

		time += Time.deltaTime;
		if (time >= stationaryConfig.turretCooldown)
		{
			SetFire(1.0f);
		}
	}

	private void SetFire(float fire)
	{
		inputHandler.SetFire(fire);
	}

	#endregion

}
