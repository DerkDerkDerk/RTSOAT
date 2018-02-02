using UnityEngine;
using System.Collections.Generic;
using System.Linq;

class Unit : MonoBehaviour
{
	public bool isSelected = true;
	public GameObject dest;
	private bool is_moving_to_dest = false;
	public float movement_speed = 3.0f; // units per second
	public bool is_jumping = false;
	public Vector3 jump_start;
	public Vector3 jump_end;
	public float jump_height_distance_ratio = 0.3f;
	public float max_jump_distance = 1.5f;
	public float jump_speed = 4.0f;
	private GameObject vector_bone;
	public GameObject current_region;
	public GameObject dest_region;
	public GameObject vehicle_piloting_object;
	public Vehicle vehicle_piloting_comp;
	public List<GameObject> current_path;
	public int current_path_index = -1;

	public GameObject unit_selection_ring;

	private GameObject prev_region;

	public float weight = 150f;

	void Awake()
	{
		dest = new GameObject("dest of " + gameObject.name);
		dest.transform.position = Vector3.zero;
		dest.transform.parent = gameObject.transform;
		vector_bone = gameObject.transform.Find("vector_bone").gameObject;
		Debug.Assert(vector_bone != null, gameObject.name + " has no vector bone");
		current_region = updateCurrentRegion();
		setDest(getPosition(), current_region);
		gameObject.transform.parent = getCurrentRegion().transform;

		unit_selection_ring = vector_bone.transform.Find("unit_selection_ring").gameObject;
		Debug.Assert(vector_bone != null, gameObject.name + " has no unit selection ring");
		isSelected = false;
		unit_selection_ring.GetComponent<MeshRenderer>().enabled = false;

		current_path = new List<GameObject>();
	}

	void Update()
	{
		current_region = updateCurrentRegion();
		handleMovement();
		handlePilotZone();
	}

	void handleMovement()
	{
		if (getPosition() != getDestPos())
		{
			is_moving_to_dest = true;
			if (is_jumping)
			{
				// THIS JUMP ASSUMES EVEN SURFACE HEIGHT
				// THIS JUMP HAS A SYMMETRIC PARABOLIC SHAPE
				// THIS JUMP IS SUPER SUPER PLACEHOLDER AND BASIC
				// move along jump arc
				// y = jump_height(1 - (x^2/(jump_dist/2)^2))
				// h is jump height
				// x is centered on 0, spanning -(dist/2) to (dist/2)
				// http://mathworld.wolfram.com/ParabolicSegment.html

				// TODO these asserts are useless. Do better, Derk.
				Debug.Assert(jump_start != null, "character is jumping, but jump_start is null");
				Debug.Assert(jump_end != null, "character is jumping, but jump_end is null");

				drawJumpArc(jump_start, jump_end, 20);

				// calculate the current position along the arc
				float jump_dist = Vector3.Distance(jump_start, jump_end);
				float dist_to_end = Vector3.Distance(new Vector3(jump_end.x, 0, jump_end.z), new Vector3(getPosition().x, 0, getPosition().z));
				//Debug.Log("Jump Progress: " + (jump_dist - dist_to_end).ToString("F2") + " / " + jump_dist.ToString("F2"));
				float jump_height = jump_height_distance_ratio * jump_dist;
				float cur_arc_x = (jump_dist / 2f) - dist_to_end;
				float cur_arc_y = jump_height * (1 - (cur_arc_x * cur_arc_x) / ((jump_dist / 2f) * (jump_dist / 2f)));
				// calculate next position along the arc
				float lateral_jump_speed = jump_speed;
				float movement_dist_x = Mathf.Min(lateral_jump_speed * Time.deltaTime, dist_to_end);
				if (movement_dist_x == dist_to_end)
				{
					move(jump_end - getPosition());
					is_jumping = false;
					//Debug.Break();
				}
				else
				{
					float dest_arc_x = cur_arc_x + movement_dist_x;
					float dest_arc_y = jump_height * (1 - (dest_arc_x * dest_arc_x) / (jump_dist * jump_dist / 4f));
					float movement_dist_y = dest_arc_y - cur_arc_y;
					Vector3 jump_direction_xy = new Vector3(movement_dist_x, movement_dist_y, 0);
					// rotate this jump vector about the Y axis until it points towards the destination
					Vector3 start_to_dest = new Vector3(jump_end.x, 0, jump_end.z) - new Vector3(jump_start.x, 0, jump_start.z);
					float angle = Vector3.Angle(Vector3.right, start_to_dest);
					float rot_dir = Vector3.Cross(Vector3.right, start_to_dest).normalized.y;
					if (rot_dir == 0) {rot_dir = 1f;}
					Debug.DrawLine(getPosition(), getPosition() + (Quaternion.AngleAxis(angle * rot_dir, Vector3.up) * start_to_dest), Color.green, 0, false);
					Vector3 jump_movement = Quaternion.AngleAxis(angle * rot_dir, Vector3.up) * jump_direction_xy;
					move(jump_movement);
				}
			}
			else
			{
				float dist_to_end = Vector3.Distance(getPosition(), getDestPos());
				Vector3 movement_direction = (getDestPos() - getPosition()).normalized;
				float movement_distance = Mathf.Min(movement_speed * Time.deltaTime, dist_to_end);
				move(movement_direction * movement_distance);
			}
		}
		else
		{
			if (current_path.Count > current_path_index + 1)
			{
				current_path_index++;
				moveToWaypoint(current_path[current_path_index]);
			}
			is_moving_to_dest = false;
		}
	}

