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
    // 날짜, 시간 추가
    public string comment = "";
}

// abstract ARObject
abstract public class ARObject : MonoBehaviour
{
    public enum ARObjectType : int { ARObjectError = 0, ADPlane, ARComment };

    public GameObject GameOBJ
    {
        get { return GameOBJ; }
        set { GameOBJ = value; }
    }

    public ARObjectType ObjectType = ARObjectType.ARObjectError;

    abstract public void Create();
    abstract public void Update();
    abstract public void Destroy();// delete가 없음 null로 수정해서 참조 횟수를 줄임
};

public class ARPlane : ARObject {
    public ADInfo AdInfo
    {
        get { return AdInfo; }
        set { AdInfo = value; }
    }

    public ARPlane(ADInfo info)
    {
        AdInfo = info;
    }

    IEnumerator GetWebTexture(GameObject planeInfo, ADInfo adInfo)
    {
        Texture tmpTexture;

        UnityWebRequest textureWebRequest = UnityWebRequestTexture.GetTexture(adInfo.bannerUrl);
        Debug.Log("Request to server!");
        yield return textureWebRequest.Send();

        Debug.Log("Create Texture!");
        tmpTexture = DownloadHandlerTexture.GetContent(textureWebRequest);
        Debug.Log("GetWeb " + tmpTexture.GetInstanceID());

        planeInfo.GetComponent<MeshRenderer>().material.mainTexture = tmpTexture;
    }

    public override void Create()
    {
        // 텍스쳐 생성
        StartCoroutine(GetWebTexture(GameOBJ, AdInfo));

        ObjectType = ARObjectType.ADPlane;
        GameOBJ = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameOBJ.transform.eulerAngles = new Vector3(90.0f, -90.0f, 90.0f);
        // 모든 plane은 new Vector3(90.0f, -90.0f, 90.0f); 만큼 회전해야함 
    }

    public override void Update()
    {
        // position update
    }

    public override void Destroy()
    {
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
        throw new NotImplementedException();
    }

    public override void Update()
    {
        // Billboard? - calculate camera's inverse matrix
        throw new NotImplementedException();
    }

    public override void Destroy()
    {
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

    public GameObject mainCamera = null;
}

/// <summary>
/// End of Class Definition
/// </summary>
/// 

public class test : MonoBehaviour
{
    public GameObject tb1;
    public GameObject tb2;
    public GameObject tb3;

    /*  Starting Infomation */
    public UserInfo userInfo;

    public List<ARObject> ARObjectList;

    private Vector3 targetPosition;
    private Vector3 planeRelativePosition;
    private Vector3 planeGPSLocation;

    void Start()
    {
        /*  Debug Info Printer    */
        tb1 = GameObject.FindGameObjectWithTag("latitudeText");
        tb2 = GameObject.FindGameObjectWithTag("longitudeText");
        tb3 = GameObject.FindGameObjectWithTag("altitudeText");

        // GPS Coroutine Start
        StartCoroutine(GetGps());

        // Create User informaion
        userInfo = new UserInfo();
        userInfo.mainCamera = GameObject.FindGameObjectWithTag("MainCamera"); // main Camera Setting

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

        ARPlane tmp_plane = new ARPlane(tmp_ad_info);

        ARObjectList.Add(tmp_plane);
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }

    void Update()
    {
        //// Position Update
        //foreach(ARObject entity in ARObjectList) {
        //    entity.Update();
        //}
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
        //planeList[0].transform.position = planeRelativePosition - targetPosition;
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
                userInfo.currentLatitude = Input.location.lastData.latitude;
                userInfo.currentLongitude = Input.location.lastData.longitude;
                userInfo.currentAltitude = Input.location.lastData.altitude;

                // print debug info
                tb1.GetComponent<Text>().text = "latitude : " + userInfo.currentLatitude;
                tb2.GetComponent<Text>().text = "longitude : " + userInfo.currentLongitude;
                tb3.GetComponent<Text>().text = "altitude : " + userInfo.currentAltitude;
                //calculate the distance between where the player was when the app started and where they are now.

                UpdatePosition(userInfo.startingLatitude, userInfo.startingLongitude, userInfo.startingAltitude,
                    userInfo.currentLatitude, userInfo.currentLongitude, userInfo.currentAltitude);
            }
            Input.location.Stop();
        }
    }
}