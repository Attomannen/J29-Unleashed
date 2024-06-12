using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetingController : MonoBehaviour
{
	List<GameObject> targetList = new List<GameObject>();

	private static TargetingController instance;

	public static TargetingController Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<TargetingController>();
				if (instance == null)
				{
					Debug.LogError("TargetingController instance is null and it couldn't be found in the scene.");
				}
			}
			return instance;
		}
	}

	public void RegisterTarget(GameObject target)
	{
		targetList.Add(target);
	}

	public void RemoveTarget(GameObject target)
	{
		targetList.Remove(target);
	}

	#region Camera Based
	public GameObject GetTargetInRange(Vector3 targeterPos, float rangeThreshold, float viewportThreshold)
	{
		GameObject bestTarget = null;
		float bestScore = float.MaxValue;

		Camera mainCamera = Camera.main;

		foreach (GameObject target in targetList)
		{
			if (target == null || target == InputHandler.Instance.gameObject)
				continue;

			float distance = Vector3.Distance(targeterPos, target.transform.position);
			if (distance < rangeThreshold)
			{
				Vector3 viewportPoint = mainCamera.WorldToViewportPoint(target.transform.position);
				if (IsInViewport(viewportPoint, viewportThreshold))
				{
					float deviation = Vector2.Distance(new Vector2(viewportPoint.x, viewportPoint.y), new Vector2(0.5f, 0.5f));

					float score = distance + deviation * rangeThreshold;

					if (score < bestScore)
					{
						bestScore = score;
						bestTarget = target;
					}
				}
			}
		}
		return bestTarget;
	}

	public bool IsInViewport(Vector3 viewportPoint, float viewportThreshold)
	{
		float viewportCenterX = 0.5f;
		float viewportCenterY = 0.5f;
		return Mathf.Abs(viewportPoint.x - viewportCenterX) <= viewportThreshold && Mathf.Abs(viewportPoint.y - viewportCenterY) <= viewportThreshold && viewportPoint.z > 0;
	}
	#endregion

	public GameObject GetTargetInRange(Vector3 targeterPos, float rangeThreshold, GameObject excluder)
	{
		GameObject closestTarget = null;
		GameObject closestNonExcluderTarget = null;
		float closestDistance = rangeThreshold;

		foreach (GameObject target in targetList)
		{
			if (target == null || target == excluder || target.GetComponent<Rigidbody>() == null)
				continue;

			float distance = Vector3.Distance(targeterPos, target.transform.position);
			if (distance < closestDistance)
			{
				if (closestNonExcluderTarget == null || target != excluder)
				{
					closestNonExcluderTarget = target;
				}
				closestTarget = target;
				closestDistance = distance;
			}
		}

		return closestNonExcluderTarget != null ? closestNonExcluderTarget : closestTarget;
	}

	public static Vector3 InterceptLead(Vector3 shooterPosition,
		Vector3 shooterVelocity,
		float shotSpeed,
		Vector3 targetPosition,
		Vector3 targetVelocity)
	{
		Vector3 relTargetPos = targetPosition - shooterPosition;

		Vector3 relTargetVel = targetVelocity - shooterVelocity;

		float t = InterceptTime(shotSpeed, relTargetPos, relTargetVel);
		return targetPosition + t * (relTargetVel);
	}

	public static float InterceptTime(float projectileSpeed, Vector3 targetRelativePosition, Vector3 targetRelativeVelocity)
	{
		float targetVelocityMagnitudeSquared = targetRelativeVelocity.sqrMagnitude;

		if (targetVelocityMagnitudeSquared < 0.001f)
		{
			return 0f;
		}
		float a = targetVelocityMagnitudeSquared - projectileSpeed * projectileSpeed;

		if (Mathf.Abs(a) < 0.001f)
		{
			float interceptTime = -targetRelativePosition.sqrMagnitude / (2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition));
			return Mathf.Max(interceptTime, 0f);
		}

		float b = 2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition);
		float c = targetRelativePosition.sqrMagnitude;
		float determinant = b * b - 4f * a * c;

		if (determinant > 0f)
		{
			float interceptTime1 = (-b + Mathf.Sqrt(determinant)) / (2f * a);
			float interceptTime2 = (-b - Mathf.Sqrt(determinant)) / (2f * a);

			if (interceptTime1 > 0f)
			{
				if (interceptTime2 > 0f)
					return Mathf.Min(interceptTime1, interceptTime2);
				else
					return interceptTime1;
			}
			else
			{
				return Mathf.Max(interceptTime2, 0f);
			}
		}
		else if (determinant < 0f)
		{
			return 0f;
		}
		else
		{
			return Mathf.Max(-b / (2f * a), 0f);
		}
	}
}