	// TODO fix this function. I hate it. Piloting a vehicle shouldnt just be a casual happenstance of being in the correct position!
	void handlePilotZone()
	{
		if (vehicle_piloting_object != null)
		{
			return;
		}

		if (current_region.name == "pilot_zone")
		{
			PilotZone current_pilot_zone_component = current_region.GetComponent<PilotZone>();
			Debug.Assert(current_pilot_zone_component != null, "current pilot zone region has no PilotZone component.");
			
			if (current_pilot_zone_component.getVehicleComponent().pilot == null)
			{
				if (getPosition() == current_pilot_zone_component.getPilotPosition())
				{
					pilotVehicle(current_pilot_zone_component.getVehicleObject());
				}
			}
		}
	}

	void move(Vector3 movememnt_vector)
	{
		gameObject.transform.position += movememnt_vector;
	}

	void jump(Vector3 jd)
	{
		// Move along an arc path to the destination point.
		jump_start = getPosition();
		jump_end = jd;
		is_jumping = true;

		// visualize the jump path
		// Right now, this is just a triangle from start to peak to dest.
		float jump_dist = Vector3.Distance(jump_start, jump_end);
		float jump_height = jump_height_distance_ratio * jump_dist;
		Debug.DrawLine(jump_start, Vector3.Lerp(jump_start, jump_end, 0.5f) + new Vector3 (0, jump_height, 0), Color.blue, 5, false);
		Debug.DrawLine(Vector3.Lerp(jump_start, jump_end, 0.5f) + new Vector3 (0, jump_height, 0), jump_end, Color.blue, 5, false);
	}

	void pilotVehicle(GameObject v)
	{
		vehicle_piloting_object = v;
		vehicle_piloting_comp = v.GetComponent<Vehicle>();
		vehicle_piloting_comp.pilot = gameObject;

		Debug.Log(gameObject.name + " piloted " + vehicle_piloting_object.name);
	}

	void exitVehicle()
	{
		// stop the vehicle from moving any more
		vehicle_piloting_comp.stopMovingToDest();
		// remove the vehicle's pilot
		vehicle_piloting_comp.pilot = null;
		vehicle_piloting_comp = null;
		vehicle_piloting_object = null;

		Debug.Log(gameObject.name + " exited vehicle.");
	}

	Vector3 getPosition()
	{
		return vector_bone.transform.position;
	}

	Vector3 getObjectOffset()
	{
		return gameObject.transform.position - vector_bone.transform.position;
	}

	void setDest(Vector3 pos, GameObject obj)
	{
		//jump(pos);
		dest.transform.parent = obj.transform;
		dest.transform.position = pos;
	}

