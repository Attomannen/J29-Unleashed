using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingController : MonoBehaviour
{
	public GameObject GetTargetInRange(float rangeThreshold, float viewportThreshold)
	{
		GameObject[] targets = GameObject.FindGameObjectsWithTag("Target");
		GameObject closestTarget = null;
		float closestDistance = rangeThreshold;

		Camera mainCamera = Camera.main;

		foreach (GameObject target in targets)
		{
			float distance = Vector3.Distance(transform.position, target.transform.position);
			if (distance < closestDistance)
			{
				Vector3 viewportPoint = mainCamera.WorldToViewportPoint(target.transform.position);
				if (IsInViewport(viewportPoint, viewportThreshold))
				{
					closestTarget = target;
					closestDistance = distance;
				}
			}
		}

		if (closestTarget == null)
		{
			Debug.LogWarning("No target found within range.");
		}
		return closestTarget;
	}

	public bool IsInViewport(Vector3 viewportPoint, float viewportThreshold)
	{
		float viewportCenterX = 0.5f;
		float viewportCenterY = 0.5f;
		return Mathf.Abs(viewportPoint.x - viewportCenterX) <= viewportThreshold && Mathf.Abs(viewportPoint.y - viewportCenterY) <= viewportThreshold && viewportPoint.z > 0;
	}

	public static Vector3 InterceptLead(Vector3 shooterPosition, Vector3 shooterVelocity, float shotSpeed, Vector3 targetPosition, Vector3 targetVelocity)
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
