using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIFlightController : MonoBehaviour
{
	Rigidbody rb;
	Vector3 targetPosition;
	float timeSinceLastTarget = 0f;
	Vector3 lastPosition;
	float timeSinceLastMovement = 0f;

	[Header("Player Following")]
	[SerializeField] Transform playerTransform;
	[SerializeField] float followProbability = 0.3f;

	[Header("Obstacle Avoidance")]
	[SerializeField] private LayerMask obstacleMask;
	[SerializeField] private float avoidanceDistance = 10f;
	[SerializeField] private float maxAvoidanceAngle = 45f;

	[Header("Flight Parameters")]
	[SerializeField] private float maxSpeed = 100f;
	[SerializeField] private float turnSpeed = 2f;
	[SerializeField] private float targetChangeInterval = 5f;
	[SerializeField] private float areaRadius = 1000f;

	[Header("Cost Parameters")]
	[SerializeField] private float upCostMultiplier = 5f;
	[SerializeField] private float horizontalCostMultiplier = 1f;
	[SerializeField] private float downCostMultiplier = 0.5f;
	[SerializeField] private float groundProximityCostMultiplier = 10f;
	[SerializeField] private float minAltitude = 10f;

	[Header("Stuck Detection")]
	[SerializeField] private float stuckThreshold = 1f;
	[SerializeField] private float stuckTimeThreshold = 3f;

	[Header("Lift Parameters")]
	[SerializeField] private float minLift = 0.5f;
	[SerializeField] private float maxLift = 2.0f;
	[SerializeField] private float minMass = 1.0f;
	[SerializeField] private float maxMass = 2.0f;
	[SerializeField] private float airDensity = 1.225f;
	[SerializeField] private float wingArea = 10.0f;

	[Header("Velocity Parameters")]
	[SerializeField] private float maxVelocity = 100.0f;

	private float liftCoefficient;
	private Vector3 liftForce;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		SetRandomTarget();
		lastPosition = transform.position;
	}

	private void FixedUpdate()
	{
		UpdateAI();
		UpdateFlightParameters();
		ApplyForces();
		ApplyLift();
		AlignVelocityWithForwardDirection();
		CapVelocity();
		CheckIfStuck();
	}

	#region AI Logic

	private void UpdateAI()
	{
		timeSinceLastTarget += Time.fixedDeltaTime;

		Vector3 targetDirection = targetPosition - transform.position;
		float angleToTarget = Vector3.Angle(transform.forward, targetDirection);
		float distanceToTarget = targetDirection.magnitude;

		bool movingTowardsPlayer = Vector3.Dot(transform.forward, targetDirection.normalized) > 0;

		if (movingTowardsPlayer)
		{
			RotateTowardsTarget(targetDirection);
		}
		else
		{
			AvoidObstacles(targetDirection, angleToTarget);
		}

		if (distanceToTarget < 100f || timeSinceLastTarget > targetChangeInterval)
		{
			SetRandomTarget();
			timeSinceLastTarget = 0f;
		}
	}

	private void RotateTowardsTarget(Vector3 targetDirection)
	{
		Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
		rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
	}

	private void AvoidObstacles(Vector3 targetDirection, float angleToTarget)
	{
		if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, avoidanceDistance, obstacleMask))
		{
			Vector3 avoidanceDirection = Vector3.Cross(transform.up, hit.normal).normalized;
			Quaternion avoidanceRotation = Quaternion.LookRotation(avoidanceDirection);
			rb.MoveRotation(Quaternion.Slerp(transform.rotation, avoidanceRotation, turnSpeed * Time.fixedDeltaTime));
		}
		else if (angleToTarget > maxAvoidanceAngle)
		{
			RotateTowardsTarget(targetDirection);
		}
	}

	private void SetRandomTarget()
	{
		if (playerTransform != null && Random.value < followProbability)
		{
			targetPosition = playerTransform.position;
		}
		else
		{
			targetPosition = FindBestRandomTarget();
		}
	}

	private Vector3 FindBestRandomTarget()
	{
		Vector3 bestTarget = Vector3.zero;
		float bestCost = float.MaxValue;

		for (int i = 0; i < 2; i++)
		{
			Vector3 randomDirection = Random.insideUnitSphere * areaRadius;
			randomDirection.y = Mathf.Abs(randomDirection.y);
			Vector3 potentialTarget = transform.position + randomDirection;

			float verticalMovement = potentialTarget.y - transform.position.y;
			float cost = CalculateMovementCost(verticalMovement);
			cost += CalculateGroundProximityCost(potentialTarget);

			if (cost < bestCost)
			{
				bestCost = cost;
				bestTarget = potentialTarget;
			}
		}

		return bestTarget;
	}

	private float CalculateMovementCost(float verticalMovement)
	{
		if (verticalMovement > 0)
		{
			return verticalMovement * upCostMultiplier;
		}
		else if (verticalMovement < 0)
		{
			return -verticalMovement * downCostMultiplier;
		}
		else
		{
			return horizontalCostMultiplier;
		}
	}

	private float CalculateGroundProximityCost(Vector3 target)
	{
		if (Physics.Raycast(target, Vector3.down, out RaycastHit hit))
		{
			float distanceToGround = hit.distance;
			if (distanceToGround < minAltitude)
			{
				return (minAltitude - distanceToGround) * groundProximityCostMultiplier;
			}
		}
		return 0f;
	}

	#endregion

	#region Flight Mechanics

	private void UpdateFlightParameters()
	{
		rb.mass = Mathf.Lerp(minMass, maxMass, rb.velocity.magnitude / 100f);
		liftCoefficient = Mathf.Lerp(minLift, maxLift, rb.velocity.magnitude / 175.0f);
	}

	private void ApplyForces()
	{
		rb.AddRelativeForce(Vector3.forward * maxSpeed * Time.fixedDeltaTime);
	}

	private void ApplyLift()
	{
		float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
		float gravitationalForce = rb.mass * Physics.gravity.magnitude;
		float liftForceMagnitude = 0.5f * airDensity * forwardSpeed * forwardSpeed * wingArea * liftCoefficient;

		float minSpeedForLift = 34f;
		float speedFactor = Mathf.Clamp01((forwardSpeed - minSpeedForLift) / minSpeedForLift);
		liftForceMagnitude *= speedFactor;
		liftForceMagnitude = Mathf.Min(liftForceMagnitude, gravitationalForce);

		Vector3 liftDirection = transform.up;
		liftForce = liftDirection * liftForceMagnitude;

		rb.AddForce(liftForce);
	}

	private void AlignVelocityWithForwardDirection()
	{
		float gravitationalForce = rb.mass * Physics.gravity.magnitude;
		bool isLiftSufficient = liftForce.magnitude / 5.0f >= gravitationalForce;

		float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
		float alignmentSpeed = isLiftSufficient ? 0.1f : 0.01f;
		Vector3 alignedVelocity = Vector3.Lerp(rb.velocity, transform.forward * forwardSpeed, alignmentSpeed);

		rb.velocity = alignedVelocity;
	}

	private void CapVelocity()
	{
		if (rb.velocity.magnitude > maxVelocity)
		{
			rb.velocity = rb.velocity.normalized * maxVelocity;
		}
	}

	#endregion

	#region Stuck Detection

	private void CheckIfStuck()
	{
		if (Vector3.Distance(transform.position, lastPosition) < stuckThreshold)
		{
			timeSinceLastMovement += Time.fixedDeltaTime;
			if (timeSinceLastMovement > stuckTimeThreshold)
			{
				transform.rotation = Quaternion.LookRotation(-transform.forward);
				timeSinceLastMovement = 0f;
			}
		}
		else
		{
			timeSinceLastMovement = 0f;
		}
		lastPosition = transform.position;
	}

	#endregion
}
