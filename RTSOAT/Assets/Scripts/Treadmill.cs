// Treadmill.cs

// The infinite ground that moves beneath the vehicles.
// This script manages the floor tiles, moving them across the screen, deleting the old tiles, and generating new ones.

// Tiles are X units wide (z-axis) and X units long (x-axis). 

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

class Treadmill : MonoBehaviour
{
	public Object tile_prefab;
	public Object hazard_prefab;
	public Object alert_prefab;
	List<GameObject> tiles;
	List<Tile> tile_comps;
	List<GameObject> hazards;
	List<Material> mats;
	public Material mat1;
	public Material mat2;
	public Material mat3;
	public Material mat4;
	private int lastMatIndex = -1;

	public float speed = 11f;
	public int min_tiles = 8;
	public float old_threshold = 20f;

	public bool treadmill_off = false;

	public float hazard_timer = 5f;
	private float seconds_until_hazard = 3f;
	public int hazard_num = 0;

	public GameObject alert_bound_right;
	public GameObject alert_bound_left;


	//TODO there is some issue with tiles overlapping at the edges, as evidenced by z-fighting.
	// hopefully it's just a positional thing, but it could be an issue with mesh generation :/

	void Awake()
	{
		if (tile_prefab == null)	{tile_prefab =		Resources.Load("Prefabs/Tile");}
		if (hazard_prefab == null)	{hazard_prefab =	Resources.Load("Prefabs/Hazard");}
		if (alert_prefab == null)	{alert_prefab =	Resources.Load("Prefabs/TreadmillAlert");}

		tiles = new List<GameObject>();
		tile_comps = new List<Tile>();
		hazards = new List<GameObject>();
		mats = new List<Material>();
		mat1 = Resources.Load("Materials/tile_progress_mat_2") as Material;
		if (mat1 != null){mats.Add(mat1);}
		if (mat2 != null){mats.Add(mat2);}
		if (mat3 != null){mats.Add(mat3);}
		if (mat4 != null){mats.Add(mat4);}
		// claim the default starting tile that is already present in the scene.
		if (alert_bound_right == null)	{alert_bound_right =	transform.Find("AlertBoundRight").gameObject;}
		if (alert_bound_left == null)	{alert_bound_left =		transform.Find("AlertBoundLeft").gameObject;}
	}

	void Start()
	{
		GameObject new_tile = makeTile(transform.position);
		tiles.Add(new_tile);
		tile_comps.Add(new_tile.GetComponent<Tile>());

		while (tiles.Count < min_tiles)
		{
			appendTile();
		}
	}

	void Update()
	{
		Debug.Assert(tiles.Count == tile_comps.Count, "GameObject/Script mismatch in tile arrays.");
		if (!treadmill_off)
		{
			float tile_move_dist = Time.deltaTime * speed;
			tiles[0].transform.position += new Vector3(tile_move_dist, 0, 0);
			for (int i = 1; i < tiles.Count; i++)
			{
				// move the treadmill
				tiles[i].transform.position = tiles[0].transform.position - new Vector3(tile_comps[i].x_width * i, 0, 0);
			}
		}

		// delete old tiles and create new ones;
		for (int i = 0; i < tiles.Count; i++)
		{
			if (isOldTile(i)) {cycleTile(i);}
		}

		seconds_until_hazard -= Time.deltaTime;
		if (seconds_until_hazard < 0)
		{
			spawnHazard();
			seconds_until_hazard = hazard_timer;
		}
		
	}

	bool isOldTile(int tile_index)
	{
		// check whether the tile is beyond the "old threshold"
		if ((tiles[tile_index].transform.position.x - tile_comps[tile_index].x_width / 2f) > old_threshold){return true;}
		else{return false;}
	}

	void appendTile()
	{
		GameObject new_tile = makeTile(tiles.Last().transform.position - new Vector3(tiles.Last().GetComponent<Tile>().x_width, 0, 0));
		tiles.Add(new_tile);
		tile_comps.Add(new_tile.GetComponent<Tile>());
	}

	void cycleTile(int tile_index)
	{
		GameObject old_tile = tiles[tile_index];
		foreach (GameObject hazard in tile_comps[tile_index].hazards)
		{
			Debug.Log("Removing " + hazard.name);
			hazards.Remove(hazard);
		}
		Destroy(old_tile);
		tiles.RemoveAt(tile_index);
		tile_comps.RemoveAt(tile_index);

		appendTile();
	}

	GameObject makeTile(Vector3 pos)
	{
		GameObject tile = (GameObject)Instantiate(tile_prefab, pos, Quaternion.identity, transform);
		Tile tile_comp = tile.GetComponent<Tile>();
		tile_comp.generateMesh();

		// assign a random material
		if (mats.Count > 0)
		{
			int mat_index = Random.Range(0, mats.Count);
			// if there's more than one material, then make sure we don't select the same one twice in a row
			if (mats.Count > 1){while (mat_index == lastMatIndex) {mat_index = Random.Range(0, 4);}}
			lastMatIndex = mat_index;
			tile_comp.setMaterial(mats[mat_index]);
		}
		return tile;
	}

	void spawnHazard()
	{
		int ti = Random.Range(2,6);
		GameObject tile = tiles[ti];
		Tile tile_comp = tile_comps[ti];

		Vector3 interp_from = alert_bound_right.transform.position;
		Vector3 interp_to = alert_bound_left.transform.position;
		
		float hazard_x = Random.Range(-30f, 30f);
		float hazard_z = Random.Range(interp_to.z, 	interp_from.z);
		Vector3 haz_local_pos = new Vector3(hazard_x, 0, hazard_z);
		GameObject new_hazard = (GameObject)Instantiate(hazard_prefab, tile.transform.position + haz_local_pos, Quaternion.identity, tile.transform);
		new_hazard.name = "Hazard_" + hazard_num++;
		tile_comp.hazards.Add(new_hazard);
		hazards.Add(new_hazard);

		// TODO support rotating camera.
		Debug.Assert(interp_from.z != interp_to.z, "Rotating camera not supported! Something has gone mapping hazard alert to screen edge");
		float interp_amount = Mathf.Abs((interp_from.z - hazard_z) / (interp_from.z - interp_to.z));
		Vector3 alert_pos = new Vector3(Mathf.Lerp(interp_from.x, interp_to.x, interp_amount), 0, hazard_z);
		GameObject alert = (GameObject)Instantiate(alert_prefab, alert_pos, Quaternion.identity, transform);
		alert.GetComponent<TreadmillAlert>().setObject(new_hazard);
		// Debug.Break();


	}
}