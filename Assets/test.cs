using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using KalmanDemo;


public class test : MonoBehaviour
{
    public GameObject tb;
    public GameObject tb1;
    public GameObject tb2;
    public GameObject tb3;
    public List<GameObject> planeList;
    public GameObject mainCamera;

    public float startingBearing;
    private float startingLatitude;
    private float startingLongitude;
    private float startingAltitude;

    private float currentLatitude;
    private float currentLongitude;
    private float currentAltitude;
    private float currentBearing;

    private bool setOriginalValues = true;

    private Vector3 targetPosition;
    private Vector3 planeRelativePosition;
    private Vector3 planeGPSLocation;

    private Kalman1D kalman_longitude;
    private Kalman1D kalman_latitude;
    private Kalman1D kalman_altitude;
    private Kalman1D kalman_bearing;
    private float lastGpsMeasureTime;

    private float speed = .1f;

    public Texture myTexture;
    public UnityWebRequest googleRequest;

    private IEnumerator GetGPSCoroutine;

    void Start()
    {
        //tb = GameObject.FindGameObjectWithTag("distanceText");
        //tb1 = GameObject.FindGameObjectWithTag("latitudeText");
        //tb2 = GameObject.FindGameObjectWithTag("longitudeText");
        //tb3 = GameObject.FindGameObjectWithTag("altitudeText");
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        Debug.Log("Create Plane");
        //GameObject googlePlane = new GameObject("google");
        //planeList.Add(googlePlane);

        GameObject googlePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        googlePlane.name = "google";
        planeList.Add(googlePlane);
        Debug.Log(planeList.Count);

        StartCoroutine(GetWebTexture());

        planeGPSLocation = new Vector3(126.6572f, 37.45068f, 52.9f);
        planeRelativePosition = new Vector3(0.0f, 0.0f, 30.0f);
        planeList[0].transform.position = planeRelativePosition;
        planeList[0].transform.eulerAngles = new Vector3(90.0f, -90.0f, 90.0f);
        StartCoroutine(GetGps());
        //        GetGPSCoroutine = GetGps();

        // Debug.Log(myTexture.GetInstanceID() + " " + myTexture.width + " " + myTexture.height);
    }

    void Update()
    {
        var currentTime = Time.time;
        var deltaTime = lastGpsMeasureTime - currentTime;
        if (deltaTime == 0)
            Debug.Log("delta Time");
        currentBearing = (float)kalman_bearing.Update(Input.compass.trueHeading, deltaTime);
        mainCamera.transform.eulerAngles = new Vector3(0.0f, currentBearing, 0.0f);
    }

    Vector2 DistanceAndBrearing(float latitude1, float longitude1, float latitude2, float longitude2)
    {
        const float earthRadiusMeter = 6378137.0f;
        var radianLatitude1 = latitude1 * Mathf.PI / 180.0f;
        var radianLatitude2 = latitude2 * Mathf.PI / 180.0f;
        var latitudeDifference = radianLatitude2 - radianLatitude1;

        var radianLongitude1 = longitude1 * Mathf.PI / 180.0f;
        var radianLongitude2 = longitude2 * Mathf.PI / 180.0f;
        var longitudeDifference = radianLongitude2 - radianLongitude1;

        var a = Mathf.Sin(latitudeDifference / 2.0f) * Mathf.Sin(latitudeDifference / 2.0f) +
                Mathf.Cos(radianLatitude1) * Mathf.Cos(radianLatitude2) *
                Mathf.Sin(longitudeDifference / 2.0f) * Mathf.Sin(longitudeDifference / 2.0f);
        var angualrDistance = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        var distance = earthRadiusMeter * angualrDistance;

        var y = Mathf.Sin(longitudeDifference) * Mathf.Cos(radianLatitude1);
        var x = Mathf.Cos(radianLatitude1) * Mathf.Sin(radianLatitude2) -
                Mathf.Sin(radianLatitude1) * Mathf.Cos(radianLatitude2) * Mathf.Cos(longitudeDifference);
        var bearing = Mathf.Atan2(y, x);

        //tb.GetComponent<Text>().text = "distance : " + distance;

        return new Vector2(distance, bearing);
    }

    Vector3 CoordinateDifference(float latitude1, float longitude1, float latitude2, float longitude2)
    {
        var distanceBearingVector = DistanceAndBrearing(latitude1, longitude1, latitude2, longitude2);
        var distance = distanceBearingVector[0];
        var bearing = distanceBearingVector[1];
        var xDifference = distance * Mathf.Cos(bearing);
        var yDifference = distance * Mathf.Sin(bearing);
        return new Vector3(yDifference, 0.0f, xDifference);
    }

