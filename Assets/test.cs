using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ADInfo
{
    public string name;
    public Vector3 GPSInfo;
    public float bearing;
    public string bannerUrl;
    public string sub;
    public Texture tex;
};

public class ARObject
{
    public GameObject GameOBJ
    {
        get { return GameOBJ; }
        set { GameOBJ = value; }
    }
    public ADInfo AdInfo
    {
        get { return AdInfo; }
        set { AdInfo = value; }
    }
    public void Create()
    {

    }
    public void Update()
    {

    }
    public void Destroy()
    {

    }
};

public class test : MonoBehaviour
{
    public GameObject tb1;
    public GameObject tb2;
    public GameObject tb3;

    public GameObject mainCamera;

    /*  Starting Infomation */
    public float startingBearing;
    private float startingLatitude;
    private float startingLongitude;
    private float startingAltitude;

    private float currentLatitude;
    private float currentLongitude;
    private float currentAltitude;

    private bool setOriginalValues = true;

    private Vector3 targetPosition;
    private Vector3 planeRelativePosition;
    private Vector3 planeGPSLocation;

    public UnityWebRequest textureWebRequest;

    private IEnumerator GetGPSCoroutine;

    void Start()
    {
        /*  Debug Info Print    */
        tb1 = GameObject.FindGameObjectWithTag("latitudeText");
        tb2 = GameObject.FindGameObjectWithTag("longitudeText");
        tb3 = GameObject.FindGameObjectWithTag("altitudeText");

        // GPS Coroutine Start
        StartCoroutine(GetGps());

        // main Camera Setting
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        /*  Test Data Create    */
        ADInfo tmp_ad_info = new ADInfo
        {
            name = "Google",
            GPSInfo = new Vector3(126.39394f, 0.0f, 37.26993f),
            bearing = 0.0f,
            bannerUrl = "https://lh4.googleusercontent.com/-v0soe-ievYE/AAAAAAAAAAI/AAAAAAADwkE/KyrKDjjeV1o/photo.jpg",
            sub = "",
            tex = null
        };

        GameObject tmpPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);

        ARObject test_set_1;

        test_set_1.
        ///////////////////////////////////////////////////

        // 모든 plane은 new Vector3(90.0f, -90.0f, 90.0f); 만큼 회전해야함 

        // create plane
        

    }

    void Update()
    {
        //        GetGPSCoroutine.MoveNext();
    }
    void CreateADPlane(ADInfo info)
    { 
        tmpPlane.name = info.name;
        
        // 텍스쳐 다운로드
        StartCoroutine(GetWebTexture(tmpPlane, info));
 
        // add to list
        planeList.Add(tmpPlane);
        ADList.Add(info);
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
                    startingLatitude = Input.location.lastData.latitude;
                    startingLongitude = Input.location.lastData.longitude;
                    startingAltitude = Input.location.lastData.altitude;
                    startingBearing = Input.compass.trueHeading;

                    mainCamera.transform.eulerAngles = new Vector3(0.0f, startingBearing, 0.0f);
                    Debug.Log("startingBearing : " + startingBearing);
                    setOriginalValues = false;
                }

                //overwrite current lat and lon everytime
                currentLatitude = Input.location.lastData.latitude;
                currentLongitude = Input.location.lastData.longitude;
                currentAltitude = Input.location.lastData.altitude;
                
                // print debug info
                tb1.GetComponent<Text>().text = "latitude : " + currentLatitude;
                tb2.GetComponent<Text>().text = "longitude : " + currentLongitude;
                tb3.GetComponent<Text>().text = "altitude : " + currentAltitude;
                //calculate the distance between where the player was when the app started and where they are now.

                UpdatePosition(startingLatitude, startingLongitude, startingAltitude, currentLatitude, currentLongitude, currentAltitude);
            }
            Input.location.Stop();
        }
    }

    IEnumerator GetWebTexture(GameObject planeInfo, ADInfo adInfo)
    {
        Texture tmpTexture;

        textureWebRequest = UnityWebRequestTexture.GetTexture(adInfo.bannerUrl);
        Debug.Log("Request to server!");
        yield return textureWebRequest.Send();

        Debug.Log("Create Texture!");
        tmpTexture = DownloadHandlerTexture.GetContent(textureWebRequest);
        Debug.Log("GetWeb " + tmpTexture.GetInstanceID());
        
        planeInfo.GetComponent<MeshRenderer>().material.mainTexture = tmpTexture;

        // load texture from file
        //
        //        byte[] fileData;
        //        var filePath = Application.dataPath + "/Resources/Textures/photo.jpg";
        //        Debug.Log(filePath);
        //        if (File.Exists(filePath))
        //        {
        //            Debug.Log("photo File exists!!");
        //            Texture2D tmp;
        //            fileData = File.ReadAllBytes(filePath);
        //            tmp = new Texture2D(512, 512, TextureFormat.ARGB32, false);
        //            tmp.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        //            myTexture = (Texture2D) tmp;
        //        }
        //        myTexture = (Texture)Resources.Load("photo");
    }
}