using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{

    private float xRotation;
    Vector2 mouseLook; // total Movement 
    Vector2 smoothV;
    Vector2 mouseDelta;
    public float mouseSensitivity = 20.0f;
    public float mouseSmoothing = 2.0f;

    GameObject character; // automaticly set in Start();
    Rigidbody character_rigid;

    // Start is called before the first frame update
    private void Start()
    {
        character = this.transform.parent.gameObject;
        character_rigid = character.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void Update()
    {
        mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(mouseSensitivity * mouseSmoothing, mouseSensitivity * mouseSmoothing));
        smoothV.x = Mathf.Lerp(smoothV.x, mouseDelta.x, 1f / mouseSmoothing);
        smoothV.y = Mathf.Lerp(smoothV.y, mouseDelta.y, 1f / mouseSmoothing); // Smooth between 2 points 
        mouseLook += smoothV;
        mouseLook.y = Mathf.Clamp(mouseLook.y, -90f, 90f); //nicht koplett um 360 grad drehen, sondern nur 180 = Clamping
        mouseLook.x = Mathf.Clamp(mouseLook.x, -90f, 90f); //nicht koplett um 360 grad drehen, sondern nur 180 = Clamping

        //transform.localRotation = Quaternion.AngleAxis(-mouseLook.y, Vector3.right);

        Vector3 myAngles = transform.localEulerAngles;
        myAngles.y += smoothV.x;
        myAngles.x += -smoothV.y;
        transform.localEulerAngles = myAngles;

    }

    private void FixedUpdate()
    {
    }
}
