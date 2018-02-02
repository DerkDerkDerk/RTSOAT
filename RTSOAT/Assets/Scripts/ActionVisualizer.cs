// ActionVisualizer.cs

// This class handles the dynamic HUD display for movement and other actions as the player moves their mouse.
// Unit path visualization will likely mean drawing a path from A to B, similar to XCOM.
// Vehicle path visualization will likely be linear, but a 3D "ghost" fo the vehicle will be drawn where the destination is.
// The "vehicle ghost" will not more 1-to-1 with the mouse! Its position is interpolated to allow fine-tuned movement.


using UnityEngine;

class ActionVisualizer : MonoBehaviour
{
	public string mode = "vehicle";
	public GameObject ghost_prefab;
	public GameObject ghost;

	public void displayVehiclePath(Vector3 pos, Vehicle vehicle_comp)
	{
		ghost = vehicle_comp.ghost;
		if (!ghost.activeSelf)
		{
			ghost.SetActive(true);
		}
		else
		{
			Debug.Assert(ghost.activeInHierarchy, "This ghost is enabled, but its parent is disabled (so the ghost is also effectively disabled). Why are we here?");
		}
		ghost.transform.position = pos;
	}

	public void hideVehicleGhost(Vehicle vehicle_comp)
	{
		vehicle_comp.ghost.SetActive(false);
	}
}