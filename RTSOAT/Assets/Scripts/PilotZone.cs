using UnityEngine;

class PilotZone : MonoBehaviour
{
	public GameObject vehicle_object;
	public Vehicle vehicle_component;
	public GameObject surface;
	public GameObject exit;
	// pilot information is handled inside the Vehicle component itself.

	void Awake()
	{
		Transform parent = gameObject.transform.parent;
		while (vehicle_object == null)
		{
			if (parent.gameObject.GetComponent<Vehicle>() != null)
			{
				vehicle_object = parent.gameObject;
				vehicle_component = vehicle_object.GetComponent<Vehicle>();
			}
			else
			{
				Debug.Assert(parent.transform.parent != null, "pilot_zone has no vehicle parent.");
				parent = parent.transform.parent;
			}
		}
		if (surface == null)
		{
			if (transform.parent.gameObject.GetComponent<Surface>() != null) {surface = transform.parent.gameObject;}
			else {Debug.Assert(false, "Pilot zone for vehicle " + vehicle_object.name + " is not the child of a surface, apparently?");}
		}
		if (exit == null) {exit = transform.Find("pilot_exit").gameObject;}
		Debug.Assert(exit != null, "no exit found for pilot zone of " + vehicle_object.name);
	}
	public Vector3 getPilotPosition()
	{
		return gameObject.transform.position;
	}

	public Vector3 getExitPosition()
	{
		return exit.transform.position;
	}

	public GameObject getVehicleObject()
	{
		return vehicle_object;
	}

	public Vehicle getVehicleComponent()
	{
		return vehicle_component;
	}

	public bool isOccupied()
	{
		return vehicle_component.hasPilot();
	}
}