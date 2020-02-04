using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Phidget22;
using Phidget22.Events;
using System;
using UnityEngine.SceneManagement;

public class ScooterController : MonoBehaviour
{

    // ____ SCOOTER  
    [Header("SCOOTER")]
    public WheelCollider frontWheel;
    public WheelCollider rearWheel;
    public GameObject meshFront;
    public GameObject meshRear;
    public GameObject centerOfmassOBJ;


    // MOVEMENT 
    [Header("MOVEMENT")]
    // PUBLIC 
    float horizontalInput;
    float verticalInput;
    public PhidgetManager _phidScript;
    public float resistance = 10;
    public float maxSpeed= 20;
    // PRIVAT 
    Vector3 lastPosition;
    Rigidbody body;
    float rbVelocityMagnitude;
    float medRPM;
    Vector3 myPosition;
    Vector3 temporaryVector;
    Quaternion temporaryQuaternion;
    bool crash;
    float battery = 100;


    // INTERACTIV 
    [Header("INTERAKTIV")]
    //____ COLLISION WITH ENEMY 
    public GameObject explosion;


    //UI 
    [Header("UI")]
    public Text batteryText;
    public Text warningText;
    public Image[] mySpeedImages = new Image[5]; 
    public Slider batteryBar;
    public Image batteryFill;
    public GameObject SpeedUpPanel;
    // Colors Speed 
    public Color activeCol = Color.white;
    public Color idleCol = Color.grey;

    // UI privat
    float seconds, minutes;
    float euros;

    // IEnumerator Memorie 
    float p_maxSpeed;
    float p_resistance;
 


    // Start is called before the first frame update
    void Start()
    {
        // MOVEMENT SETUP
        transform.rotation = Quaternion.identity;

        body = GetComponent<Rigidbody>();
        body.centerOfMass = transform.InverseTransformPoint(centerOfmassOBJ.transform.position);
        centerOfmassOBJ.transform.parent = transform;
        centerOfmassOBJ.transform.localPosition = new Vector3(0.0f, -0.3f, 0.0f);
        body.centerOfMass = transform.InverseTransformPoint(centerOfmassOBJ.transform.position);

        body.interpolation = RigidbodyInterpolation.Extrapolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // UI 
        SetBatteryText();
        SpeedUpPanel.SetActive(false);

        // IEnumerator Memorie Setup
        p_maxSpeed = maxSpeed;
        p_resistance = resistance;

    }


    void Update()
    {
        frontWheel.GetWorldPose(out temporaryVector, out temporaryQuaternion);
        meshFront.transform.position = temporaryVector;
        temporaryQuaternion *= Quaternion.Euler(0, 0, 90);
        meshFront.transform.rotation = temporaryQuaternion;
        rearWheel.GetWorldPose(out temporaryVector, out temporaryQuaternion);
        meshRear.transform.position = temporaryVector;
        temporaryQuaternion *= Quaternion.Euler(0, 0, 90);
        meshRear.transform.rotation = temporaryQuaternion;

        myPosition = body.position;




        //UI

        //COUNTER TEXT 
        minutes = (int)(Time.time / 60f);
        seconds = (int)(Time.time % 60f);
        euros = seconds / 4;

        // UI Speed
        // normalisieren - durch 20(max) dann sind alle werte zwischen 0 und 1 // und dann bis 4 hochzählen
        float imgIndex = body.velocity.magnitude / 20 * 4;
        // index muss ganze zahl sein 2.56 wird also zu 2
        imgIndex = Mathf.Floor(imgIndex);
        // index darf nicht über länge den höchsten arrayindex (length-1) gehen sonst fehler
        imgIndex = Mathf.Min(imgIndex, mySpeedImages.Length - 1);

        for (int i = 0; i < mySpeedImages.Length; i++) //länge von array - alle - diese farbe 
        {
            mySpeedImages[i].color = idleCol;
        }

        for (int i = 0; i <= imgIndex; i++) // geh alle durch die imgIndex entsprechen 
        {
            mySpeedImages[i].color = activeCol; // unf gibt diese Farben
            if (i == 3)
            {
                mySpeedImages[i].color = Color.yellow;

            }
            if (i == 4)
            {
                mySpeedImages[i].color = Color.red;
            }
        };



        //Reset Scene/ Player
        if (Input.GetButtonDown("Cancel"))
        {
            Debug.Log("Reset Scene");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetButtonDown("Jump"))
        {
            Debug.Log("Reset Rotation");
            StartCoroutine(ResetPlayerPosition());
        }

    }


