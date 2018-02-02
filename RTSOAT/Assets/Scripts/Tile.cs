// Tile.cs

using UnityEngine;
using System.Collections.Generic;

class Tile : MonoBehaviour
{
	public float x_width = 72f;
	public float z_width = 12f;
	public int x_res = 96; // this many rectangles. this many + 1 verts.
	public int z_res = 12; // this many rectangles. this many + 1 verts.
	public List<GameObject> hazards; // hazards which are children of this tile

	MeshRenderer mesh_renderer;
	MeshFilter mesh_filter;

	void Awake()
	{
		mesh_renderer = GetComponent<MeshRenderer>();
		mesh_filter = GetComponent<MeshFilter>();
		hazards = new List<GameObject>();
	}

	public void generateMesh()
	{
		List<Vector3>	verts	= new List<Vector3>();
		List<Vector3>	norms	= new List<Vector3>();
		List<int> 		tris	= new List<int>();
		List<Vector2> 	uvs		= new List<Vector2>();
		Mesh mesh = new Mesh();

		int num_rects_x = x_res;
		int num_rects_z = z_res;
		int num_verts_x = x_res + 1;
		int num_verts_z = z_res + 1;

		for (int x = 0; x < num_verts_x; x++)
		{
			for (int z = 0; z < num_verts_z; z++)
			{
				float vec_x = -1f * (x_width / 2f) + ((float)x_width / (float)x_res) * x;
				float vec_y = 0f;
				float vec_z = -1f * (z_width / 2f) + ((float)z_width / (float)z_res) * z;
				verts.Add(new Vector3(vec_x, vec_y, vec_z));

				float u = ((float)x / (float)x_res);
				float v = ((float)z / (float)z_res);
				uvs.Add(new Vector2(u, v));

				norms.Add(Vector3.up);
			}
		}

		for (int x = 0; x < num_rects_x; x++)
		{
			for (int z = 0; z < num_rects_z; z++)
			{
				tris.Add(z + (x + 0) * num_verts_z		);
				tris.Add(z + (x + 1) * num_verts_z + 1	);
				tris.Add(z + (x + 1) * num_verts_z		);
				tris.Add(z + (x + 0) * num_verts_z		);
				tris.Add(z + (x + 0) * num_verts_z + 1	);
				tris.Add(z + (x + 1) * num_verts_z + 1	);
			}
		}
		mesh.vertices = verts.ToArray();
		mesh.triangles = tris.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.normals = norms.ToArray();
		mesh_filter.mesh = mesh;
	}

	public void setMaterial(Material mat)
	{
		mesh_renderer.material = mat;
	}
}