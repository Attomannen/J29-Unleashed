using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.IO;

[RequireComponent(typeof(Rigidbody))]
public class FlightController : MonoBehaviour
{

	private Rigidbody rb = null;
	private Vector3 liftForce = Vector3.zero;

	private float throttleInput = 0f;

	private float pitchAngle = 0f;
	private float startPitchFactor = 0f;

	private float stallTimer = 0f;
	private float finalDiveSpeed = 0.0f;
	private float diveAcceleration = 0.0f;

	private float yaw = 0f;
	private float pitch = 0f;
	private float roll = 0f;

	private float maxLift = 2.0f;

	public float GetThrottle() { return throttle; }
	public bool IsStalling() { return isStalling; }
	public bool IsDiving() { return isDiving; }


	[Header("Throttle")]
	[SerializeField] private float throttleChangeSpeed = 0.0f;
	[SerializeField] private float throttleMultiplier = 0f;
	[SerializeField, Range(0, 100f)] private float throttle = 0f;

	[Header("Roll")]
	[SerializeField] private float rollMultiplier = 0f;

	[Header("Pitch")]
	[SerializeField] private float pitchMultiplier = 0f;
	[SerializeField] private float pitchFactor = 1f;

	[Header("Yaw")]
	[SerializeField] private float yawMultiplier = 0f;

	[Header("Mass Scaling")]
	[SerializeField] private float minMass = 600f;
	[SerializeField] private float maxMass = 10f;

	[Header("Drag Scaling")]
	[SerializeField] private float minDrag = 600f;
	[SerializeField] private float maxDrag = 10f;

	[Header("Lift Parameters")]
	[SerializeField] private float maxAlignmentSpeed = 0.5f;
	[SerializeField] private float minAlignmentSpeed = 0.05f;
	[SerializeField] private float liftCoefficient = 1f;
	[SerializeField] private float airDensity = 1.225f;
	[SerializeField] private float wingArea = 10f;
	[SerializeField] private float liftMultiplier = 0.5f;
	[SerializeField] private float minSpeedForLift = 34f;

	[Header("Stall And Dive Parameters")]
	[SerializeField] private float stallTimeThreshold = 2f;
	[SerializeField, Range(-1f, 1f)] private float stallAngle = 0.75f;
	[SerializeField, Range(-1f, 1f)] private float criticalStallAngle = 0.75f;
	[SerializeField, Range(-1f, 1f)] private float diveAngle = -0.35f;
	bool isDiving = false;
	bool isStalling = false;

	[Header("Velocity Parameters")]
	[SerializeField] private float maxVelocity = 250f;

