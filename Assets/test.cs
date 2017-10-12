using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary>
/// Start of Class Definition
/// 
/// ADInfo - 광고 정보
/// CommentInfo - 코멘트 정보
/// ARObject abstract ARObject class
/// ARPlane - 광고 정보를 놓는 ARObject (ARObject 상속)
/// ARComment - 사용자 정보를 놓는 ARObject (ARObject 상속)
/// UserInfo - 사용자 정보 클래스 GPS정보, ID정보
/// </summary>
/// 

static class Constants
{
    public const float SmoothFactorCompass = 0.125f;
    public const float SmoothThresholdCompass = 45.0f;
}

public class ADInfo
{
    public string name = "";
    public Vector3 GPSInfo;
    public float bearing = 0.0f;
    public string bannerUrl = "";
    public string sub = "";
    public Texture tex = null;
};

public class CommentInfo
{
    public string id = "";
    public DateTime dateTime;
    public string comment = "";
}

public class StaticCoroutine : MonoBehaviour
{
    private static StaticCoroutine mInstance = null;
    private static StaticCoroutine instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = GameObject.FindObjectOfType(typeof(StaticCoroutine)) as StaticCoroutine;

                if (mInstance == null)
                {
                    mInstance = new GameObject("StaticCoroutine").AddComponent<StaticCoroutine>();
                }
            }
            return mInstance;
        }
    }

    void Awake()
    {
        if (mInstance == null)
        {
            mInstance = this as StaticCoroutine;
        }
    }

    IEnumerator Perform(IEnumerator coroutine)
    {
        yield return StartCoroutine(coroutine);
        //Die();
    }

    public static void DoCoroutine(IEnumerator coroutine)
    {
        //actually this point will be start coroutine
        instance.StartCoroutine(instance.Perform(coroutine));
    }

    void Die()
    {
        mInstance = null;
        Destroy(gameObject);
    }

    void OnApplicationQuit()
    {
        mInstance = null;
    }
}

// abstract ARObject
abstract public class ARObject
{
    public enum ARObjectType : int { ARObjectError = 0, ADPlane, ARComment };

    public GameObject GameOBJ;

    public UserInfo userInfo;

    public ARObjectType ObjectType;

    public Vector2 DistanceAndBrearing(float latitude1, float longitude1, float latitude2, float longitude2)
    {
        Debug.Log("in DistanceAndBrearing : " + latitude1 + " " + longitude1 + " " + latitude2 + " " + longitude2);
        const float earthRadiusMeter = 6378137.0f;
        float radianLatitude1 = latitude1 * Mathf.PI / 180.0f;
        float radianLatitude2 = latitude2 * Mathf.PI / 180.0f;
        float latitudeDifference = radianLatitude2 - radianLatitude1;

        float radianLongitude1 = longitude1 * Mathf.PI / 180.0f;
        float radianLongitude2 = longitude2 * Mathf.PI / 180.0f;
        float longitudeDifference = radianLongitude2 - radianLongitude1;

        float a = Mathf.Sin(latitudeDifference / 2.0f) * Mathf.Sin(latitudeDifference / 2.0f) +
                Mathf.Cos(radianLatitude1) * Mathf.Cos(radianLatitude2) *
                Mathf.Sin(longitudeDifference / 2.0f) * Mathf.Sin(longitudeDifference / 2.0f);
        float angualrDistance = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        float distance = earthRadiusMeter * angualrDistance;

        float y = Mathf.Sin(longitudeDifference) * Mathf.Cos(radianLatitude1);
        float x = Mathf.Cos(radianLatitude1) * Mathf.Sin(radianLatitude2) -
                Mathf.Sin(radianLatitude1) * Mathf.Cos(radianLatitude2) * Mathf.Cos(longitudeDifference);
        float bearing = Mathf.Atan2(y, x);

        return new Vector2(distance, bearing);
    }

    public Vector3 CoordinateDifference(float latitude1, float longitude1, float latitude2, float longitude2)
    {
        Debug.Log("in CoordinateDifference : " + latitude1 + " " + longitude1 + " " + latitude2 + " " + longitude2);
        Vector3 distanceBearingVector = DistanceAndBrearing(latitude1, longitude1, latitude2, longitude2);
        float distance = distanceBearingVector[0];
        float bearing = distanceBearingVector[1];
        float xDifference = distance * Mathf.Cos(bearing);
        float yDifference = distance * Mathf.Sin(bearing);
        Debug.Log("Distance : " + distance + " bearing : " + bearing);
        return new Vector3(yDifference, 0.0f, xDifference);
    }

    abstract public void Create();
    abstract public void Update();
    abstract public void Destroy();// delete가 없음 null로 수정해서 참조 횟수를 줄임
};

public class ARPlane : ARObject
{
    public ADInfo AdInfo;

    public ARPlane(ADInfo info, UserInfo info2)
    {
        AdInfo = info;
        userInfo = info2;
        Create();
    }

