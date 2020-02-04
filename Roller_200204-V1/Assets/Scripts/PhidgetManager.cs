using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Phidget22;
using Phidget22.Events;
using System;
//
using UnityEngine.UI;

public class PhidgetManager : MonoBehaviour
{
    [Header("Phidget")]
   
    public List<VoltageRatioInput> sensors = new List<VoltageRatioInput>();

    float multiplier = 20000;
    public Vector4 calibratedSensorData, sensorOffset, smoothedSensorData;
    private float dataInt = 16, timer;
    private bool calibrate = true;

    [Header("OneEuroFilter Settings")]
    public float filterFrequency = 120.0f;
    public float filterMinCutoff = 1.0f;
    public float filterBeta = 0.0f;
    public float filterDcutoff = 1.0f;
    OneEuroFilter<Vector4> PhidgetFilter;

    public float horizontalWeight, verticalWeight;

    //_______ MINE 
    public Text calibratorText; 



    void IsAttached(object sender, AttachEventArgs e)
    {
        VoltageRatioInput ch = (VoltageRatioInput)sender;
        sensors[ch.Channel].DataInterval = (int)dataInt;
    }

    public void DoCalibrate()
    {
        if (calibrate)
        {
            try
            {
                sensorOffset = new Vector4(); //for initial position to level sensor data

                for (int i = 0; i < 4; i++)
                {
                    sensors[i].DataInterval = 8;
                    sensorOffset[i] = (float)(sensors[i].VoltageRatio * multiplier);
                    print("calibrated");
                    //
                    calibratorText.text = "GO";
                    Destroy(calibratorText, 3); 
                }
                
                calibrate = false;
            }
            catch (Exception)
            {
                print("not ready yet");
                //
                calibratorText.text = "WAIT";
            }
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        PhidgetFilter = new OneEuroFilter<Vector4>(filterFrequency);

        for (int i = 0; i < 4; i++)
        {
            sensors.Add(new VoltageRatioInput());
            sensors[i].Channel = i;
            sensors[i].Attach += IsAttached;
            sensors[i].Open();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        DoCalibrate();

        if (!calibrate)
        {
            PhidgetFilter.UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);

            for (int i = 0; i < 4; i++)
            {
                // get calibrated sensor values
                calibratedSensorData[i] = ((float)sensors[i].VoltageRatio * multiplier) - sensorOffset[i];

                // smooth the data if needed
                smoothedSensorData = PhidgetFilter.Filter(calibratedSensorData);

                horizontalWeight = (smoothedSensorData[0] + smoothedSensorData[1] - (smoothedSensorData[2] + smoothedSensorData[3]));
                verticalWeight = (smoothedSensorData[0] + smoothedSensorData[3]) - (smoothedSensorData[1] + smoothedSensorData[2]);
            }

            //transform.rotation = Quaternion.Euler(new Vector3(horizontalWeight*10, 0, verticalWeight*10));

        }
    }

    private void OnApplicationQuit()
    {
        for (int i = 0; i < 4; i++)
        {
            sensors[i].Close();
            sensors[i] = null;
        }

        if (Application.isEditor)
        {
            Phidget.ResetLibrary();
        }
        else
        {
            Phidget.FinalizeLibrary(0);
        }

    }

}