	[Header("Debugging Parameters")]
	[SerializeField] bool ShouldAlignWithVelocty = false; 
	private IPlayerInputHandler playerInputHandler;
	private IAIFlightInputHandler aiInputHandler;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		startPitchFactor = pitchFactor;

	}

	public void InitializePlayer(IPlayerInputHandler playerInputHandler)
	{
		this.playerInputHandler = playerInputHandler;
	}

	public void InitializeAI(IAIFlightInputHandler aiInputHandler)
	{
		this.aiInputHandler = aiInputHandler;
	}

	private void Update()
	{
		HandleInput();
		UpdateFlightParameters();

	}

	private void FixedUpdate()
	{
		ApplyForces();
		ApplyLift();
		AlignVelocityWithForwardDirection();
		CheckPitchAngle();
		CapVelocity();
	}

	#region Input Handling
	private void HandleInput()
	{
		if (playerInputHandler != null)
		{
			float currentThrottleInput = playerInputHandler.GetThrottle();
			float currentYawInput = playerInputHandler.GetYaw();
			float currentPitchInput = playerInputHandler.GetPitch();
			float currentRollInput = playerInputHandler.GetRoll();

			if (currentThrottleInput != throttleInput)
			{
				throttleInput = currentThrottleInput;
			}
			if (currentYawInput != yaw)
			{
				yaw = currentYawInput;
			}
			if (currentPitchInput != pitch)
			{
				pitch = currentPitchInput;
			}
			if (currentRollInput != roll)
			{
				roll = currentRollInput;
			}
		}

		if (aiInputHandler != null)
		{
			throttleInput = aiInputHandler.GetThrottle();
			yaw = aiInputHandler.GetYaw();
			pitch = aiInputHandler.GetPitch();
			roll = aiInputHandler.GetRoll();
		}

		throttle += throttleInput * Time.deltaTime * throttleChangeSpeed;
		throttle = Mathf.Clamp(throttle, 0, 100f);

		if (throttle == 0 && throttleInput < 0)
		{
			rb.drag = Mathf.Lerp(minDrag, 0.2f, Mathf.Abs(throttleInput));
			maxLift = 2.0f;
		}
		else
		{
			rb.drag = Mathf.Lerp(minDrag, maxDrag, throttle / 100f);
			maxLift = 1.0f;
		}
	}

	#endregion

	#region Flight Mechanics

	private void UpdateFlightParameters()
	{
		rb.mass = Mathf.Lerp(minMass, maxMass, throttle / 100f);
		liftCoefficient = Mathf.Lerp(0, maxLift, rb.velocity.magnitude / 175.0f);
	}

	private void ApplyForces()
	{
		rb.AddRelativeForce(Vector3.forward * finalDiveSpeed * throttle * throttleMultiplier * Time.deltaTime);
		float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
		float autoPitch = Mathf.Lerp(0, -1f, forwardSpeed / 10f) * pitchFactor;

		rb.AddRelativeTorque((pitch + autoPitch) * pitchMultiplier * Time.deltaTime, yaw * yawMultiplier * Time.deltaTime, -roll * rollMultiplier * Time.deltaTime);
	}

	private void ApplyLift()
	{
		float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
		float gravitationalForce = rb.mass * Physics.gravity.magnitude;
		float liftForceMagnitude = liftMultiplier * airDensity * forwardSpeed * forwardSpeed * wingArea * liftCoefficient;

		float speedFactor = Mathf.Clamp01((forwardSpeed - minSpeedForLift) / minSpeedForLift);
		liftForceMagnitude = Mathf.Min(liftForceMagnitude, gravitationalForce);
		liftForceMagnitude *= speedFactor;

		Vector3 liftDirection = transform.up;
		liftForce = liftDirection * liftForceMagnitude;

		rb.AddForce(liftForce);
	}

	private void AlignVelocityWithForwardDirection()
	{
		if (!ShouldAlignWithVelocty)
			return;

		float gravitationalForce = rb.mass * Physics.gravity.magnitude;
		bool isLiftSufficient = liftForce.magnitude / 2.0f >= gravitationalForce;

		float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
		float alignmentSpeed = isLiftSufficient ? maxAlignmentSpeed : minAlignmentSpeed;
		Vector3 alignedVelocity = Vector3.Lerp(rb.velocity, transform.forward * forwardSpeed, alignmentSpeed);

		rb.velocity = alignedVelocity;
	}

	private void CheckPitchAngle()
	{
		pitchAngle = Vector3.Dot(transform.forward, Vector3.up);

		if (pitchAngle > stallAngle)
		{
			float velocityReduction = Mathf.Lerp(0, 0.3f, Mathf.Clamp01((pitchAngle - stallAngle) / stallAngle));
			rb.velocity *= (1.0f - velocityReduction);
			isStalling = true;
			isDiving   = false;
			if (pitchAngle > criticalStallAngle)
			{

				stallTimer += Time.fixedDeltaTime;

				if (stallTimer > stallTimeThreshold)
					pitchFactor -= 1.0f * Time.deltaTime;
			}
		}
		else
		{
			ResetStallTimer();
		}

		if (pitchAngle <= diveAngle)
		{
			float t = 0.0f;
			pitchFactor = 0.0f;
			t += 0.125f * Time.deltaTime;
			isDiving   = true;
			isStalling = false;
			diveAcceleration = (((pitchAngle - diveAngle) / diveAngle) + 6.0f) / 2.0f;
			finalDiveSpeed = Mathf.Lerp(finalDiveSpeed, diveAcceleration, t);
			finalDiveSpeed = Mathf.Clamp(finalDiveSpeed, 1.0f, 4.0f);
		}
		else
		{
			float t = 0.0f;
			t += 0.5f * Time.deltaTime;
			isDiving = false;


			finalDiveSpeed = Mathf.Lerp(finalDiveSpeed, 1.0f, t);
		}
	}

	private void CapVelocity()
	{
		if (rb.velocity.magnitude > maxVelocity)
		{
			rb.velocity = rb.velocity.normalized * maxVelocity;
		}
	}

	private void ResetStallTimer()
	{

		stallTimer -= 2f * Time.fixedDeltaTime;
		if (stallTimer < 0f)
		{
			pitchFactor = startPitchFactor;
			isStalling = false;
			stallTimer = 0f;
		}
	}

	#endregion
}