    IEnumerator GetWebTex()
    {
        Texture tmpTexture;

        UnityWebRequest textureWebRequest = UnityWebRequestTexture.GetTexture(AdInfo.bannerUrl);
        Debug.Log(AdInfo.name + " Request to server!");
        yield return textureWebRequest.Send();

        Debug.Log(AdInfo.name + " Create Texture!");
        tmpTexture = DownloadHandlerTexture.GetContent(textureWebRequest);
        Debug.Log(AdInfo.name + "GetWeb " + tmpTexture.GetInstanceID());

        GameOBJ.GetComponent<MeshRenderer>().material.mainTexture = tmpTexture;
    }

    public override void Create()
    {
        // 텍스쳐 생성
        // StaticCorutine은 처음 호출시 생성되며 수행 이후 파괴되지 않고 필요할때 마다 이용됨.
        StaticCoroutine.DoCoroutine(GetWebTex());

        ObjectType = ARObjectType.ADPlane;
        GameOBJ = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameOBJ.name = AdInfo.name;

        // 초기 포지션 설정
        Debug.Log("plane gps info : " + AdInfo.GPSInfo[0] + " " + AdInfo.GPSInfo[1] + " " + AdInfo.GPSInfo[2]);
        Vector3 tmp = CoordinateDifference(userInfo.currentLatitude, userInfo.currentLongitude, AdInfo.GPSInfo[0], AdInfo.GPSInfo[1]);
        //tmp.y = userInfo.currentAltitude - AdInfo.GPSInfo[2];
        tmp.y = 0.0f;

        GameOBJ.transform.position = tmp + userInfo.mainCamera.transform.position;
        //GameOBJ.transform.position = new Vector3(0.0f, 0.0f, 30.0f);
        GameOBJ.transform.eulerAngles = new Vector3(90.0f, -90.0f, 90.0f); // gimbal lock이 발생하는 것 같음 90 0 -180으로 됨
        //GameOBJ.transform.rotation = Quaternion.Euler(90.0f, -90.0f, 90.0f);
        // 모든 plane은 new Vector3(90.0f, -90.0f, 90.0f); 만큼 회전해야함 
    }

    public override void Update()
    {
        // position update or animation update
    }

    public override void Destroy()
    {
        MonoBehaviour.Destroy(GameOBJ); // object 제거, Null ptr 설정
        GameOBJ = null;
        AdInfo = null;
    }
}

public class ARComment : ARObject
{
    public CommentInfo Comment
    {
        get { return Comment; }
        set { Comment = value; }
    }

    public ARComment(CommentInfo info)
    {
        Comment = info;
    }

    public override void Create()
    {
        // Mesh Type Definition
        ObjectType = ARObjectType.ARComment;
    }

    public override void Update()
    {
        // Billboard? - calculate camera's inverse matrix
        throw new NotImplementedException();
    }

    public override void Destroy()
    {
        MonoBehaviour.Destroy(GameOBJ); // object 제거, Null ptr 설정
        GameOBJ = null;
        Comment = null;
    }
}

public class UserInfo
{
    public bool setOriginalValues = true;

    public float startingBearing = 0.0f;
    public float startingLatitude = 0.0f;
    public float startingLongitude = 0.0f;
    public float startingAltitude = 0.0f;

    public float currentBearing = 0.0f;
    public float currentLatitude = 0.0f;
    public float currentLongitude = 0.0f;
    public float currentAltitude = 0.0f;
    public float lastGpsMeasureTime = 0.0f;

    public GameObject mainCamera = null;
}

/// <summary>
/// End of Class Definition
/// </summary>
///

public class test : MonoBehaviour
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

        while (userInfo.setOriginalValues)
        {
            new WaitForSeconds(1.0f); //waiting for gps info initialize
        }

        // Create Object List
        ARObjectList = new List<ARObject>();

        /*  Test Data Create    */
        ADInfo tmp_ad_info = new ADInfo
        {
            name = "Google",
            GPSInfo = new Vector3(126.6580f, 37.4507f, 0.0f),
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
        Vector3 coordinateDifferenceFromStart = CoordinateDifference(userInfo.startingLatitude, userInfo.startingLongitude, userInfo.currentLatitude, userInfo.currentLongitude);
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
                    "Origin: " + userInfo.startingLongitude + ", " + userInfo.startingLatitude + ", " + userInfo.startingAltitude
                    + "\nGPS: " + userInfo.currentLongitude + ", " + userInfo.currentLatitude + ", " + userInfo.currentAltitude
                    + "\nplane position: " + ARObjectList[0].GameOBJ.transform.position.ToString()
                    + "\ncamera position: " + userInfo.mainCamera.transform.position
                    + "\ncamera angle: " + userInfo.mainCamera.transform.eulerAngles;
            }
            Input.location.Stop();
        }
    }
}