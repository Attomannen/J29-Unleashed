using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FlightController))]
public class PlayerController : MonoBehaviour
{
	static PlayerController _instance = null;
	public static PlayerController Instance { get { return _instance; } }

	private FlightController flightController = null;
	private WeaponController weaponController = null;
	private Rigidbody rb                      = null;


	float maxVelocity = 200f;


	[Header("FOV")]
	[SerializeField] float aimFOV       = 50f;
	[SerializeField] float fovLerpSpeed = 1.5f;
	[SerializeField] float minFOV       = 45f;
	[SerializeField] float maxFOV       = 65f;
	private float targetFillAmount      = 0.0f;
	private float targetFOV             = 0.0f;

	[Header("UI")]
	[SerializeField] TextMeshProUGUI velocityText = null;
	[SerializeField] TextMeshProUGUI throttleText = null;
	[SerializeField] Image healthBar              = null;

	[Header("Audio")]
	[SerializeField] AudioSource thrusterSource = null;
	[SerializeField] float startVolume = 0.05f;
	[SerializeField] float endVolume   = 0.25f;

	[SerializeField] float startPitch = 0.2f;
	[SerializeField] float endPitch   = 1.5f;

	[Header("Quest")]
	public int destroyedEnemies = 0;
	bool hasCompletedQuest = false;
	[SerializeField] GameObject[] quests;

	private void Awake()
	{
		_instance = this;
	}

	private void Start()
	{
		flightController = GetComponent<FlightController>();
		weaponController = GetComponent<WeaponController>();

		flightController.InitializePlayer(InputHandler.Instance);
		weaponController.InitializePlayer(InputHandler.Instance);
		Cursor.lockState = CursorLockMode.Locked;
		rb = GetComponent<Rigidbody>();
		targetFOV = Camera.main.fieldOfView;
	}

	private void Update()
	{
		velocityText.text = $"{rb.velocity.magnitude:F0} m/s";
		throttleText.text ="Throttle: " + $"{flightController.GetThrottle():F0}%";
		ChangeHealthbar();

		if (InputHandler.Instance.GetAim() <= 0.0f)
		{
			ChangeFOV();
		}
		else
		{
			Aim();
		}

		UpdateFOV();
		ThrusterChangeAudio();
		if (destroyedEnemies == 12 && !hasCompletedQuest)
		{
			quests[0].GetComponent<QuestGiver>().CompleteQuest();
			hasCompletedQuest = true;
		}
	}

	private void ChangeFOV()
	{
		float velocityMagnitude = rb.velocity.magnitude;
		float t = Mathf.Clamp01(velocityMagnitude / maxVelocity);
		targetFOV = Mathf.Lerp(minFOV, maxFOV, t);
	}

	private void ChangeHealthbar()
	{
		float currentHealth = GetComponent<Health>().GetHealth();
		targetFillAmount = currentHealth / 100.0f;
		healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, targetFillAmount, Time.deltaTime * 6.0f);
	}
	private void ThrusterChangeAudio()
	{
		if (!thrusterSource)
			return;

		thrusterSource.pitch = Mathf.Lerp(startPitch, endPitch, flightController.GetThrottle() / 100f);
		thrusterSource.volume = Mathf.Lerp(startVolume, endVolume, flightController.GetThrottle() / 100f);
	}
	private void Aim()
	{
		targetFOV = aimFOV;
	}

	private void UpdateFOV()
	{
		Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
	}

	private void OnDisable()
	{
		if(healthBar)
			healthBar.fillAmount = 0;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "TurretActivation")
		{
			other.transform.GetChild(0).gameObject.SetActive(true);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.tag == "TurretActivation")
		{
			other.transform.GetChild(0).gameObject.SetActive(false);
		}
	}


}
