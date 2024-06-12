using UnityEngine;

public class MissileController : MonoBehaviour
{
	[SerializeField] private GameObject explosionParticle = null;

	[Header("Sphere casting")]
	[SerializeField] private float sphereCastDistance  = 10.0f;
	[SerializeField] private float radius              = 1.0f;
	[SerializeField] private LayerMask collisionLayers = 0;

	[Header("Missile Parameters")]
	[SerializeField] private float explosionForce               = 250.0f;
	[SerializeField] private float initialAccelerationTime      = 1.0f;
	[SerializeField] private float maxTurnSpeed                 = 0.35f;
	[SerializeField] private float outOfAngleExplosionTime      = 1.0f;
	[SerializeField] private float distanceBeforeSelfDetonation = 5.0f;
	[SerializeField] private float maxAngleToTarget             = 45.0f;

	private GameObject target      = null;
	private GameObject instigator  = null;
	private Rigidbody rb           = null;

	private Vector3 playerVelocity = Vector3.zero;

	private float currentSpeed     = 0.0f;
	private float timer            = 0.0f;
	private float outOfAngleTimer  = 0.0f;

	public void SetInstigator(GameObject instigatorObject)
	{
		instigator = instigatorObject;
	}

	private void Start()
	{
		InitializeTarget();
		InitializeRigidbody();
		InitializeSpeed();
	}

	private void Update()
	{
		HandleMovement();
	}

	private void InitializeTarget()
	{
		if (instigator.GetComponent<InputHandler>() != null)
		{
			target = TargetingController.Instance.GetTargetInRange(instigator.transform.position, 2000.0f, 0.25f);
		}
		else
		{
			var weaponController = instigator.GetComponentInParent<WeaponController>();
			target = TargetingController.Instance.GetTargetInRange(instigator.transform.position, weaponController.cannonConfig.convergenceDistance, instigator);
		}
	}

	private void InitializeRigidbody()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void InitializeSpeed()
	{
		var weaponController = instigator.GetComponentInParent<WeaponController>();
		currentSpeed = weaponController.GetMissileSpeed();
	}

	private void HandleMovement()
	{
		timer += Time.deltaTime;

		if (timer < initialAccelerationTime)
		{
			if (timer > initialAccelerationTime / 2.0f)
			{
				MoveForward();
			}
			return;
		}

		if (target != null)
		{
			HandleTargetMovement();
		}
		else
		{
			HandleNoTargetMovement();
		}

		HandleSphereCastCollision();
	}

	private void HandleTargetMovement()
	{
		float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
		if (distanceToTarget <= distanceBeforeSelfDetonation)
		{
			Explode();
			return;
		}

		if (IsTargetWithinAngle())
		{
			MoveToTarget();
		}
		else
		{
			outOfAngleTimer += Time.deltaTime;
			if (outOfAngleTimer >= outOfAngleExplosionTime)
			{
				Explode();
			}
		}

		if (timer - initialAccelerationTime >= 5.65f)
		{
			timer = 0.0f;
			target = null;
		}
	}

	private void HandleNoTargetMovement()
	{
		if (timer - initialAccelerationTime >= 2.5f)
		{
			Explode();
		}
	}

	private void HandleSphereCastCollision()
	{
		RaycastHit hit;
		if (Physics.SphereCast(transform.position, radius, transform.forward, out hit, sphereCastDistance, collisionLayers))
		{
			if (hit.collider != null)
			{
				Explode();
			}
		}
	}

	private void MoveToTarget()
	{
		outOfAngleTimer = 0f;

		Vector3 interceptDirection = CalculateInterceptCourse(target.transform.position, GetTargetVelocity(), transform.position, rb.velocity.magnitude);

		rb.velocity = Vector3.RotateTowards(rb.velocity, interceptDirection * currentSpeed + playerVelocity, maxTurnSpeed * Time.deltaTime, 0.0f);
	}

	private Vector3 GetTargetVelocity()
	{
		Rigidbody targetRigidbody = target.GetComponent<Rigidbody>();
		return targetRigidbody != null ? targetRigidbody.velocity : Vector3.zero;
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

	public static Vector3 CalculateInterceptCourse(Vector3 targetPos, Vector3 targetSpeed, Vector3 interceptorPos, float interceptorSpeed)
	{
		Vector3 targetDir = targetPos - interceptorPos;

		float iSpeedSquared = interceptorSpeed * interceptorSpeed;
		float tSpeedSquared = targetSpeed.sqrMagnitude;
		float forwardDot = Vector3.Dot(targetDir, targetSpeed);
		float targetDist = targetDir.sqrMagnitude;
		float d = (forwardDot * forwardDot) - targetDist * (tSpeedSquared - iSpeedSquared);

		if (d < 0.0f)
		{
			return targetDir.normalized;
		}

		float sqrt = Mathf.Sqrt(d);
		float S1 = (-forwardDot - sqrt) / targetDist;
		float S2 = (-forwardDot + sqrt) / targetDist;

		float S = Mathf.Max(S1, S2);
		return (targetDir * S + targetSpeed).normalized;
	}

	private void MoveForward()
	{
		rb.velocity = (transform.forward * currentSpeed + playerVelocity) / 1.5f;
	}

	private void Explode()
	{
		if (explosionParticle != null)
		{
			Instantiate(explosionParticle, transform.position, Quaternion.identity);
		}

		Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
		foreach (Collider hit in colliders)
		{
			Rigidbody hitRigidbody = hit.GetComponentInParent<Rigidbody>();
			if (hitRigidbody != null)
			{
				hitRigidbody.AddExplosionForce(explosionForce, transform.position, radius);
			}
		}
		Destroy(gameObject);
	}

	private void OnCollisionEnter(Collision collision)
	{
		Explode();
	}
}
