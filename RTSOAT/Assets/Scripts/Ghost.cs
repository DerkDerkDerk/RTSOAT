// Ghost.cs
using UnityEngine;
using System.Collections.Generic;

class Ghost : MonoBehaviour
{
	private List<GameObject> surfaces;
	private List<MeshRenderer> surface_mesh_renderers;
	private List<MeshFilter> surface_mesh_filters;

	public Material valid_material;
	public Material invalid_material;
	public float ghostliness = 0.5f;
	private bool valid = true;

	void Awake()
	{
		surfaces = new List<GameObject>();
		surface_mesh_renderers = new List<MeshRenderer>();
		surface_mesh_filters = new List<MeshFilter>();

		Debug.Assert(valid_material != null && invalid_material != null, "Ghost Materials not set.");
	}

	public void makeGhostOf(GameObject living_obj)
	{
		// if object is a Vehicle
		Vehicle vehicle_comp = living_obj.GetComponent<Vehicle>();
		gameObject.transform.parent = living_obj.transform;
		gameObject.transform.localPosition = Vector3.zero;


		if (vehicle_comp != null)
		{
			foreach (GameObject surface in vehicle_comp.surfaces)
			{
				GameObject ghost_surface = new GameObject();
				ghost_surface.transform.parent = gameObject.transform;
				ghost_surface.transform.localPosition = Vector3.zero;
				ghost_surface.transform.localScale = surface.transform.localScale;
				MeshRenderer ghost_mesh_renderer = ghost_surface.AddComponent<MeshRenderer>() as MeshRenderer;
				MeshFilter ghost_mesh_filter = ghost_surface.AddComponent<MeshFilter>() as MeshFilter;

				ghost_mesh_filter.mesh = Instantiate(surface.GetComponent<MeshFilter>().mesh);

				Material ghost_mat = valid_material;
				Color ghost_mat_color = ghost_mat.color;
				ghost_mat_color.a = ghostliness;
				ghost_mat.color = ghost_mat_color;
				ghost_mesh_renderer.material = ghost_mat;


				surfaces.Add(ghost_surface);
				// Debug.Log("Added ghost surface: " + ghost_surface.name);
				surface_mesh_renderers.Add(ghost_mesh_renderer);
				// Debug.Log("Added ghost_mesh_renderer");
				surface_mesh_filters.Add(ghost_mesh_filter);
				// Debug.Log("Added ghost_mesh_filter");
			}
		}
	}

	public bool isValid()
	{
		return valid;
	}

	void setValid(bool v)
	{
		if (v)
		{
			valid = true;
			foreach (MeshRenderer mr in surface_mesh_renderers)
			{
				mr.material = valid_material;
			}
		}
		else
		{
			valid = false;
			foreach (MeshRenderer mr in surface_mesh_renderers)
			{
				mr.material = invalid_material;
			}
		}
	}
}