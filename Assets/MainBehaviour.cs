using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;

public class MainBehaviour : MonoBehaviour
{
    public GameObject tb;

    /*  Starting Infomation */
    public UserInfo userInfo;

    public List<ARObject> ARObjectList;

    private Vector3 targetPosition;

    void Start()
    {
        /*  Debug Info Printer    */
        //tb = GameObject.FindGameObjectWithTag("debugInfo"); Unity Editor에서 연결 시켰음.

        // Create User informaion
        userInfo = new UserInfo();
        userInfo.mainCamera = GameObject.FindGameObjectWithTag("MainCamera"); // main Camera Setting

        // GPS Coroutine Start
        StartCoroutine(GetGps());

        // Create Object List
        ARObjectList = new List<ARObject>();

        /*  Test Data Create    */
        ADInfo tmp_ad_info = new ADInfo
        {
            name = "Google",
            GPSInfo = new Vector3(37.4507f, 126.6580f, 0.0f),
            bearing = 0.0f,
            bannerUrl = "https://lh4.googleusercontent.com/-v0soe-ievYE/AAAAAAAAAAI/AAAAAAADwkE/KyrKDjjeV1o/photo.jpg",
            sub = "",
            tex = null
        };

        ARObjectList.Add(new ARPlane(tmp_ad_info, userInfo));
        /////////////////////////////////////////////////////////////////////////////////////////
    }

    void Update()
    {
        UpdateBearingWithSmoothing();
        UpdatePosition();
        //// ARObject Update (animation)
        //foreach(ARObject entity in ARObjectList) {
        //    entity.Update();
        //}
    }

    private void UpdateBearingWithSmoothing()
    {
        // 방위각
        //        0:
        //        북
        // 270:서    동:90
        //        남
        //        :180
        float newCompass = Input.compass.trueHeading;
        if (Mathf.Abs(newCompass - userInfo.currentBearing) < 180)
        {
            if (Math.Abs(newCompass - userInfo.currentBearing) > Constants.SmoothThresholdCompass)
            {
                userInfo.currentBearing = newCompass;
            }
            else
            {
                userInfo.currentBearing = userInfo.currentBearing + Constants.SmoothFactorCompass * (newCompass - userInfo.currentBearing);
            }
        }
        else
        {
            if (360.0 - Math.Abs(newCompass - userInfo.currentBearing) > Constants.SmoothThresholdCompass)
            {
                userInfo.currentBearing = newCompass;
            }
            else
            {
                if (userInfo.currentBearing > newCompass)
                {
                    userInfo.currentBearing = (userInfo.currentBearing + Constants.SmoothFactorCompass * ((360 + newCompass - userInfo.currentBearing) % 360) +
                                      360) % 360;
                }
                else
                {
                    userInfo.currentBearing = (userInfo.currentBearing - Constants.SmoothFactorCompass * ((360 - newCompass + userInfo.currentBearing) % 360) +
                                      360) % 360;
                }
            }
        }
        Vector3 cameraAngle = userInfo.mainCamera.transform.eulerAngles;
        cameraAngle.y = userInfo.currentBearing;
        userInfo.mainCamera.transform.eulerAngles = cameraAngle;
    }

    public void UpdatePosition()
    {
        //        z:
        //        +
        //        북
        // x: -서    동+ :x
        //        남
        //        -
        //        :z
        // y: 위+
        //    아래-
        Vector3 coordinateDifferenceFromStart = GPSCalulator.CoordinateDifference(userInfo.startingLatitude, userInfo.startingLongitude, userInfo.currentLatitude, userInfo.currentLongitude);
        //coordinateDifferenceFromStart.y = userInfo.currentAltitude - userInfo.currentLatitude;

        //        mainCamera.transform.position = coordinateDifferenceFromStart;

        userInfo.mainCamera.transform.position = coordinateDifferenceFromStart;
        //userInfo.mainCamera.transform.position = new Vector3(0, 0, -30);
    }

    IEnumerator GetGps()
    {
        //while true so this function keeps running once started.
        while (true)
        {
            // check if user has location service enabled
            if (!Input.location.isEnabledByUser)
                yield break;

            // Start service before querying location
            Input.location.Start(1f, .1f);
            Input.compass.enabled = true;

            // Wait until service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait < 1)
            {
                print("Timed out");
                yield break;
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                print("Unable to determine device location");
                yield break;
            }
            else
            {
                if (userInfo.setOriginalValues)
                {
                    userInfo.lastGpsMeasureTime = Time.time;

                    userInfo.startingLatitude = Input.location.lastData.latitude;
                    userInfo.startingLongitude = Input.location.lastData.longitude;
                    userInfo.startingAltitude = Input.location.lastData.altitude;
                    userInfo.startingBearing = Input.compass.trueHeading;

                    // 초기 월드 회전각
                    userInfo.mainCamera.transform.eulerAngles = new Vector3(0.0f, userInfo.startingBearing, 0.0f);
                    Debug.Log("startingBearing : " + userInfo.startingBearing);
                    userInfo.setOriginalValues = false;
                }

                //overwrite current lat and lon everytime
                userInfo.lastGpsMeasureTime = Time.time;

                userInfo.currentLatitude = Input.location.lastData.latitude;
                userInfo.currentLongitude = Input.location.lastData.longitude;
                userInfo.currentAltitude = Input.location.lastData.altitude;
                userInfo.currentBearing = Input.compass.trueHeading;

                // print debug info
                tb.GetComponent<Text>().text =
                    "Origin: " + userInfo.startingLatitude + ", " + userInfo.startingLongitude + ", " + userInfo.startingAltitude
                    + "\nGPS: " + userInfo.currentLatitude + ", " + userInfo.currentLongitude + ", " + userInfo.currentAltitude
                    + "\nplane position: " + ARObjectList[0].GameOBJ.transform.position.ToString()
                    + "\ncamera position: " + userInfo.mainCamera.transform.position
                    + "\ncamera angle: " + userInfo.mainCamera.transform.eulerAngles;
            }
            Input.location.Stop();
        }
    }
}