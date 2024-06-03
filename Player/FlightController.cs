using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class FlightController : MonoBehaviour
{
	Rigidbody rb                  = null;
	Vector3 liftForce             = Vector3.zero;
	float startPitchFactor        = 0f;
	float throttleInput           = 0f;
	float stallTimer              = 0f;
	float pitchAngle              = 0f;
	float pitch                   = 0f;
	float maxLift                 = 2.0f;
	float roll                    = 0f;
	float yaw                     = 0f;


	[Header("Throttle")]
	[SerializeField] private float throttleChangeSpeed      = 0.0f;
	[SerializeField] private float throttleMultiplier       = 0f;
	[SerializeField, Range(0, 100f)] float throttle         = 0f;
	[SerializeField] private float divingSpeed              = 100f;

	[Header("Roll")]
	[SerializeField] private float rollMultiplier = 0f;

	[Header("Pitch")]
	[SerializeField] private float pitchMultiplier = 0f;
	[SerializeField] private float pitchFactor     = 1f;

	[Header("Yaw")]
	[SerializeField] private float yawMultiplier = 0f;

	[Header("Canvas")]
	[SerializeField] private TextMeshProUGUI throttleText = null;
	[SerializeField] private TextMeshProUGUI velocityText = null;
	[SerializeField] private TextMeshProUGUI pitchText    = null;

	[Header("Mass Scaling")]
	[SerializeField] private float minMass = 600f;
	[SerializeField] private float maxMass = 10f;

	[Header("Drag Scaling")]
	[SerializeField] private float minDrag = 600f;
	[SerializeField] private float maxDrag = 10f;

	[Header("FOV Scaling")]
	[SerializeField] private float minFOV = 600f;
	[SerializeField] private float maxFOV = 10f;

	[Header("Lift Parameters")]
	[SerializeField] private float maxAlignmentSpeed = 0.5f;
	[SerializeField] private float minAlignmentSpeed = 0.05f;
	[SerializeField] private float liftCoefficient   = 1f;
	[SerializeField] private float airDensity        = 1.225f;
	[SerializeField] private float wingArea          = 10f;
	[SerializeField] private float liftMultiplier          = 0.5f;
	[SerializeField] float minSpeedForLift = 34f;




	[Header("Stall And Dive Parameters")]
	[SerializeField] private float stallTimeThreshold = 2f;
	[SerializeField] private float ForceWhenStall = 200f;
	[SerializeField] private float ForceWhenDive = 200f; 
	[SerializeField, Range(-1f, 1f)] float stallAngle = 0.75f;
	[SerializeField, Range(-1f, 1f)] float diveAngle = -0.35f;


	[Header("Velocity Parameters")]
	[SerializeField] private float maxVelocity = 250f;

	[Header("Sensitivity Parameters")]
	[SerializeField] private float pitchSensitivity = 0.1f;
	[SerializeField] private float rollSensitivity  = 0.2f;

	public InputType currentInput = 0;
	public enum InputType { Keyboard, Controller, Mouse }

	private void Start()
	{
		rb                      = GetComponent<Rigidbody>();
		startPitchFactor        = pitchFactor;
		Cursor.lockState        = CursorLockMode.Locked;
	}

	private void Update()
	{

		HandleInput();
		UpdateFlightParameters();
		HandleMouseInput();

		throttleText.text = $"{throttle:F2}%";
		velocityText.text = $"{rb.velocity.magnitude:F2} m/s";
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
		throttle += throttleInput * Time.deltaTime * throttleChangeSpeed;
		throttle  = Mathf.Clamp(throttle, 0, 100f);

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

	private void HandleMouseInput()
	{
		if (currentInput == InputType.Mouse)
		{
			float pitchInput = Mouse.current.delta.y.ReadValue() * pitchSensitivity;
			float rollInput = Mouse.current.delta.x.ReadValue() * rollSensitivity;

			pitch = Mathf.Clamp(pitchInput, -1f, 1f);
			roll = Mathf.Clamp(rollInput, -1f, 1f);
		}
	}

	#endregion

	#region Flight Mechanics

	private void UpdateFlightParameters()
	{
		rb.mass                 = Mathf.Lerp(minMass, maxMass, throttle / 100f);
		liftCoefficient         = Mathf.Lerp(0, maxLift, rb.velocity.magnitude / 175.0f);
		Camera.main.fieldOfView = Mathf.Lerp(minFOV, maxFOV, rb.velocity.magnitude / 115f);
	}

	private void ApplyForces()
	{
		rb.AddRelativeForce(Vector3.forward * throttle * throttleMultiplier * Time.deltaTime);
		float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
		float autoPitch    = Mathf.Lerp(0, -1f, forwardSpeed / 10f) * pitchFactor;

		rb.AddRelativeTorque((pitch + autoPitch) * 
			pitchMultiplier         * Time.deltaTime,
			yaw * yawMultiplier     * Time.deltaTime,
			-roll * rollMultiplier  * Time.deltaTime);
	}

	private void ApplyLift()
	{
		float forwardSpeed       = Vector3.Dot(rb.velocity, transform.forward);
		float gravitationalForce = rb.mass * Physics.gravity.magnitude;
		float liftForceMagnitude = liftMultiplier * airDensity * forwardSpeed * forwardSpeed * wingArea * liftCoefficient;

		float speedFactor     = Mathf.Clamp01((forwardSpeed - minSpeedForLift) / minSpeedForLift);
		liftForceMagnitude    = Mathf.Min(liftForceMagnitude, gravitationalForce);
		liftForceMagnitude    *= speedFactor;

		Vector3 liftDirection = transform.up;
		liftForce             = liftDirection * liftForceMagnitude;

		rb.AddForce(liftForce);
	}

	private void AlignVelocityWithForwardDirection()
	{
		float gravitationalForce = rb.mass * Physics.gravity.magnitude;
		bool isLiftSufficient    = liftForce.magnitude / 2.0f >= gravitationalForce;

		float forwardSpeed       = Vector3.Dot(rb.velocity, transform.forward);
		float alignmentSpeed     = isLiftSufficient ? maxAlignmentSpeed : minAlignmentSpeed;
		Vector3 alignedVelocity  = Vector3.Lerp(rb.velocity, transform.forward * forwardSpeed, alignmentSpeed);

		rb.velocity = alignedVelocity;
	}


	private void CheckPitchAngle()
	{
		pitchAngle = Vector3.Dot(transform.forward, Vector3.up);

		pitchText.text = pitchAngle.ToString("F2");
		if (pitchAngle > stallAngle)
		{
			stallTimer += Time.fixedDeltaTime;

			float velocityReduction = Mathf.Lerp(0, 1, Mathf.Clamp01((pitchAngle - stallAngle) / stallAngle));
			rb.velocity             *= (1 - velocityReduction);
			float StallFactor        = Mathf.Abs(pitchAngle) * ForceWhenStall;

			rb.AddRelativeForce(-Vector3.forward * divingSpeed * StallFactor * Time.fixedDeltaTime);

			pitchText.text = $"Critical Angle";
			if (stallTimer > stallTimeThreshold)
			{
				pitchFactor -= 1.0f * Time.deltaTime;
			}
		}
		else
		{
			ResetStallTimer();
		}

		if (pitchAngle <= diveAngle)
		{
			pitchFactor  = 0.0f;
			pitchText.text = $"Diving";

			float diveFactor = Mathf.Abs(pitchAngle) * ForceWhenDive;
			rb.AddRelativeForce(-Vector3.forward * divingSpeed * diveFactor * Time.fixedDeltaTime);
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
		stallTimer -= Time.fixedDeltaTime;
		if (stallTimer < 0f)
		{
			pitchFactor = startPitchFactor;
			stallTimer = 0f;
		}
	}

	#endregion

	#region Input Callbacks

	public void OnThrottleChange(InputAction.CallbackContext context)
	{
		throttleInput = context.ReadValue<float>();
	}

	public void OnPitchChanged(InputAction.CallbackContext context)
	{
		pitch = context.ReadValue<float>();
	}

	public void OnRollChanged(InputAction.CallbackContext context)
	{
		roll = context.ReadValue<float>();
	}

	public void OnYawChanged(InputAction.CallbackContext context)
	{
		yaw = context.ReadValue<float>();
	}

	#endregion
}
