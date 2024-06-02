using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyWeaponController : MonoBehaviour
{
	float CannonCooldownTimer;
	int currentMuzzleIndex = 0;
	Vector3 convergencePoint = Vector3.zero;
	[System.Serializable]
	public class Cannon
	{
		public List<Transform> muzzlePositions = new List<Transform>();
		public GameObject projectilePrefab;
		public int numProjectilesPerMuzzle = 1;
		public float projectileSpeed = 100.0f;
		public float convergenceDistance = 500.0f;
		public float CannonCooldown = 1.0f;
	}
	public Cannon _Cannon;

	void Start()
	{
		UpdateConvergencePoint();
	}
	void Update()
	{
		UpdateConvergencePoint();

		float randomValue = Random.value; 


		if (randomValue > 0.5f)
		{
			FireCannons();
		}

		if (CannonCooldownTimer > 0)
		{
			CannonCooldownTimer -= Time.deltaTime;
		}

	}
	void UpdateConvergencePoint()
	{

		RaycastHit hit;
		Vector3 rayDirection = transform.forward;
		if (Physics.Raycast(transform.position, rayDirection, out hit, _Cannon.convergenceDistance))
		{
			convergencePoint = hit.point;
		}
		else
		{
			convergencePoint = transform.position + rayDirection * _Cannon.convergenceDistance;
		}

	}

	void FireCannons()
	{
		if (_Cannon.projectilePrefab == null)
		{
			return;
		}

		if (CannonCooldownTimer > 0)
		{
			return;
		}

		CannonCooldownTimer = _Cannon.CannonCooldown;

		if (_Cannon.muzzlePositions.Count == 0)
		{
			return;
		}

		Transform muzzle = _Cannon.muzzlePositions[currentMuzzleIndex];

		Vector3 directionToConvergencePoint = (convergencePoint - muzzle.position).normalized;

		for (int i = 0; i < _Cannon.numProjectilesPerMuzzle; i++)
		{
			Vector3 projectileVelocity = directionToConvergencePoint * _Cannon.projectileSpeed;

			GameObject projectile = Instantiate(_Cannon.projectilePrefab, muzzle.position, Quaternion.LookRotation(projectileVelocity));

			Rigidbody rb = projectile.GetComponent<Rigidbody>();
			if (rb != null)
			{
				rb.velocity = projectileVelocity + gameObject.GetComponent<Rigidbody>().velocity;
			}
		}

		currentMuzzleIndex = (currentMuzzleIndex + 1) % _Cannon.muzzlePositions.Count;
	}

}
