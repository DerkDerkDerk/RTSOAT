using UnityEngine;

class Waypoint : MonoBehaviour
{
	public string nav_type = "walk"; // walk, jump, pilot
	public GameObject region = null;

	void Awake()
	{
		if (transform.parent != null)
		{
			region = transform.parent.gameObject;
		}
	}

	void OnDrawGizmos()
	{
		if (nav_type == "walk")			{Gizmos.color = Color.yellow;}
		else if (nav_type == "jump")	{Gizmos.color = Color.green;}

		Gizmos.DrawWireSphere(transform.position, 0.12f);
	}

	public void setRegion(GameObject r)
	{
		region = r;
	}

	public void setName(string n)
	{
		gameObject.name = n;
	}
}