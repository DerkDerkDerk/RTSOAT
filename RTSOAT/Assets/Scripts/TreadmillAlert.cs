// TreamillAlert.cs

using UnityEngine;
using System.Collections;
using TMPro;

class TreadmillAlert : MonoBehaviour
{
	GameObject text_obj;
	TextMeshPro text_comp;

	public GameObject incoming_obj;
	private float distance;
	private float uninitialized_timer = 1f;

	void Awake()
	{
		if (text_obj == null){text_obj = transform.Find("TextMeshPro").gameObject;}
		Debug.Assert(text_obj != null, "Could not find TextMeshPro object");
		text_comp = text_obj.GetComponent<TextMeshPro>();
		// text_comp.anchor = TMPro.AnchorPositions.BottomLeft; // anchor might be deprecated?
		// text_obj.transform.LookAt(Camera.main.transform);
		text_obj.SetActive(false);
	}

	public void setObject(GameObject o)
	{
		incoming_obj = o;
		text_obj.SetActive(true);
		//text_obj.transform.LookAt(Camera.main.transform);
		updateText();
	}

	void Update()
	{
		if (incoming_obj == null)
		{
			uninitialized_timer -= Time.deltaTime;
			if (uninitialized_timer <= 0)
			{
				Debug.Log("TreadmillAlert was created, but never assigned an incoming object.");
				Debug.Break();
			}
		}
		else
		{
			float prev_dist = distance;
			updateText();
			if (distance > prev_dist) {Destroy(gameObject);}
		}
	}

	void updateText()
	{
		distance = Vector3.Distance(transform.position, incoming_obj.transform.position);
		text_comp.SetText(distance.ToString("F2"));
	}
}