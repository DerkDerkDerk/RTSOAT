using UnityEngine;

class GameController : MonoBehaviour
{
	public GameObject selected_unit;
	public Unit selected_unit_comp;
	private Camera cam;
	public Vector3 av_pos;
    private bool av_pos_fresh = false;
    public ActionVisualizer av;

	public GameObject treadmill_obj;

	void Awake()
	{
		cam = Camera.main; // it's possible this may change to a temporary close-up camera at times.
		av = GetComponent<ActionVisualizer>();

		if (treadmill_obj == null){treadmill_obj = GameObject.Find("Treadmill");}
	}

	void Update()
	{
        av_pos_fresh = false;
        handleMouseInput();

        // display vehicle path
        if (selected_unit_comp != null && av_pos != null)
        {
        	if (selected_unit_comp.vehicle_piloting_comp != null)
        	{
                if (av_pos_fresh)
                {
                    av.displayVehiclePath(av_pos, selected_unit_comp.vehicle_piloting_comp);
                }
        		else
                {
                    av.hideVehicleGhost(selected_unit_comp.vehicle_piloting_comp);
                }
        	}
        }
    }
 
    void handleMouseInput()
    {
        ///// CLICKING INPUT //////

    	// Left Click in the world is Select
    	if (Input.GetMouseButtonDown(0))
    	{
    		RaycastHit hit;

			if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit))
			{
				// Debug.Log("Left clicked " + hit.collider.gameObject.name + ".");
				if (hit.collider.gameObject.GetComponent<Unit>() != null)
				{
					// Debug.Log("Left clicked " + hit.collider.gameObject.name + ".");
					selectUnit(hit.collider.gameObject);
				}
			}
    	}
    	// Right Click in the world is Order
    	if (Input.GetMouseButtonDown(1))
        {
			//Debug.Log("Pressed right click.");

			RaycastHit hit;

			if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit))
			{
				if (selected_unit_comp != null)
				{
					selected_unit_comp.handle_order(hit.point, hit.collider.gameObject);
				}
				else
				{
					Debug.Log("Right clicked " + hit.collider.gameObject.name + ", but no unit is selected.");
				}
			}
			else{
				Debug.Assert(false, "Right click did not hit any collider");
			}
        }

        //////// POSITIONAL INPUT /////////////
        
        // Display destination location for vehicle, given current mouse position
        if (selected_unit_comp != null)
        {
        	RaycastHit hit;
        	if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit))
        	{
        		if (selected_unit_comp.vehicle_piloting_object != null && hit.collider.gameObject == treadmill_obj)
        		{
        			av_pos = selected_unit_comp.vehicle_piloting_comp.getActualDest(hit.point);
                    av_pos_fresh = true;
        		}
        	}
        }
    }

    void selectUnit(GameObject u)
    {
    	if (selected_unit != null)
    	{
    		deselectUnit(u);
    	}
    	selected_unit = u;
    	selected_unit_comp = u.GetComponent<Unit>();
    	Debug.Assert(selected_unit_comp != null, "attempted to select an object which is not a unit!");
    	selected_unit_comp.performSelectionBehavior();

    }

    void deselectUnit(GameObject u)
    {
    	selected_unit_comp.performDeselectionBehavior();
    	selected_unit = null;
    	selected_unit_comp = null;
    }
}