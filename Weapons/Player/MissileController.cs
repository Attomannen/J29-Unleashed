using UnityEngine;

public class MissileController : MonoBehaviour
{
	GameObject target = null;
	[SerializeField] Rigidbody rb = null;
	float currentSpeed = 0.0f;
	Vector3 playerVelocity = Vector3.zero;
	float timer = 0.0f;

	[SerializeField] GameObject ExplosionParticle;
	[SerializeField] float radius = 1.0f;
	[SerializeField] LayerMask collisionLayers;
	[SerializeField] float explosionForce = 250.0f;
	[SerializeField] float sphereCastDistance = 10.0f;
	[SerializeField] float initialAccelerationTime = 1.0f;
	[SerializeField] float maxTurnSpeed = 0.35f;
	[SerializeField] float outOfAngleExplosionTime = 1.0f;
	float outOfAngleTimer = 0.0f;

	[SerializeField] float maxAngleToTarget = 45.0f;
	WeaponController weaponController = null;
	TargetingController targetFinder;

	void Start()
	{
		GameObject player = GameObject.FindGameObjectWithTag("Player");
		if (player != null)
		{
			playerVelocity = player.GetComponentInParent<Rigidbody>().velocity;
			weaponController = player.GetComponentInParent<WeaponController>();
			targetFinder = player.GetComponentInParent<TargetingController>();
			target = targetFinder.GetTargetInRange(weaponController.cannonConfig.convergenceDistance * 2.0f, 0.25f);
		}
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		currentSpeed = weaponController.GetMissileSpeed();

	}
	void Update()
	{
		timer += Time.deltaTime;
		if (timer < initialAccelerationTime)
		{
			MoveForward();
			return;
		}
		else
		{

			if (target != null)
			{
				float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
				if (distanceToTarget <= 5.0f)
				{
					Explode();
					return;
				}

				if (IsTargetWithinAngle())
				{
					outOfAngleTimer = 0f;

					Vector3 targetPosition = target.transform.position;
					Rigidbody targetRigidbody = target.GetComponent<Rigidbody>();
					if (targetRigidbody != null)
					{
						Vector3 targetVelocity = targetRigidbody.velocity;

						Vector3 interceptDirection = CalculateInterceptCourse(targetPosition, targetVelocity, transform.position, rb.velocity.magnitude);
						rb.velocity = Vector3.RotateTowards(rb.velocity, interceptDirection * currentSpeed + playerVelocity, maxTurnSpeed * Time.deltaTime, 0.0f);
					}
				}
				else
				{
					if (outOfAngleTimer >= outOfAngleExplosionTime)
					{
						Explode();
					}
					else
					{
						outOfAngleTimer += Time.deltaTime;
					}
				}

				if (timer - initialAccelerationTime >= 5.65f)
				{
					timer = 0.0f;
					target = null;
				}
			}
			else
			{
				if (timer - initialAccelerationTime >= 2.5f)
				{
					Explode();
				}
			}

			RaycastHit hit;
			if (Physics.SphereCast(transform.position, radius, transform.forward, out hit, sphereCastDistance, collisionLayers))
			{
				if (hit.collider != null)
				{
					Explode();
				}
			}
		}

	}

	void Explode()
	{
		if (ExplosionParticle != null)
		{
			Instantiate(ExplosionParticle, transform.position, Quaternion.identity);
		}


		Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
		foreach (Collider hit in colliders)
		{
			Rigidbody hitRigidbody = hit.GetComponent<Rigidbody>();
			if (hitRigidbody != null)
			{
				hitRigidbody.AddExplosionForce(explosionForce, transform.position, radius);
			}
		}
		Destroy(gameObject);
	}

	private void MoveForward()
	{
		rb.velocity = (transform.forward  * currentSpeed + playerVelocity) / 1.5f;
	}

	private bool IsTargetWithinAngle()
	{
		if (target != null)
		{
			Vector3 targetDirection = (target.transform.position - transform.position).normalized;
			float angle = Vector3.Angle(targetDirection, transform.forward);
			return angle <= maxAngleToTarget;
		}
		return false;
	}

	public static Vector3 CalculateInterceptCourse(Vector3 _targetPos, Vector3 _targetSpeed, Vector3 _interceptorPos, float _interceptorSpeed)
	{
		Vector3 targetDir = _targetPos - _interceptorPos;

		float iSpeedSquared = _interceptorSpeed * _interceptorSpeed;
		float tSpeedSquared = _targetSpeed.sqrMagnitude;
		float forwardDot = Vector3.Dot(targetDir, _targetSpeed);
		float targetDist = targetDir.sqrMagnitude;
		float d = (forwardDot * forwardDot) - targetDist * (tSpeedSquared - iSpeedSquared);

		if (d < 0.0f)
			return targetDir.normalized;

		float sqrt = Mathf.Sqrt(d);
		float S1 = (-forwardDot - sqrt) / targetDist;
		float S2 = (-forwardDot + sqrt) / targetDist;

		float S = Mathf.Max(S1, S2);
		return (targetDir * S + _targetSpeed).normalized;
	}
	void OnCollisionEnter(Collision collision)
	{
		Explode();
	}
}
