using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class FlightController : MonoBehaviour
{
	Rigidbody rb                  = null;
	PhotonView view               = null;
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
	[SerializeField] float throttleChangeSpeed      = 0.0f;
	[SerializeField, Range(0, 100f)] float throttle = 0f;
	[SerializeField] float throttleMultiplier       = 0f;
	[SerializeField] float divingSpeed              = 100f;

	[Header("Roll")]
	[SerializeField] float rollMultiplier = 0f;

	[Header("Pitch")]
	[SerializeField] float pitchMultiplier = 0f;
	[SerializeField] float pitchFactor     = 1f;

	[Header("Yaw")]
	[SerializeField] float yawMultiplier = 0f;

	[Header("Canvas")]
	[SerializeField] TextMeshProUGUI throttleText;
	[SerializeField] TextMeshProUGUI velocityText;

	[Header("Mass Scaling")]
	[SerializeField] float minMass = 600f;
	[SerializeField] float maxMass = 10f;

	[Header("Drag Scaling")]
	[SerializeField] float minDrag = 600f;
	[SerializeField] float maxDrag = 10f;

	[Header("FOV Scaling")]
	[SerializeField] float minFOV = 600f;
	[SerializeField] float maxFOV = 10f;

	[Header("Lift Parameters")]
	[SerializeField] float maxAlignmentSpeed = 0.5f;
	[SerializeField] float minAlignmentSpeed = 0.05f;
	[SerializeField] float liftCoefficient   = 1.0f;
	[SerializeField] float airDensity        = 1.225f;
	[SerializeField] float wingArea          = 10.0f;

	[Header("Stall Parameters")]
	[SerializeField] float stallTimeThreshold = 2f;

	[Header("Velocity Parameters")]
	[SerializeField] float maxVelocity = 250f;

	[Header("Sensitivity Parameters")]
	[SerializeField] float pitchSensitivity = 0.1f;
	[SerializeField] float rollSensitivity  = 0.2f;

	[Header("Landing Parameters")]
	[SerializeField] GameObject[] landingGears = null;

	public InputType currentInput = 0;
	public enum InputType { Keyboard, Controller, Mouse }

	private void Start()
	{
		rb                      = GetComponent<Rigidbody>();
		startPitchFactor        = pitchFactor;
		Cursor.lockState        = CursorLockMode.Locked;
		view                    = GetComponent<PhotonView>();
	}

	private void Update()
	{
		if (view.IsMine)
		{
			HandleInput();
			UpdateFlightParameters();
			HandleMouseInput();
		}
	}

	private void FixedUpdate()
	{
		if (view.IsMine)
		{
			ApplyForces();
			ApplyLift();
			AlignVelocityWithForwardDirection();
			CheckPitchAngle();
			CapVelocity();
		}
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
			float rollInput  = Mouse.current.delta.x.ReadValue() * rollSensitivity;

			pitch = Mathf.Clamp(pitchInput, -1f, 1f);
			roll  = Mathf.Clamp(rollInput, -1f, 1f);
		}

		if (Mouse.current.middleButton.wasPressedThisFrame)
		{
			throttle = 0f;
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

		rb.AddRelativeTorque(
			(pitch + autoPitch) * pitchMultiplier * Time.deltaTime,
			yaw * yawMultiplier * Time.deltaTime,
			-roll * rollMultiplier * 2 * Time.deltaTime
		);
	}

	private void ApplyLift()
	{
		float forwardSpeed       = Vector3.Dot(rb.velocity, transform.forward);
		float gravitationalForce = rb.mass * Physics.gravity.magnitude;
		float liftForceMagnitude = 0.5f * airDensity * forwardSpeed * forwardSpeed * wingArea * liftCoefficient;

		float minSpeedForLift = 34f;
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

	private void CapVelocity()
	{
		if (rb.velocity.magnitude > maxVelocity)
		{
			rb.velocity = rb.velocity.normalized * maxVelocity;
		}
	}

	private void CheckPitchAngle()
	{
		pitchAngle = Vector3.Dot(transform.forward, Vector3.up);

		if (pitchAngle > 0.75f)
		{
			stallTimer += Time.fixedDeltaTime;

			float velocityReduction = Mathf.Lerp(0, 1, Mathf.Clamp01((pitchAngle - 0.75f) / 0.75f));
			rb.velocity             *= (1 - velocityReduction);
			float diveFactor        = Mathf.Abs(pitchAngle) * 200.0f;

			rb.AddRelativeForce(-Vector3.forward * divingSpeed * diveFactor * Time.fixedDeltaTime);

			if (stallTimer > stallTimeThreshold)
			{
				pitchFactor -= 1.0f * Time.deltaTime;
			}
		}
		else
		{
			ResetStallTimer();
		}

		if (pitchAngle <= -0.35f)
		{
			pitchFactor  = 0.0f;
			float diveFactor = Mathf.Abs(pitchAngle) * 200.0f;
			rb.AddRelativeForce(-Vector3.forward * divingSpeed * diveFactor * Time.fixedDeltaTime);
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