	Vector3 getDestPos()
	{
		return dest.transform.position;
	}

	GameObject updateCurrentRegion()
	{
		prev_region = getCurrentRegion();
		RaycastHit hit;
		Physics.Raycast(gameObject.transform.position, -Vector3.up, out hit);
		current_region = hit.collider.gameObject;
		if (prev_region != current_region) 
		{
			Debug.Log("Unit is in " + current_region.name);
			updateCurrentRegion();
			gameObject.transform.parent = getCurrentRegion().transform;
			// update Vehicle Weights
			// TODO fix weight handling
			if (prev_region.GetComponent<Surface>() != null)
			{
				Vehicle prev_vehicle_comp = prev_region.GetComponent<Surface>().vehicle.GetComponent<Vehicle>();
				prev_vehicle_comp.removeWeight(weight);
			}
			if (current_region.GetComponent<Surface>() != null)
			{
				Vehicle new_vehicle_comp = current_region.GetComponent<Surface>().vehicle.GetComponent<Vehicle>();
				new_vehicle_comp.addWeight(weight);
			}
		}
		return current_region;
	}

	GameObject getCurrentRegion()
	{
		return current_region;
	}

	public void handle_order(Vector3 pos, GameObject obj)
	{
		// if it's a Surface or PilotZone
		if (obj.GetComponent<Surface>() != null || obj.GetComponent<PilotZone>() != null)
		{
			if (isMovementOrderValid(pos, obj))
			{
				// if it's a PilotZone
				if (obj.GetComponent<PilotZone>() != null)
				{
					// move to the PilotZone location
					// TODO this should use the Pathing funcionality.
					// Will need to expand pathing to allow utnit to pilot vehicle at the end of the path.
					// similar to jump waypoint, this will likely just be another tag.
					moveToObject(obj.GetComponent<PilotZone>().getPilotPosition(), obj);
				}
				// otherwise (it's a Vehicle), just move to the clicked position
				else{setPath(getPath(pos, obj));}				
			}
		}
		// if it's Ground
		else if (obj.GetComponent<Treadmill>() != null)
		{
			// if Unit is piloting a vehicle
			if (vehicle_piloting_object != null)
			{
				Debug.Assert(vehicle_piloting_comp != null, "vehicle piloting object is set, but not component.");

				if (isPilotOrderValid(pos, obj))
				{
					// move the vehicle to that position
					vehicle_piloting_comp.moveToObject(vehicle_piloting_comp.getActualDest(pos), obj);
				}
			}
		}
	}

	bool isMovementOrderValid(Vector3 pos, GameObject obj)
	{
		// Pilot Zone checks
		PilotZone pz = obj.GetComponent<PilotZone>(); // can be null
		if (pz != null)
		{
			// if we're ordered to move to the same pilot zone we're currently occupying, no need to follow the order.
			if (current_region == obj)
			{
				Debug.Assert(getPosition() == obj.GetComponent<PilotZone>().getPilotPosition(), 
					"We're at the pilot zone, and ordered to go to the pilot zone, but we're not actually in position. " +
					"Should we still process this order?");
				return false;
			}
			// Also, a pilot zone order is invalid if another unit is already piloting it. THe order shouldn't be acted on at all.
			// Later, this order will be to hijack the vehicle if an unfriendly unit occupies it!
			if (pz.isOccupied())
			{
				return false;
			}
		}

		// TODO
		// here, we'd do a pathing check to see if this navigation makes sense
		// At a basic level, we can check to see if the location on a vehicle is the same vehicle as our parent.
		// This will need to become more advanced when vehicle-hopping becomes a thing.
		return true;
	}

	bool isPilotOrderValid(Vector3 pos, GameObject obj)
	{
		// here, we'd do a pathing check to see if this navigation makes sense

		return true;
	}

	public void moveToObject(Vector3 pos, GameObject obj)
	{
		if (vehicle_piloting_object != null)
		{
			exitVehicle();
		}

		setDest(pos, obj);
		//jump(pos);
	}

