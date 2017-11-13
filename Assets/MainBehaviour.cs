using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public class JsonPlaneData
{
    public int ad_no;
    public string name;
    public float latitude;
    public float longitude;
    public float altitude;
    public float bearing;
    public float width;
    public float height;
    public string banner_url;
    public string texture_url;
}

[System.Serializable]
public class JsonPointData
{
    public int pointReward;
    public bool clickLogFlag;
    
}

[System.Serializable]
public class JsonPlaneDataArray
{
    public JsonPlaneData[] data;
}

/// <summary>
/// 앱 시작 시 실행되는 메인 Behavior.
/// </summary>
public class MainBehaviour : MonoBehaviour
{
    // toast
    string toastString;
    AndroidJavaObject currentActivity;

    /// <summary>
    /// 텍스트 출력창 (디버깅용)</summary>
    public GameObject TextBox;

    /// <summary>
    /// 이 앱에 로드된 모든 AR 오브젝트의 목록</summary>
    private Dictionary<int, ArObject> _arObjects;

    /// <summary>
    /// 현재 사용자 정보</summary>
    //private ClientInfo _clientInfo;
    private ClientInfo _clientInfo;

    private UserInfo _userInfo;

    JsonPointData _pointData;

    private void Start()
    {
        //GameObject sessionInfo = GameObject.FindGameObjectWithTag("Loadingscenemanager");
        //Debug.Log("꺄" + sessionInfo.GetComponent<Text>().text);

        // 사용자 정보 생성
        //_clientInfo = new ClientInfo();
        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();

        _clientInfo.MainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        _userInfo = GameObject.FindGameObjectWithTag("UserInfo").GetComponent<UserInfo>();

        // GPS 좌표 정보 갱신용 코루틴 시작
        StartCoroutine(GetGps());
        // AR 오브젝트 리스트 초기화
        _arObjects = new Dictionary<int, ArObject>();

        // 주변 오브젝트 목록 주기적 업데이트를 위한 코루틴 시작
        StartCoroutine(GetPlaneList(5.0f));
    }

    private void Update()
    {
        //UpdateCameraBearing();
        UpdateCameraPosition();

        // 화면을 터치했을 때
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = Input.GetTouch(0).position;

            Ray ray = Camera.main.ScreenPointToRay(new Vector3(touchPosition.x, touchPosition.y, 0.0f));

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    break;

                case TouchPhase.Moved:
                    break;

                case TouchPhase.Stationary:
                    break;

                case TouchPhase.Ended:
                    RaycastHit hitObject;
                    Physics.Raycast(ray, out hitObject, Mathf.Infinity);
                    int adNumber = hitObject.collider.GetComponent<DataContainer>().AdNum;
                    StartCoroutine(pointCoroutine(adNumber));
                    Application.OpenURL(hitObject.collider.GetComponent<DataContainer>().BannerUrl);



                    break;

                case TouchPhase.Canceled:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //위치 정보 출력(디버그)
        TextBox.GetComponent<Text>().text =
            "Origin: " + _clientInfo.StartingLatitude + ", " + _clientInfo.StartingLongitude + ", " + _clientInfo.StartingAltitude
            + "\nGPS: " + _clientInfo.CurrentLatitude + ", " + _clientInfo.CurrentLongitude + ", " + _clientInfo.CurrentAltitude
            + "\nBearing: " + _clientInfo.CurrentBearing
            + "\ncamera position: " + _clientInfo.MainCamera.transform.position
            + "\ncamera angle: " + _clientInfo.MainCamera.transform.eulerAngles.x.ToString() + ", " + (_clientInfo.MainCamera.transform.eulerAngles.y + _clientInfo.StartingBearing).ToString() + ", "
            + _clientInfo.MainCamera.transform.eulerAngles.z.ToString()
            + "\nObject Count: " + _arObjects.Count
            + "\nCamera to object: ";


        foreach (ArObject entity in _arObjects.Values)
        {
            Vector3 cameraToObject = entity.GameObj.transform.position - _clientInfo.MainCamera.transform.position;
            TextBox.GetComponent<Text>().text += cameraToObject + "\n";
        }

        //+ "\nplane position: " + _arObjects[0].GameObj.transform.position.ToString()
        //// ARObject Update (animation)
        //foreach(ARObject entity in ARObjectList) {
        //    entity.Update();
        //}
    }

