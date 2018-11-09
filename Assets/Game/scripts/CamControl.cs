using UnityEngine;

public class CamControl : MonoBehaviour
{
	void Update()
    {
        if (Input.GetKey(KeyCode.I))
            Move(Vector3.forward);
        if (Input.GetKey(KeyCode.K))
            Move(-Vector3.forward);
        if (Input.GetKey(KeyCode.H))
            Move(-Vector3.right);
        if (Input.GetKey(KeyCode.L))
            Move(Vector3.right);
        if (Input.GetKey(KeyCode.U))
            Move(Vector3.up);
        if (Input.GetKey(KeyCode.J))
            Move(-Vector3.up);
        if (Input.GetKey(KeyCode.Y))
            transform.Rotate(transform.up, -Time.deltaTime * 25);
        if (Input.GetKey(KeyCode.O))
            transform.Rotate(transform.up, Time.deltaTime * 25);
        if (Input.GetKeyDown(KeyCode.Tab))
            transform.Rotate(transform.parent.up, 180, Space.World);
        if (Input.GetKeyDown(KeyCode.Return))
        {
            var person = PlayerAvatar.getLocalPlayer()?.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>();
            if (person)
                person.m_DebugMode = !person.m_DebugMode;
        }
    }

    private void Move(Vector3 desiredMove)
    {
        transform.Translate(desiredMove * Time.deltaTime);
    }
}
