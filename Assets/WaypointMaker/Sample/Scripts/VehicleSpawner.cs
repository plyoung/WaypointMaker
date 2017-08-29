using UnityEngine;


public class VehicleSpawner : MonoBehaviour
{
	[SerializeField] private GameObject fab = null;
	[SerializeField] private Material[] mats = new Material[0];
	[SerializeField] private int maxVehicles = 10;

	private void Start()
	{
		// spawn a few cars
		for (int i = 0; i < maxVehicles; i++)
		{
			Transform tr = Instantiate(fab).transform;
			tr.GetChild(0).GetComponent<Renderer>().material = mats[Random.Range(0, mats.Length)];
		}
	}
}