	public void moveToWaypoint(GameObject wp)
	{
		if (vehicle_piloting_object != null)
		{
			exitVehicle();
		}

		Waypoint wp_script = wp.GetComponent<Waypoint>();

		setDest(wp.transform.position, wp_script.region);
		if (wp_script.nav_type == "jump")
		{
			jump(wp.transform.position);
		}

	}

	public void performSelectionBehavior()
	{
		Debug.Log(gameObject.name + " is selected.");
		isSelected = true;
		unit_selection_ring.GetComponent<MeshRenderer>().enabled = true;
	}

	public void performDeselectionBehavior()
	{
		Debug.Log(gameObject.name + " is deselected.");
		isSelected = false;
		unit_selection_ring.GetComponent<MeshRenderer>().enabled = false;
	}

	public List<GameObject> getPath(Vector3 pos, GameObject obj)
	{
		// 10/8/17
		// This function needs hig-level info about the GameState.
		// Any time a unit attempts to path off of their vehicle, a global connectivity graph needs to be generated.
		// This graph will show all the connctions between SURFACES in the game. Some vehicles have multiple surfacees. 
		// Aside from some exceptions, they will always have the same local connectivity, so this won't need to be re-generated.
		// However, all connections to surfaces on other vehicles will be re-generated, with info about the height/distance of each connection
		// Very basic collision detection should  occur so connections don't happen through vehicles
		// There is a distance cutoff, so that connections aren't made where no unit can traverse.

		// Once this graph is generated, it is passed to the Unit. The Unit then determines which connections they are 
		// personallly able to traverse, and which path is the best path to their destination.

		// 10/28/17
		// This function currently returns a hardcoded path for the unit to follow.

		List<GameObject> waypoints = new List<GameObject>();
		int wp_count = 0;

		// First waypoint is always the unit's starting location
		waypoints.Add(makeWaypoint(getPosition(), "wp_" + wp_count++ + "_" + gameObject.name, current_region));

		// if they're piloting a vehiclem they first must exit the pilot zone.
		if (vehicle_piloting_comp != null)
		{
			waypoints.Add(makeWaypoint(	vehicle_piloting_comp.pilot_zone_comp.getExitPosition(), 
										"wp_" + wp_count++ + "_" + gameObject.name, 
										vehicle_piloting_object.transform.parent.gameObject));
		}


		// if the destination is on a different platform, then we need to jump.
		// instead of comparing the unit's current region, we compare the parents of the WAYPOINTS. This is a better measure.
		if (waypoints.Last().transform.parent != obj.transform)
		{
			GameObject start_wp = waypoints.Last();
			Vector3 walkvec;
			Vector3 jumpvec;

			Bounds current_platform_bounds = waypoints.Last().transform.parent.gameObject.GetComponent<BoxCollider>().bounds;
			Bounds dest_platform_bounds = obj.GetComponent<BoxCollider>().bounds;

			// Use bounds.extents, the given pos, and the Unit pos, to determine where (if at all) a jump must be made.
			// Assumes rectangular platforms
			// Assumes no height difference between platforms
			// Assumes platforms are not intersecting (AKA collision detection for platforms is implemented)
			// Only allows jumps in 4 directions (along x or z axes)

			Vector3 platform_direction = dest_platform_bounds.center - current_platform_bounds.center;

			float cur_left = 	current_platform_bounds.center.x 	- current_platform_bounds.extents.x;
			float cur_right = 	current_platform_bounds.center.x 	+ current_platform_bounds.extents.x;
			float cur_top =		current_platform_bounds.center.z 	+ current_platform_bounds.extents.z;
			float cur_bottom =	current_platform_bounds.center.z 	- current_platform_bounds.extents.z;
			float dest_left = 	dest_platform_bounds.center.x 		- dest_platform_bounds.extents.x;
			float dest_right = 	dest_platform_bounds.center.x 		+ dest_platform_bounds.extents.x;
			float dest_top =	dest_platform_bounds.center.z 		+ dest_platform_bounds.extents.z;
			float dest_bottom =	dest_platform_bounds.center.z 		- dest_platform_bounds.extents.z;
			float cur_y = 		current_platform_bounds.center.y 	+ current_platform_bounds.extents.y;
			float dest_y = 		dest_platform_bounds.center.y 		+ dest_platform_bounds.extents.y;

			bool is_left = 	dest_right 	< cur_left;
			bool is_right = dest_left 	> cur_right;
			bool is_above = dest_bottom > cur_top;
			bool is_below = dest_top 	< cur_bottom;

			//Debug.DrawLine(new Vector3(cur_left, cur_y, cur_top), new Vector3(dest_left, dest_y, dest_top), Color.red, 5, false);

			//if ((is_left && is_above) || (is_above && is_right) || (is_right && is_below) || (is_below && is_left))
			// if ((is_left || is_right) && (is_above || is_below)){Debug.Log("No Overlap");}
			// else if (is_above)									{Debug.Log("Above");}
			// else if (is_right)									{Debug.Log("Right");}
			// else if (is_below)									{Debug.Log("Below");}
			// else if (is_left)									{Debug.Log("Left");}
			// else												{Debug.Log("Collision!");}

			// when jumping is involved, the unit generally wants to jump as directly onto the dest platform as possible, before doing any more navigating. If there's a straight shot from the unit position to the dest platform, then go for that. if not, then if there's overlap between the adjacent sides, then aim to jump across the midpoint of that overlap. be sure not to navigate past the destination though. if the dest is before the midpoint, then line up with the dest. if there's no overlap between adjacent sides, then jump from this platform's corner to the dest platform's corner.

			// if the dest platform is overlapping to the right or left
			if ((is_left || is_right) && !(is_above || is_below))
			{
				float launch_z;
				// if it's a straight shot, then navigate directly to the edge and jump
				if (start_wp.transform.position.z < dest_top && start_wp.transform.position.z > dest_bottom)
				{
					// Debug.Log("Jumping a straight shot. z = " + start_wp.transform.position.z);
					launch_z = start_wp.transform.position.z;
				}
				// if it's not a straight shot, then navigate to overlap and then jump
				else
				{
					List<float> bounds_vert = new List<float>() {cur_top, cur_bottom, dest_top, dest_bottom};
					bounds_vert.Sort();
					float overlap_midpoint = (bounds_vert[1] + bounds_vert[2]) / 2f;

					// navigate to either the midpoint of the overlap, or align with the destination, whichever is closer
					if (Mathf.Abs(start_wp.transform.position.z - overlap_midpoint) < Mathf.Abs(start_wp.transform.position.z - pos.z))
					{
						// jump at the overlap midpoint
						// Debug.Log("Jumping at the midpoint. z = " + overlap_midpoint);
						launch_z = overlap_midpoint;
					}
					else
					{
						// jump at the destination point's z position
						// Debug.Log("Jumping aligned with the dest. z = " + pos.z);
						launch_z = pos.z;
					}
				}
				walkvec = new Vector3(is_left ? cur_left : cur_right, start_wp.transform.position.y, launch_z);
				jumpvec = new Vector3(is_left ? dest_right : dest_left, start_wp.transform.position.y, launch_z);
			}
			// if the dest platform is overlapping above or below
			else if ((is_above || is_below) && !(is_left || is_right))
			{
				float launch_x;
				// if it's a straight shot, then navigate directly to the edge and jump
				if (start_wp.transform.position.x > dest_left && start_wp.transform.position.x < dest_right)
				{
					// Debug.Log("Jumping a straight shot. x = " + start_wp.transform.position.x);
					launch_x = start_wp.transform.position.x;
				}
				// if it's not a straight shot, then navigate to overlap and then jump
				else
				{
					List<float> bounds_hor = new List<float>() {cur_left, cur_right, dest_left, dest_right};
					bounds_hor.Sort();
					float overlap_midpoint = (bounds_hor[1] + bounds_hor[2]) / 2f;

					// navigate to either the midpoint of the overlap, or align with the destination, whichever is closer
					if (Mathf.Abs(start_wp.transform.position.x - overlap_midpoint) < Mathf.Abs(start_wp.transform.position.x - pos.x))
					{
						// jump at the overlap midpoint
						// Debug.Log("Jumping at the midpoint. x = " + overlap_midpoint);
						launch_x = overlap_midpoint;
					}
					else
					{
						// jump at the destination point's x position
						// Debug.Log("Jumping aligned with the dest. x = " + pos.x);
						launch_x = pos.x;
					}
				}
				walkvec = new Vector3(launch_x, start_wp.transform.position.y, is_above ? cur_top : cur_bottom);
				jumpvec = new Vector3(launch_x, start_wp.transform.position.y, is_above ? dest_bottom : dest_top);
			}
			// if the dest platform is not overlapping on any side
			else
			{
				Debug.Log("This requires a diagonal jump. Not supported yet.");
				return waypoints;
			}
			
			waypoints.Add(makeWaypoint(walkvec, "wp_" + wp_count++ + "_" + gameObject.name, current_region, "walk"));
			waypoints.Add(makeWaypoint(jumpvec, "wp_" + wp_count++ + "_" + gameObject.name, obj, "jump"));
		}

		waypoints.Add(makeWaypoint(pos, "wp_" + wp_count++ + "_" + gameObject.name, obj));
		return waypoints;
	}