    public void UpdatePosition(float lat1, float lon1, float alt1, float lat2, float lon2, float alt2)
    {
        var coordinateDifference = CoordinateDifference(lat1, lon1, lat2, lon2);
        coordinateDifference.y = alt2 - alt1;

        //set the target position of the ufo, this is where we lerp to in the update function
        targetPosition = coordinateDifference;
        //targetPosition = originalPosition - new Vector3(0, 0, distanceFloat * 12);
        //distance was multiplied by 12 so I didn't have to walk that far to get the UFO to show up closer
        Debug.Log("PlanePosion : " + planeList[0].transform.position);
        Debug.Log("planeRelativePosition : " + planeRelativePosition);
        Debug.Log("targetPosiion : " + targetPosition);
        planeList[0].transform.position = planeRelativePosition - targetPosition;

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
                if (setOriginalValues)
                {
                    lastGpsMeasureTime = Time.time;
                    currentLatitude = startingLatitude = Input.location.lastData.latitude;
                    currentLongitude = startingLongitude = Input.location.lastData.longitude;
                    currentAltitude = startingAltitude = Input.location.lastData.altitude;
                    currentBearing = startingBearing = Input.compass.trueHeading;

                    kalman_longitude = new Kalman1D();
                    kalman_longitude.Reset(0.1, 0.1, 0.1, 400.0, startingLongitude);
                    kalman_latitude = new Kalman1D();
                    kalman_latitude.Reset(0.1, 0.1, 0.1, 400.0, startingLatitude);
                    kalman_altitude = new Kalman1D();
                    kalman_altitude.Reset(0.1, 0.1, 0.1, 400.0, startingLatitude);
                    kalman_bearing = new Kalman1D();
                    kalman_bearing.Reset(0.1, 0.1, 0.1, 400.0, startingBearing);

                    float[] tmpBearing = new float[10];
                    for (int i = 0; i < 10; i++)
                    {
                        tmpBearing[i] = Input.compass.trueHeading;
                        Debug.Log(tmpBearing[i]);
                    }

                    mainCamera.transform.eulerAngles = new Vector3(0.0f, startingBearing, 0.0f);
                    Debug.Log("startingBearing : " + startingBearing);
                    setOriginalValues = false;
                }
                else
                {
                    //overwrite current lat and lon everytime
                    var currentTime = Time.time;
                    var deltaTime = lastGpsMeasureTime - currentTime;
                    if (deltaTime == 0)
                        Debug.Log("delta Time");
                    lastGpsMeasureTime = currentTime;
//                    currentLongitude = (float)kalman_longitude.Update(Input.location.lastData.longitude, deltaTime);
//                    currentLatitude = (float)kalman_latitude.Update(Input.location.lastData.latitude, deltaTime);
//                    currentAltitude = (float)kalman_altitude.Update(Input.location.lastData.altitude, deltaTime);
//                    currentBearing = (float)kalman_bearing.Update(Input.compass.trueHeading, deltaTime);
                    currentLongitude = Input.location.lastData.longitude;
                    currentLatitude = Input.location.lastData.latitude;
                    currentAltitude = Input.location.lastData.altitude;
                    currentBearing = Input.compass.trueHeading;
                    
                    mainCamera.transform.eulerAngles = new Vector3(0.0f, currentBearing, 0.0f);
                    //calculate the distance between where the player was when the app started and where they are now.
                    tb.GetComponent<Text>().text =
                        "Origin: " + startingLongitude + ", " + startingLatitude + ", " + startingAltitude +
                        "\nGPS: " + currentLongitude + ", " + currentLatitude + ", " + currentAltitude
                        + "\nplane angle: " + planeList[0].transform.eulerAngles.ToString()
                        + "\ncamera angle: " + mainCamera.transform.eulerAngles;

                    UpdatePosition(startingLatitude, startingLongitude, startingAltitude, currentLatitude,
                        currentLongitude, currentAltitude);

                    //tb.GetComponent<Text>().text += "\nRelative position: " + targetPosition;
                }
            }
            Input.location.Stop();
        }
    }

    IEnumerator GetWebTexture()
    {
        googleRequest = UnityWebRequestTexture.GetTexture("https://lh4.googleusercontent.com/-v0soe-ievYE/AAAAAAAAAAI/AAAAAAADwkE/KyrKDjjeV1o/photo.jpg");
        Debug.Log("Request to google server!");
        yield return googleRequest.Send();
        Debug.Log("Create Texture!");
        myTexture = DownloadHandlerTexture.GetContent(googleRequest);
        Debug.Log("GetWeb " + myTexture.GetInstanceID());

        //        byte[] fileData;
        //        var filePath = Application.dataPath + "/Resources/Textures/photo.jpg";
        //        Debug.Log(filePath);
        //        Debug.Log("1111");
        //        if (File.Exists(filePath))
        //        {
        //            Debug.Log("photo File exists!!");
        //            Texture2D tmp;
        //            fileData = File.ReadAllBytes(filePath);
        //            tmp = new Texture2D(512, 512, TextureFormat.ARGB32, false);
        //            Debug.Log(tmp.GetInstanceID());
        //            tmp.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        //            myTexture = (Texture2D) tmp;
        //            Debug.Log("2222");
        //        }
        //        myTexture = (Texture)Resources.Load("photo");
        planeList[0].GetComponent<MeshRenderer>().material.mainTexture = myTexture;
        Debug.Log(planeList[0].GetComponent<MeshRenderer>().material.shader);
        Debug.Log("3333");

        yield return null;
    }
}