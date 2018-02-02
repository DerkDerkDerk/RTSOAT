using UnityEngine;

class AlertBound : MonoBehaviour
{
	bool is_right = true;

	void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(transform.position, 0.2f);
	}
}