	public void setPath(List<GameObject> path)
	{
		deletePath(current_path);
		current_path = path;
		current_path_index = 0;

		moveToWaypoint(current_path[current_path_index]);
	}

	public void clearPath()
	{
		current_path.Clear();
		current_path_index = -1;
	}

	GameObject makeWaypoint(Vector3 pos, string name, GameObject parent = null, string nt = "walk")
	{
		GameObject wp;
		if (parent != null)
		{
			wp = (GameObject)Instantiate(Resources.Load("Prefabs/Waypoint"), 
													pos, 
													Quaternion.identity, 
													parent.transform);
		}
		else
		{
			wp = (GameObject)Instantiate(Resources.Load("Prefabs/Waypoint"), 
													pos, 
													Quaternion.identity);
		}
		wp.name = name;

		Waypoint wp_comp = wp.GetComponent<Waypoint>();
		wp_comp.nav_type = nt;
		wp_comp.region = parent;
		return wp;
	}

	void deletePath(List<GameObject> path)
	{
		foreach (GameObject waypoint in path){Destroy(waypoint);}
	}

	void drawJumpArc(Vector3 js, Vector3 je, int n)
	{
		// y = h(1 - x^2/a^2)
		float func_a = 0.5f * Vector3.Distance(js, je);
		float func_h = jump_height_distance_ratio * func_a * 2f;
		// draw each segment
		for (int s = 0; s < n; s++)
		{
			float x_0 = 0f;
			float x_1 = 2f * func_a;
			if (s > 0){x_0 = (float) s / (float) n * 2f * func_a;}
			if (s < n - 1){x_1 = (float) (s + 1) / (float) (n) * 2f * func_a;}
			// x_0 -= func_a;
			// x_1 -= func_a;
			float y_0 = func_h * (1f - ((x_0 - func_a)*(x_0 - func_a))/(func_a*func_a));
			float y_1 = func_h * (1f - ((x_1 - func_a)*(x_1 - func_a))/(func_a*func_a));

			Vector3 seg_0 = new Vector3(x_0, y_0, 0f);
			Vector3 seg_1 = new Vector3(x_1, y_1, 0f);

			float angle = Vector3.Angle(Vector3.right, new Vector3(je.x, 0f, je.z) - new Vector3(js.x, 0f, js.z));
			float rot_dir = Vector3.Cross(Vector3.right, new Vector3(je.x, 0f, je.z) - new Vector3(js.x, 0f, js.z)).normalized.y;
			if (rot_dir == 0) {rot_dir = 1f;}

			//if (s == 0){Debug.Log("angle: " + angle + ", rot_dir: " + rot_dir);}

			Vector3 seg_s = (Quaternion.AngleAxis(angle * rot_dir, Vector3.up) * seg_0);
			Vector3 seg_e = (Quaternion.AngleAxis(angle * rot_dir, Vector3.up) * seg_1);

			seg_s += js;
			seg_e += js;

			Debug.DrawLine(seg_s, seg_e, Color.green, 0, false);
		}
	}

}