    /// <summary>
    /// 나침반 센서 값을 카메라 방위각에 적용한다.
    /// </summary>
    private void UpdateCameraBearing()
    {
        // 방위각
        //          0.0:
        //          북
        // 270.0:서    동:90.0
        //          남
        //          :180.0
        // 로우 패스 (스무딩) 필터
        float newBearing = Input.compass.trueHeading;
        if (Mathf.Abs(newBearing - _clientInfo.CurrentBearing) < 180)
        {
            if (Math.Abs(newBearing - _clientInfo.CurrentBearing) > Constants.SmoothThresholdCompass)
            {
                _clientInfo.CurrentBearing = newBearing;
            }
            else
            {
                _clientInfo.CurrentBearing = _clientInfo.CurrentBearing + Constants.SmoothFactorCompass * (newBearing - _clientInfo.CurrentBearing);
            }
        }
        else
        {
            if (360.0 - Math.Abs(newBearing - _clientInfo.CurrentBearing) > Constants.SmoothThresholdCompass)
            {
                _clientInfo.CurrentBearing = newBearing;
            }
            else
            {
                if (_clientInfo.CurrentBearing > newBearing)
                {
                    _clientInfo.CurrentBearing = (_clientInfo.CurrentBearing + Constants.SmoothFactorCompass * ((360 + newBearing - _clientInfo.CurrentBearing) % 360) +
                                      360) % 360;
                }
                else
                {
                    _clientInfo.CurrentBearing = (_clientInfo.CurrentBearing - Constants.SmoothFactorCompass * ((360 - newBearing + _clientInfo.CurrentBearing) % 360) +
                                      360) % 360;
                }
            }
        }

        Vector3 newCameraAngle = _clientInfo.MainCamera.transform.eulerAngles;
        newCameraAngle.y = _clientInfo.CurrentBearing;
        _clientInfo.MainCamera.transform.eulerAngles = newCameraAngle;
    }

    /// <summary>
    /// _userInfo의 GPS값을 카메라 위치에 적용한다.
    /// </summary>
    private void UpdateCameraPosition()
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
        Vector3 coordinateDifferenceFromStart = GpsCalulator.CoordinateDifference(
            _clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude,
            _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, _clientInfo.StartingAltitude);