    void FixedUpdate()
    {

        ////___ MOVEMENT
        Vector4 sensorVals = _phidScript.smoothedSensorData * -1;

        float moveForward = sensorVals[0] + sensorVals[3];
        float moveBackward = sensorVals[1] + sensorVals[2];
        verticalInput = (moveForward - moveBackward)/resistance;
        verticalInput += Input.GetAxis("Vertical");
        verticalInput = Mathf.Clamp(verticalInput,-1,1);

        float moveLeft = sensorVals[3] + sensorVals[2];
        float moveRight = sensorVals[1] + sensorVals[0];
        horizontalInput = (moveRight - moveLeft)/(resistance/2);
        horizontalInput += Input.GetAxis("Horizontal");

        transform.Rotate(0.0f, horizontalInput / 2, 0.0f);


        rearWheel.motorTorque = verticalInput * body.mass * (1 - body.velocity.magnitude / maxSpeed) * 2f;
        frontWheel.motorTorque = verticalInput * body.mass  * (1 - body.velocity.magnitude / maxSpeed)* 4f;


        //steerAngle, Lenkung
        float nextAngle = horizontalInput;
        frontWheel.steerAngle = Mathf.Lerp(frontWheel.steerAngle, nextAngle, Time.fixedDeltaTime); //damper Lenkung 

        //Maxspeed
        body.velocity = Vector3.ClampMagnitude(body.velocity, maxSpeed); //clamping mit maximal wert 

        // Stabisiator aus bei zusammenstoss // jetzt an 
        if (crash == false)
        {
            Stabilizer();
        }

        BatteryDrain(body.velocity.magnitude);
        SetBatteryText();
    }


    void Stabilizer()
    {
        Vector3 axisFromRotate = Vector3.Cross(transform.up, Vector3.up);
        Vector3 torqueForce = axisFromRotate.normalized * axisFromRotate.magnitude * 50;
        torqueForce.x = torqueForce.x * 0.8f;
        torqueForce -= body.angularVelocity;
        body.AddTorque(torqueForce * body.mass * 0.02f, ForceMode.Impulse);

        float rpmSign = Mathf.Sign(medRPM) * 0.02f;
        if (rbVelocityMagnitude > 1.0f && frontWheel.isGrounded && rearWheel.isGrounded)
        {
            body.angularVelocity += new Vector3(0, horizontalInput * rpmSign, 0);
        }
    }

    // TRIGGER 
    #region
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Pick Up"))
        {
            //other.gameObject.SetActive(false);
            StartCoroutine(PickUpPush());
        }

        if (other.gameObject.CompareTag("Charger"))
        {
            other.gameObject.SetActive(false);
            battery = Math.Min(battery + 10, 100); //funktion vergleicht die beiden Werte 
            SetBatteryText();
        }

    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            battery = Math.Max(battery - 10, 0);
            GameObject myExplosion = Instantiate(explosion, transform.position, Quaternion.identity) as GameObject;
            Destroy(collision.gameObject);
            Destroy(myExplosion, 2);
        }

        if(collision.gameObject.CompareTag("Wall"))
        {
            print(true);
            crash = true;
            body.AddForce(transform.forward * 5000, ForceMode.Impulse);
            StartCoroutine(ResetPlayerPosition());
        }
    }

    #endregion

    //BATTERIE
    #region
    void BatteryDrain(float direction)
    {
        if (direction < 0 || direction > 0)
        {
            battery = battery - 0.01f;
            batteryBar.value = battery;
            batteryBar.value = Mathf.Clamp(batteryBar.value, batteryBar.minValue, batteryBar.maxValue);
        }

        if (batteryBar.value >= 40)
        {
            batteryFill.GetComponent<Image>().color = new Color(0, 255, 0, 100);
        }

        if (batteryBar.value < 40)
        {
            batteryFill.GetComponent<Image>().color = new Color(255, 255, 0, 100); 
        }

        if (batteryBar.value < 20)
        {
            batteryFill.GetComponent<Image>().color = new Color(255, 0, 0, 100);
        }

        
        if (batteryBar.value < 30)
        {
            warningText.text = "WARNING: YOUR BATTERY IS LOW";
            Destroy(warningText, 3); 
        }
    }


    void SetBatteryText()
    {
        batteryText.text = Mathf.RoundToInt(battery).ToString() + "%";
    }

    #endregion

    //IEnumeators 
    #region
    IEnumerator PickUpPush()
    {
        SpeedUpPanel.SetActive(true);
        resistance = 5;
        maxSpeed += 50;
        yield return new WaitForSeconds(3);
        SpeedUpPanel.SetActive(false);
        resistance = p_resistance;
        maxSpeed = p_maxSpeed;
    }

    IEnumerator ResetPlayerPosition()
    {
        myPosition = body.position;
        yield return new WaitForSeconds(3f);
        Quaternion rotation = Quaternion.Euler(0, 0, 0);
        transform.localPosition = myPosition;  
        crash = false; 
    }
    #endregion

}
