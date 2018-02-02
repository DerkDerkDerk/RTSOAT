using UnityEngine;
using System.Collections.Generic;

class Vehicle : MonoBehaviour
{
	public Rigidbody rigid_body;
	public GameObject vector_bone;
	public GameObject pilot;
	public GameObject pilot_zone;
	public PilotZone pilot_zone_comp;
	public GameObject dest;
	public float movement_speed = 3.0f; // units per second
	public GameObject current_region;
	public List<GameObject> surfaces;
	public float interp_radius = 3.5f;
	public float base_weight = 2500f;

	public GameObject ghost;
	public Treadmill treadmill_comp;

	public float total_weight;
	public float current_speed;

	public Vector3 movement_last_frame = Vector3.zero;
	public Vector3 movement_this_frame = Vector3.zero;

	// public GameObject ghost_prefab;


	void Awake()
	{
		rigid_body = GetComponent<Rigidbody>();
		Debug.Assert(rigid_body != null, "Couldn't find Rigidbody");
		// create a list of all the surfaces for this vehicle
		surfaces = new List<GameObject>();
		foreach (Transform child in transform)
		{
			if (child.gameObject.GetComponent<Surface>() != null)
			{
				surfaces.Add(child.gameObject);
				child.GetComponent<Surface>().vehicle = gameObject;
			}
		}
		Debug.Assert(surfaces.Count > 0, "found no surfaces for " + gameObject.name);
		// identify the vehicle's vector bone
		if (vector_bone == null){vector_bone = transform.Find("vector_bone").gameObject;}

		// find the pilot zone for the vehicle. It must be on one of the surfaces.
		if (pilot_zone == null)
		{
			foreach (GameObject surface in surfaces)
			{
				foreach (Transform child in surface.transform)
				{
					if (child.gameObject.GetComponent<PilotZone>() != null) {pilot_zone = child.gameObject;}
					Debug.Log(child.gameObject.name);
				}
				if (pilot_zone != null) {break;}
			}
		}
		Debug.Assert(pilot_zone != null, "found no pilot zone on surfaces of " + gameObject.name);
		pilot_zone_comp = pilot_zone.GetComponent<PilotZone>();

		if (treadmill_comp == null)
		{
			GameObject t = GameObject.Find("Treadmill");
			if (t != null)
			{
				treadmill_comp = t.GetComponent<Treadmill>();
			}
		}
		Debug.Assert(treadmill_comp != null, "Could not find treadmill script.");
		
		dest = new GameObject("dest of " + gameObject.name);
		dest.transform.position = getPosition();
		dest.transform.parent = gameObject.transform;

		current_region = updateCurrentRegion();

		makeGhost();

		total_weight = base_weight;
		rigid_body.mass = total_weight;
		current_speed = 0f;
	}

	void Update()
	{
		current_region = updateCurrentRegion();
	}

	void FixedUpdate()
	{
		movement_last_frame = movement_this_frame;
		movement_this_frame = Vector3.zero;
		handleMovement();
		// current_region = updateCurrentRegion();
		// TODO fix this calculation to handle an arbitrary "forward" direction. Right now it's (-1, 0, 0)
		current_speed = (new Vector3(treadmill_comp.speed * Time.deltaTime, 0, 0) - movement_this_frame).magnitude / Time.deltaTime;
		// TODO fix speed calculation
	}

	void OnCollisionEnter(Collision c)
	{
		Hazard haz_comp = c.collider.gameObject.GetComponent<Hazard>();
		if (haz_comp)
		{
			Debug.Log(haz_comp.message);
			handleHazardCollision();
		}
		else
		{
			stopMovingToDest();
			// When it's a Side-Side collision, btoh vehicles move slightly. Smaller vehicles are moved more.

			// When it's a Front-Rear collision...
				// If the rear car was the aggressor, then the front car moves slightly. rear car doesn't move at all
				// If the front car was the aggressor, then the rear car moves significantly.
		}
	}

	public void setDest(Vector3 pos, GameObject obj)
	{
		dest.transform.parent = obj.transform;
		dest.transform.position = pos;
		Debug.Log("setDest(" + pos + "," + obj + ")");
	}

	public Vector3 getDestPos()
	{
		return dest.transform.position;
	}

	public Vector3 getPosition()
	{
		return vector_bone.transform.position;
	}

	Vector3 getObjectOffset()
	{
		return gameObject.transform.position - vector_bone.transform.position;
	}

	public GameObject getCurrentRegion()
	{
		return current_region;
	}

	GameObject updateCurrentRegion()
	{
		RaycastHit hit;
		Physics.Raycast(gameObject.transform.position, -Vector3.up, out hit);
		current_region = hit.collider.gameObject;
		return current_region;
	}

	void handleMovement()
	{
		if (!Mathf.Approximately(getPosition().x, getDestPos().x) || !Mathf.Approximately(getPosition().z, getDestPos().z))
		{
			//is_moving_to_dest = true;
			float dist_to_dest = Vector3.Distance(getPosition(), getDestPos());
			Vector3 movement_direction = (getDestPos() - getPosition()).normalized;
			float movement_distance = Mathf.Min(movement_speed * Time.fixedDeltaTime, dist_to_dest);
			movement_this_frame = movement_direction * movement_distance;
			Vector3 new_position = getPosition() + movement_this_frame + getObjectOffset();
			moveToPosition(new_position);
		}
		else
		{
			stopMovingToDest();
		}
	}

	public void stopMovingToDest()
	{
		dest.transform.parent = gameObject.transform;
		dest.transform.position = getPosition();
	}

	void moveToPosition(Vector3 pos)
	{
		// TODO account for possible collision and the results thereof
		Debug.Log(pos.ToString("F4"));
		rigid_body.MovePosition(pos);
		// gameObject.transform.position += movememnt_vector;
		
	}

	public void moveToObject(Vector3 pos, GameObject obj)
	{
		setDest(pos, obj);
	}

	public bool hasPilot()
	{
		if (pilot == null){return false;}
		else {return true;}
	}

	public float addWeight(float w)
	{
		Debug.Assert(w > 0, "Should not be adding a negative weight");
		total_weight += w;
		return total_weight;
	}

	public float removeWeight(float w)
	{
		Debug.Assert(w > 0, "Should not be removing a negative weight");
		total_weight -= w;
		return total_weight;
	}

	public Vector3 getActualDest(Vector3 mouse_pos)
	{
		float dist_from_vb = Vector3.Distance(getPosition(), mouse_pos);
		if (dist_from_vb >= interp_radius) {return mouse_pos;}

		float ratio = dist_from_vb / interp_radius;
		return Vector3.Lerp(getPosition(), mouse_pos, ratio);
	}

	private void makeGhost()
	{
		ghost = Instantiate(Resources.Load("Prefabs/Ghost") as GameObject) as GameObject;
		Ghost ghost_comp = ghost.GetComponent<Ghost>() as Ghost;
		ghost_comp.makeGhostOf(gameObject);
		ghost.SetActive(false);
	}

	public void handleHazardCollision()
	{
		Destroy(gameObject);
	}
}