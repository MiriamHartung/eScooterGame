using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    public Material redMat;
    public Material yellowMat;
    public Material greenMat;

    public GameObject redLight;
    public GameObject yellowLight;
    public GameObject greenLight;

    public GameObject car;
    public float speed = 200f; 

    // Start is called before the first frame update
    void Start()
    {
        SwitchTrafficLight(redLight, Color.red);
    }

    // Update is called once per frame
    void Update()
    {
        //carCrashPos = transform.position;
        //carCrashPos.x += 1;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            StartCoroutine(TrafficLightControlleur());
            StartCoroutine(MoveCar());
        }
    }


    IEnumerator TrafficLightControlleur(){
        yield return new WaitForSeconds(3);
        SwitchTrafficLight(yellowLight, Color.yellow);
        yield return new WaitForSeconds(1);
        SwitchTrafficLight(greenLight, Color.green);
    }

    IEnumerator MoveCar(){
        float timer = 0;

        while (timer<600)
        {
            car.transform.Translate(speed * Time.deltaTime, 0, 0);
            timer++;
            yield return new WaitForEndOfFrame();
        }

        car.SetActive(false);

        //Destroy(car);
    }



    void SwitchTrafficLight(GameObject whichLight, Color whichCol){
        //erstmal alles aus
        redLight.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
        yellowLight.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
        greenLight.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);
        //dann nur das an was wir wollen
        whichLight.GetComponent<Renderer>().material.SetColor("_EmissionColor", whichCol);
    }



}