        _clientInfo.MainCamera.transform.position = coordinateDifferenceFromStart;
    }

    /// <summary>
    /// 코루틴 주기마다 _userInfo의 GPS 값을 업데이트한다.
    /// </summary>
    private IEnumerator GetGps()
    {
        // 앱이 켜져 있는 동안 계속 실행.
        while (true)
        {
            // 위치 서비스가 켜져 있는지 체크
            if (!Input.location.isEnabledByUser)
                yield break;

            // 위치를 요청하기 전 서비스 시작
            Input.location.Start(1f, .1f);
            Input.compass.enabled = true;

            // 서비스 초기화 대기
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // 서비스가 20초 동안 켜지지 않았을 때
            if (maxWait < 1)
            {
                Debug.Log("Timed out");
                yield break;
            }

            // Connection 실패
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.Log("Unable to determine device location");
                yield break;
            }

            _clientInfo.LastGpsMeasureTime = Time.time;

            _clientInfo.CurrentLatitude = Input.location.lastData.latitude;
            _clientInfo.CurrentLongitude = Input.location.lastData.longitude;
            _clientInfo.CurrentAltitude = Input.location.lastData.altitude;
            _clientInfo.CurrentBearing = Input.compass.trueHeading;


            // 초기 위치 정보 저장
            if (!_clientInfo.OriginalValuesAreSet)
            {
                _clientInfo.StartingLatitude = _clientInfo.CurrentLatitude;
                _clientInfo.StartingLongitude = _clientInfo.CurrentLongitude;
                _clientInfo.StartingAltitude = _clientInfo.CurrentAltitude;
                _clientInfo.StartingBearing = _clientInfo.CurrentBearing;

                _clientInfo.OriginalValuesAreSet = true;
            }
            Input.location.Stop();
        }
    }

    /// <summary>
    /// 일정 시간마다 서버에 사용자의 GPS정보로 HTTP request를 보내서 현재 위치 주변에 있는 Plane List를 받아온다.
    /// </summary>
    private IEnumerator GetPlaneList(float intervalInSecond = 5.0f)
    {
        if (!_clientInfo.OriginalValuesAreSet)
            yield return new WaitUntil(() => _clientInfo.OriginalValuesAreSet);

        while (true)
        {
            string latitude = _clientInfo.CurrentLatitude.ToString();
            string longitude = _clientInfo.CurrentLongitude.ToString();
            string altitude = _clientInfo.CurrentAltitude.ToString();

            // 테스트용 GPS
            //latitude = "37.450571";
            //longitude = "126.656903";
            //altitude = "53.000000";

            WWWForm form = new WWWForm();
            form.AddField("latitude", latitude);
            form.AddField("longitude", longitude);
            form.AddField("altitude", altitude);

            // getAdObject
            using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/getGPS_distance.php", form))
            {
                yield return www.Send();

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log(www.error);
                    // TODO: 필요시 재시도
                }
                else
                {
                    // Json을 받아와 오브젝트로 변환
                    string fromServJson = www.downloadHandler.text;
                    Debug.Log(fromServJson);

                    JsonPlaneDataArray newObjectList = JsonUtility.FromJson<JsonPlaneDataArray>(fromServJson);

                    var a = newObjectList.data;
                    if (newObjectList.data.Length == 0)
                    {
                        // 받아온 리스트에 아무것도 없는 경우 - 리스트 클리어
                        foreach (var arObject in _arObjects.Values)
                            arObject.Destroy();

                        _arObjects.Clear();
                    }
                    else
                    {
                        // 오브젝트 ID 모으기
                        var newObjectIds = new List<int>();
                        foreach (var newObject in newObjectList.data)
                            newObjectIds.Add(newObject.ad_no);

                        // 받아온 리스트 없는 ArObject 삭제
                        foreach (var arObject in _arObjects)
                        {
                            if (!newObjectIds.Contains(arObject.Key))
                            {
                                arObject.Value.Destroy();
                                _arObjects.Remove(arObject.Key);
                            }
                        }

                        // 받아온 리스트에서 새로 생긴 ArObject 생성
                        foreach (JsonPlaneData jsonArObject in newObjectList.data)
                        {
                            // 기존 리스트에 이미 있는 경우 안 만듦
                            if (_arObjects.Keys.Contains(jsonArObject.ad_no))
                                continue;

                            // 새로운 ArObject 생성
                            AdInfo tmpAdInfo = new AdInfo
                            {
                                Id = jsonArObject.ad_no,
                                Name = jsonArObject.name,
                                GpsInfo = new Vector3(jsonArObject.latitude, jsonArObject.longitude,
                                    jsonArObject.altitude),
                                Bearing = jsonArObject.bearing,
                                TextureUrl = jsonArObject.texture_url,
                                BannerUrl = jsonArObject.banner_url,
                                TextAlternateToTexture = "",
                                AdTexture = null,
                                Width = jsonArObject.width,
                                Height = jsonArObject.height
                            };

                            // texture url정보 받아와서 수정 필요.
                            _arObjects[jsonArObject.ad_no] = new ArPlane(tmpAdInfo, _clientInfo);
                        }
                    }
                }
            }

            // 5초에 한번씩 실행
            yield return new WaitForSeconds(intervalInSecond);
        }
    }

    private IEnumerator pointCoroutine(int adNumber) {

        //showToastOnUiThread("adnumber "+adNumber);

        string userID = _userInfo.UserId;
        //showToastOnUiThread("userid " + userID);
        JsonPointData pointInfo;
        string fromServJson;
        WWWForm checkLogForm = new WWWForm();
        checkLogForm.AddField("Input_user", userID);
        checkLogForm.AddField("Input_ad", adNumber);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/check_log.php", checkLogForm))
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
                Debug.Log(www.error);
            else
            {

                //Debug.Log("check clickLogFlag!");

                fromServJson = www.downloadHandler.text;
                pointInfo = JsonUtility.FromJson<JsonPointData>(fromServJson);

                //showToastOnUiThread("check clickLogFlag!" + pointInfo.clickLogFlag);

                if (pointInfo.clickLogFlag)
                {
                    WWWForm adInfoForm = new WWWForm();
                    adInfoForm.AddField("Input_ad", adNumber);


                    using (UnityWebRequest www2 = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/adinfo.php", adInfoForm))
                    {
                        yield return www2.Send();

                        if (www2.isNetworkError || www2.isHttpError)
                            Debug.Log(www2.error);
                        //showToastOnUiThread(www2.error);
                        else
                        {
                            Debug.Log("get point!");

                            fromServJson = www2.downloadHandler.text;
                            pointInfo = JsonUtility.FromJson<JsonPointData>(fromServJson);

                            int pointNumber = pointInfo.pointReward;
                            //showToastOnUiThread("user id: "+userID+", ad"+adNumber+"의 adpoint: " + pointNumber);
                            WWWForm pointForm = new WWWForm();
                            pointForm.AddField("Input_point", pointNumber);
                            pointForm.AddField("Input_user", userID);
                            pointForm.AddField("Input_ad", adNumber);

                            using (UnityWebRequest www3 = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/earn_point.php", pointForm))
                            {
                                yield return www3.Send();

                                if (www3.isNetworkError || www3.isHttpError)
                                    Debug.Log(www3.error);
                                else
                                {
                                    Debug.Log("earn point!");
                                    showToastOnUiThread("earn point: " + pointNumber);
                                }
                            }


                        }
                    }

                    StartCoroutine(getPointCoroutine());

                }
                else showToastOnUiThread("You already clicked!");
            }
        }
    }

    public void ToOptionScene()
    {

        SceneManager.LoadScene("Option");
    }

    void showToastOnUiThread(string toastString)
    {
        AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

        currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        this.toastString = toastString;

        currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(showToast));
    }

    void showToast()
    {
        Debug.Log("Running on UI thread");
        AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
        AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", toastString);
        AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_SHORT"));
        toast.Call("show");
    }

    private IEnumerator getPointCoroutine()
    {

        //showToastOnUiThread("adnumber "+adNumber);

        string userID = _userInfo.UserId;


        string fromServJson;
        WWWForm checkPointForm = new WWWForm();
        checkPointForm.AddField("Input_user", userID);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/show_point.php", checkPointForm))
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
                Debug.Log(www.error);
            else
            {
                fromServJson = www.downloadHandler.text;
                _pointData = JsonUtility.FromJson<JsonPointData>(fromServJson);
                _userInfo.Point = _pointData.pointReward;
            }
        }
    }

    public void onClickHorseBtn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("horse", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void onClickGift1Btn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("gift_1", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void onClickButterflyBtn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("Butterfly", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void createObject(string typeName, Vector3 unityPosition)
    {
        //Instantiate(obj, new Vector3(40, -1, 0.0f), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/" + typeName), unityPosition, Quaternion.identity);
        
        string x = _clientInfo.CurrentLatitude.ToString();
        string y = _clientInfo.CurrentLongitude.ToString();
        string z = _clientInfo.CurrentAltitude.ToString();
        string bearing = _clientInfo.CurrentBearing.ToString();
        StartCoroutine(ObjectCreateCoroutine(x, y, z, typeName, _userInfo.UserId, bearing));

    }

    private IEnumerator ObjectCreateCoroutine(string x, string y, string z, string typeName, string id, string bearing)
    {
        
        WWWForm form = new WWWForm();
        form.AddField("latitude", x);
        form.AddField("longitude", y);
        form.AddField("altitude", z);
        form.AddField("typeName", typeName);
        form.AddField("user", id);
        form.AddField("bearing", bearing);
        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/add_3d_Object.php", form))
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("create 3dObject!");
            }
        }
    }

    
}