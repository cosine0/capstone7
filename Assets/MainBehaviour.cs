using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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
public class Json3dData
{
    public int object_no;
    public string typeName;
    public float ad_userid;
    public float latitude;
    public float longitude;
    public float altitude;
    public float bearing;
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

[System.Serializable]
public class Json3dDataArray
{
    public Json3dData[] data;
}

/// <summary>
/// 인앱 scene에 필요한 스크립트를 갖는 Behaviour.
/// </summary>
public class MainBehaviour : MonoBehaviour
{
    // 안드로이드 Toast를 띄울 때 사용되는 임시 객체
    private string _toastString;
    private AndroidJavaObject _currentActivity;

    /// <summary>
    /// 텍스트 출력창 (디버깅용)
    /// </summary>
    public GameObject TextBox;

    /// <summary>
    /// 이 앱에 로드된 모든 AR 오브젝트의 목록. Ad Number를 키, ArObject를 값으로 가진다.
    /// </summary>
    private Dictionary<int, ArObject> _arObjects;

    private Dictionary<int, ArObject> _ar3dObjects;

    /// <summary>
    /// 클라이언트 위치, 옵션 정보
    /// </summary>
    private ClientInfo _clientInfo;
    /// <summary>
    /// 현재 로그인한 사용자 정보
    /// </summary>
    private UserInfo _userInfo;

    public GameObject inAppCanvas;
    public GameObject commentViewCanvas;
    public GameObject object3DMenu;

    JsonPointData _pointData;

    private void Start()
    {
        // DontDestroyOnLoad 객체인 ClientInfo, UserInfo 가져오기
        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();
        _userInfo = GameObject.FindGameObjectWithTag("UserInfo").GetComponent<UserInfo>();

        // scene에 있는 AR 카메라 가져오기
        _clientInfo.MainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        // GPS 좌표 정보 갱신용 코루틴 시작
        StartCoroutine(GetGps(Constants.GpsMeasureIntervalInSecond));

        // AR 오브젝트 목록 초기화
        _arObjects = new Dictionary<int, ArObject>();
        _ar3dObjects = new Dictionary<int, ArObject>();

        StartCoroutine(CollectBearingDifference(Constants.CompassMeasureIntervalInSecond));
        StartCoroutine(UpdateBearingOffset(Constants.CompassMeasureIntervalInSecond));

        // 주변 오브젝트 목록 주기적 업데이트를 위한 코루틴 시작
        StartCoroutine(GetArObjectList(5.0f));
        StartCoroutine(Get3dArObjectList(5.0f));
        //StartCoroutine(GetCommentCanvas(5.0f));
    }

    private void Update()
    {
        //UpdateCameraBearing();
        UpdateCameraPosition();

        if (_arObjects.Count != 0)
        {
            foreach (var arObject in _arObjects.Values)
            {
                if (arObject.ObjectType == ArObjectType.AdPlane)
                    arObject.Update();
            }
        }
        
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    break;

                case TouchPhase.Moved:
                    break;

                case TouchPhase.Stationary:
                    break;

                case TouchPhase.Ended:
                    Debug.Log("Touch Ended!");
                    // 터치를 뗀 경우 - 터치한 위치의 광선에 닿는 물체의 BannerUrl을 브라우저에서 열고, 포인트 적립을 서버에 요청한다.

                    Vector2 touchPosition = Input.GetTouch(0).position;
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(touchPosition.x, touchPosition.y, 0.0f));
                    
                    RaycastHit hitObject;

                    GraphicRaycaster _mainCanvas = GameObject.FindGameObjectWithTag("InAppCanvas").GetComponent<GraphicRaycaster>();
                    List<RaycastResult> _results = new List<RaycastResult>();
                    PointerEventData _ped = new PointerEventData(null);
                    _ped.position = touch.position;
                    _mainCanvas.Raycast(_ped, _results);

                    // UI를 터치하지 않은 경우
                    if (_results.Count == 0)
                    {

                        if (Physics.Raycast(ray, out hitObject, Mathf.Infinity))
                        {
                            if (hitObject.collider.GetComponent<DataContainer>().ObjectType == ArObjectType.AdPlane)
                            {
                                Debug.Log("ArPlane Touch!");
                                Application.OpenURL(hitObject.collider.GetComponent<DataContainer>().BannerUrl);
                                int adNumber = hitObject.collider.GetComponent<DataContainer>().AdNum;
                                StartCoroutine(EarnPointCoroutine(adNumber));
                            }
                            else if (hitObject.collider.GetComponent<DataContainer>().ObjectType == ArObjectType.ArComment)
                            {
                                Debug.Log("CommentCanvas Touch!");
                                //commentCanvas.SetActive(true);
                                //inAppCanvas.SetActive(false);

                                // 연관 광고 정보 패싱
                                commentViewCanvas.GetComponent<CommentCanvasBehaviour>().adNum = hitObject.collider.GetComponent<DataContainer>().AdNum;
                                // CreateCommentView();
                                // - list clear
                                // - get comment list
                                // - list add
                                // - scroll view area calculate
                            }
                        }
                    }

                    break;

                case TouchPhase.Canceled:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // 위치 정보 출력 (디버그)
        TextBox.GetComponent<Text>().text =
            "Origin: " + _clientInfo.StartingLatitude + ", " + _clientInfo.StartingLongitude + ", " +
            _clientInfo.StartingAltitude
            + "\nGPS: " + _clientInfo.CurrentLatitude + ", " + _clientInfo.CurrentLongitude + ", " +
            _clientInfo.CurrentAltitude
            + "\ncamera position: " + _clientInfo.MainCamera.transform.position
            + "\ncamera angle: " + _clientInfo.MainCamera.transform.eulerAngles
            + "\ncompass: " + Input.compass.trueHeading
            + "\nCurrent (compass-gyro): " + ((Input.compass.trueHeading - _clientInfo.MainCamera.transform.eulerAngles.y) % 360f + 360f) % 360f
            + "\nAverage (compass-gyro): " + (_clientInfo.CorrectedBearingOffset % 360f + 360f) % 360f
            + "\nCorrected Bearing: " + (_clientInfo.CurrentBearing % 360f + 360f) % 360f
            + "\nObject Count: " + _arObjects.Count
            + "\nCamera to planes:\n";

        // 물체 위치 출력 (디버그)
        foreach (ArObject entity in _arObjects.Values)
        {
            Vector3 cameraToObject = entity.GameObj.transform.position - _clientInfo.MainCamera.transform.position;
            TextBox.GetComponent<Text>().text += cameraToObject + "\n";
        }

        // 물체 위치 출력 (디버그)
        TextBox.GetComponent<Text>().text += "Camera to 3D objects:\n";
        foreach (ArObject entity in _ar3dObjects.Values)
        {
            Vector3 cameraToObject = entity.GameObj.transform.position - _clientInfo.MainCamera.transform.position;
            TextBox.GetComponent<Text>().text += cameraToObject + "\n";
        }
    }

    /// <summary>
    /// <see cref="_clientInfo"/>의 GPS값을 카메라 위치에 적용한다.
    /// </summary>
    private void UpdateCameraPosition()
    {
        // 지구 상의 방위와 유니티 상의 좌표 매칭:
        //        z:
        //        +
        //        북
        // x: -서    동+ :x
        //        남
        //        -
        //        :z
        // y: 위+
        //    아래-

        // 앱을 켠 순간의 GPS 좌표 (_clientInfo.StartingXXX)에 대응하는 유니티 좌표와
        // 현재 GPS 좌표 (_clientInfo.CurrentXXX)에 대응하는 유니티 좌표의 차를 구한다.
        Vector3 coordinateDifferenceFromStart = GpsCalulator.CoordinateDifference(
            _clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude,
            _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, _clientInfo.StartingAltitude);

        // GPS 고도는 무시
        coordinateDifferenceFromStart.y = 0.0f;

        // 카메라를 유니티 상의 현재 사용자 위치로 옮기기
        _clientInfo.MainCamera.transform.position = coordinateDifferenceFromStart;
    }

    /// <summary>
    /// 코루틴 주기마다 <see cref="_clientInfo"/>의 GPS 값을 업데이트한다.
    /// </summary>
    private IEnumerator GetGps(float intervalInSecond = 0.3f)
    {
        // 앱이 켜져 있는 동안 계속 실행.
        while (true)
        {
            // 위치 서비스가 켜져 있는지 체크
            if (!Input.location.isEnabledByUser)
            {
                _clientInfo.LodingCanvas.GetComponent<LoadingCanvasBehaviour>().HideLodingCanvas();
                yield break;
            }

            // 위치 정보 초기화 전인 경우 - 위치 서비스 시작
            if (!_clientInfo.OriginalValuesAreSet)
            {
                // 위치를 요청하기 전 서비스 시작
                Input.location.Start(1.0f, 0.1f);
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
            }

            _clientInfo.LastGpsMeasureTime = Time.time;

            // DontDestroyOnLoad 오브젝트인 _clientInfo의 현재 위치 업데이트
            _clientInfo.CurrentLatitude = Input.location.lastData.latitude;
            _clientInfo.CurrentLongitude = Input.location.lastData.longitude;
            _clientInfo.CurrentAltitude = Input.location.lastData.altitude;

            // 초기 위치 정보 저장
            if (!_clientInfo.OriginalValuesAreSet)
            {
                _clientInfo.StartingLatitude = _clientInfo.CurrentLatitude;
                _clientInfo.StartingLongitude = _clientInfo.CurrentLongitude;
                _clientInfo.StartingAltitude = _clientInfo.CurrentAltitude;
                _clientInfo.CorrectedBearingOffset = Input.compass.trueHeading;

                _clientInfo.OriginalValuesAreSet = true;
                _clientInfo.LodingCanvas.GetComponent<LoadingCanvasBehaviour>().HideLodingCanvas();
            }

            // GPS 측정 주기: `intervalInSecond`초
            yield return new WaitForSeconds(intervalInSecond);
        }
    }

    /// <summary>
    /// 일정 시간마다 서버에 사용자의 GPS정보를 HTTP request로 보내서 현재 위치 주변에 있는 Plane List를 받아 온다.
    /// 그 리스트를 이용해 <see cref="_arObjects"/>를 업데이트한다.
    /// </summary>
    private IEnumerator GetArObjectList(float intervalInSecond = 5.0f)
    {
        // GPS 초기화가 될 때까지 대기
        if (Application.platform == RuntimePlatform.Android)
            if (!_clientInfo.OriginalValuesAreSet)
                yield return new WaitUntil(() => _clientInfo.OriginalValuesAreSet);

        while (true)
        {
            
            if (_clientInfo.InsideOption) {
                _clientInfo.OriginalValuesAreSet = false;
                foreach (var arObject in _arObjects.Values)
                    arObject.Destroy();
                _arObjects.Clear();
            }
            else
            {

                string latitude = _clientInfo.CurrentLatitude.ToString();
                string longitude = _clientInfo.CurrentLongitude.ToString();
                string altitude = _clientInfo.CurrentAltitude.ToString();
                string latitudeOption;
                string longitudeOption;
                if (_clientInfo.DistanceOption == 1)
                {
                    latitudeOption = "0.0002";
                    longitudeOption = "0.0001";
                }
                else if (_clientInfo.DistanceOption == 2)
                {
                    latitudeOption = "0.0004";
                    longitudeOption = "0.0002";
                }
                else
                {
                    latitudeOption = "0.0006";
                    longitudeOption = "0.0003";
                }


                //테스트용 GPS 
                //latitude = "37.450700";
                //longitude = "126.657100";
                //altitude = "53.000000";
                //_clientInfo.StartingLatitude = 37.450700f;
                //_clientInfo.StartingLongitude = 126.657100f;
                //_clientInfo.StartingAltitude = 53.000000f;
                //_clientInfo.CurrentLatitude = 37.450700f;
                //_clientInfo.CurrentLongitude = 126.657100f;
                //_clientInfo.CurrentAltitude = 53.000000f;

                WWWForm form = new WWWForm();
                form.AddField("latitude", latitude);
                form.AddField("longitude", longitude);
                form.AddField("altitude", altitude);
                form.AddField("latitudeOption", latitudeOption);
                form.AddField("longitudeOption", longitudeOption);

                // 2d 오브젝트 목록 가져오기: GPS 정보를 서버에 POST
                using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/getGPS_distance.php", form))
                {
                    // POST 전송
                    yield return www.SendWebRequest();

                    if (www.isNetworkError || www.isHttpError)
                    {
                        Debug.Log(www.error);
                        // TODO: 필요시 재시도
                    }
                    else
                    {
                        // 서버에서 Json 응답으로 준 오브젝트 리스트를 _arObjects에 적용
                        string responseJsonString = www.downloadHandler.text;
                        JsonPlaneDataArray newObjectList = JsonUtility.FromJson<JsonPlaneDataArray>(responseJsonString);

                        if (newObjectList.data.Length == 0)
                        {
                            // 받아온 리스트에 아무것도 없는 경우 - 리스트 클리어
                            foreach (var arObject in _arObjects.Values)
                                arObject.Destroy();

                            _arObjects.Clear();
                        }
                        else
                        {
                            // 받아온 오브젝트의 Ad Number 모으기 (유일한 번호인 Ad Number로 오브젝트를 구별하기 위함)
                            var newAdNumbers = new HashSet<int>();
                            foreach (var newObject in newObjectList.data)
                                newAdNumbers.Add(newObject.ad_no);

                            // _arObjects의 ArObject들 중 받아온 리스트에 없는 것 삭제
                            var oldAdNumbers = new List<int>(_arObjects.Keys);
                            foreach (var oldNumber in oldAdNumbers)
                            {
                                if (!newAdNumbers.Contains(oldNumber))
                                {
                                    _arObjects[oldNumber].Destroy();
                                    _arObjects.Remove(oldNumber);
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
                                    AdNumber = jsonArObject.ad_no,
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
                                _arObjects[jsonArObject.ad_no] = new ArPlane(tmpAdInfo, _clientInfo);
                            }
                        }
                    }
                }
            }
            // 오브젝트 목록 리퀘스트 주기: `intervalInSecond`초.
            yield return new WaitForSeconds(intervalInSecond);
        }
    }

    private IEnumerator Get3dArObjectList(float intervalInSecond = 5.0f)
    {
        // GPS 초기화가 될 때까지 대기
        if (Application.platform == RuntimePlatform.Android)
            if (!_clientInfo.OriginalValuesAreSet)
                yield return new WaitUntil(() => _clientInfo.OriginalValuesAreSet);

        while (true)
        {

            if (_clientInfo.InsideOption)
            {
                _clientInfo.OriginalValuesAreSet = false;
                foreach (var arObject in _ar3dObjects.Values)
                    arObject.Destroy();
                _ar3dObjects.Clear();
            }
            else
            {

                string latitude = _clientInfo.CurrentLatitude.ToString();
                string longitude = _clientInfo.CurrentLongitude.ToString();
                string altitude = _clientInfo.CurrentAltitude.ToString();
                string latitudeOption;
                string longitudeOption;
                if (_clientInfo.DistanceOption == 1)
                {
                    latitudeOption = "0.0002";
                    longitudeOption = "0.0001";
                }
                else if (_clientInfo.DistanceOption == 2)
                {
                    latitudeOption = "0.0004";
                    longitudeOption = "0.0002";
                }
                else
                {
                    latitudeOption = "0.0006";
                    longitudeOption = "0.0003";
                }


                // 테스트용 GPS
                //latitude = "37.450571";
                //longitude = "126.656903";
                //altitude = "53.000000";

                WWWForm form = new WWWForm();
                form.AddField("latitude", latitude);
                form.AddField("longitude", longitude);
                form.AddField("altitude", altitude);
                form.AddField("latitudeOption", latitudeOption);
                form.AddField("longitudeOption", longitudeOption);

                // 3D 오브젝트 목록 가져오기: GPS 정보를 서버에 POST
                using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/get3D_distance.php", form))
                {
                    // POST 전송
                    yield return www.SendWebRequest();

                    if (www.isNetworkError || www.isHttpError)
                    {
                        Debug.Log(www.error);
                        // TODO: 필요시 재시도
                    }
                    else
                    {
                        // 서버에서 Json 응답으로 준 오브젝트 리스트를 _ar3dObjects에 적용
                        string responseJsonString = www.downloadHandler.text;
                        //JsonPlaneDataArray newObjectList = JsonUtility.FromJson<JsonPlaneDataArray>(responseJsonString);
                        Json3dDataArray newObjectList = JsonUtility.FromJson<Json3dDataArray>(responseJsonString);

                        if (newObjectList.data.Length == 0)
                        {
                            // 받아온 리스트에 아무것도 없는 경우 - 리스트 클리어
                            foreach (var arObject in _ar3dObjects.Values)
                                arObject.Destroy();

                            _ar3dObjects.Clear();
                        }
                        else
                        {
                            // 받아온 오브젝트의 Ad Number 모으기 (유일한 번호인 Ad Number로 오브젝트를 구별하기 위함)
                            var newAdNumbers = new HashSet<int>();
                            foreach (var newObject in newObjectList.data)
                                newAdNumbers.Add(newObject.object_no);

                            // _arObjects의 ArObject들 중 받아온 리스트에 없는 것 삭제
                            var oldAdNumbers = new List<int>(_ar3dObjects.Keys);
                            foreach (var oldNumber in oldAdNumbers)
                            {
                                if (!newAdNumbers.Contains(oldNumber))
                                {
                                    _ar3dObjects[oldNumber].Destroy();
                                    _ar3dObjects.Remove(oldNumber);
                                }
                            }

                            // 받아온 리스트에서 새로 생긴 ArObject 생성
                            foreach (Json3dData json3dArObject in newObjectList.data)
                            {
                                // 기존 리스트에 이미 있는 경우 안 만듦
                                if (_ar3dObjects.Keys.Contains(json3dArObject.object_no))
                                    continue;

                                // 새로운 ArObject 생성
                                Ad3dInfo tmpAdInfo = new Ad3dInfo
                                {
                                    ObjectNumber = json3dArObject.object_no,
                                    typeName = json3dArObject.typeName,
                                    GpsInfo = new Vector3(json3dArObject.latitude, json3dArObject.longitude,
                                    json3dArObject.altitude),
                                    Bearing = json3dArObject.bearing,
                                    TextAlternateToTexture = "",
                                };
                                _ar3dObjects[json3dArObject.object_no] = new Ar3dPlane(tmpAdInfo, _clientInfo);
                            }
                        }
                    }
                }
            }
            // 오브젝트 목록 리퀘스트 주기: `intervalInSecond`초.
            yield return new WaitForSeconds(intervalInSecond);
        }
    }

    /// <summary>
    /// 나침반 값과 카메라 각의 차를 모은다.
    /// </summary>
    /// <param name="intervalInSecond">초 단위 수집 간격</param>
    private IEnumerator CollectBearingDifference(float intervalInSecond = 2.0f)
    {
        while (true)
        {
            var gyroAngles = _clientInfo.MainCamera.transform.eulerAngles;
            // 자이로 센서를 바탕으로 기기가 아래(75~90도)나 위(270~285도)를 보고 있을 때는 카운트 하지 않음
            if ((0f <= gyroAngles.x && gyroAngles.x < 75f) || (285f < gyroAngles.x && gyroAngles.x <= 360f))
            {
                var difference = Input.compass.trueHeading - gyroAngles.y;

                // 버퍼에 각 차이 저장
                _clientInfo.BearingDifferenceBuffer[_clientInfo.BearingDifferenceIndex] = difference;
                _clientInfo.BearingDifferenceIndex++;
                if (_clientInfo.BearingDifferenceIndex == Constants.BearingDifferenceBufferSize)
                {
                    _clientInfo.BearingDifferenceBufferFilled = true;
                    _clientInfo.BearingDifferenceIndex = 0;
                }

                // 나침반 측정 주기: `intervalInSecond`초
            }
            yield return new WaitForSeconds(intervalInSecond);
        }
    }

    /// <summary>
    /// 주기적으로 _clientInfo.BearingDifferences의 평균값을
    /// _clientInfo.CorrectedBearingOffset에 저장하고, 이를 이용해 ArObject들을 올바른 위치에 재배치한다.
    /// </summary>
    /// <param name="intervalInSecond"></param>
    /// <returns></returns>
    private IEnumerator UpdateBearingOffset(float intervalInSecond = 2.0f)
    {
        while (true)
        {
            // (나침반 - 메인카메라 각) 값 평균 계산. CorrectedBearingOffset +/-180 안의 값으로 나온다.
            float averageOfDifferences = 0.0f;
            int bufferCount;
            if (_clientInfo.BearingDifferenceBufferFilled)
                bufferCount = Constants.BearingDifferenceBufferSize;
            else
                bufferCount = _clientInfo.BearingDifferenceIndex;

            for (int i = 0; i < bufferCount; i++)
            {
                var difference = _clientInfo.BearingDifferenceBuffer[i];

                // CorrectedBearingOffset +/-180 값 안으로 들어오도록 조정
                if (difference > _clientInfo.CorrectedBearingOffset + 180)
                    difference -= 360;
                else if (difference < _clientInfo.CorrectedBearingOffset - 180)
                    difference += 360;
                averageOfDifferences += difference;
            }
            averageOfDifferences /= bufferCount;
            averageOfDifferences %= 360f;


            //foreach (var arObject in _arObjects.Values)
            //{
            //    // 모든 물체를 생성시 카메라 포지션 기준 회전
            //    arObject.GameObj.transform.RotateAround(arObject.GameObj.GetComponent<DataContainer>().CreatedCameraPosition
            //        , new Vector3(0.0f, 1.0f, 0.0f), averageOfDifferences - _clientInfo.CorrectedBearingOffset);

            //    if ((arObject.ObjectType == ArObjectType.AdPlane) && (((ArPlane)arObject).CommentCanvas != null))
            //    {
            //        ((ArPlane)arObject).CommentCanvas.GameObj.transform.RotateAround(arObject.GameObj.GetComponent<DataContainer>().CreatedCameraPosition
            //        , new Vector3(0.0f, 1.0f, 0.0f), averageOfDifferences - _clientInfo.CorrectedBearingOffset);
            //    }
            //}
            // Bearing Offset 값을 새로 계산된 값으로 반영
            _clientInfo.CorrectedBearingOffset = averageOfDifferences;

            yield return new WaitForSeconds(intervalInSecond);
        }
    }

    private IEnumerator EarnPointCoroutine(int adNumber)
    {
        string userId = _userInfo.UserId;
        WWWForm checkLogForm = new WWWForm();
        checkLogForm.AddField("Input_user", userId);
        checkLogForm.AddField("Input_ad", adNumber);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/check_log.php", checkLogForm))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.Log(www.error);
            else
            {
                var fromServJson = www.downloadHandler.text;
                var pointInfo = JsonUtility.FromJson<JsonPointData>(fromServJson);

                if (pointInfo.clickLogFlag)
                {
                    WWWForm adInfoForm = new WWWForm();
                    adInfoForm.AddField("Input_ad", adNumber);

                    using (UnityWebRequest www2 = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/adinfo.php", adInfoForm))
                    {
                        yield return www2.SendWebRequest();

                        if (www2.isNetworkError || www2.isHttpError)
                            Debug.Log(www2.error);
                        //showToastOnUiThread(www2.error);
                        else
                        {
                            fromServJson = www2.downloadHandler.text;
                            pointInfo = JsonUtility.FromJson<JsonPointData>(fromServJson);

                            int pointToEarn = pointInfo.pointReward;
                            WWWForm pointForm = new WWWForm();
                            pointForm.AddField("Input_point", pointToEarn);
                            pointForm.AddField("Input_user", userId);
                            pointForm.AddField("Input_ad", adNumber);

                            using (UnityWebRequest www3 = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/earn_point.php", pointForm))
                            {
                                yield return www3.SendWebRequest();

                                if (www3.isNetworkError || www3.isHttpError)
                                    Debug.Log(www3.error);
                                else
                                {
                                    ShowToastOnUiThread("earn point: " + pointToEarn);
                                }
                            }
                        }
                    }

                    StartCoroutine(GetPointCoroutine());

                }
                else ShowToastOnUiThread("You already clicked!");
            }
        }
    }


    /// <summary>
    /// 옵션 버튼에 바인드. 옵션 버튼을 눌렀을 때 옵션 scene으로 이동한다.
    /// </summary>
    public void ToOptionScene()
    {
        SceneManager.LoadScene("Option");
    }

    void ShowToastOnUiThread(string toastString)
    {
        Debug.Log("Android Toast message: " + toastString);
        if (Application.platform != RuntimePlatform.Android)
            return;

        AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

        _currentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        this._toastString = toastString;

        _currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(ShowToast));
    }

    void ShowToast()
    {
        Debug.Log("Running on UI thread");
        AndroidJavaObject context = _currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
        AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", _toastString);
        AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_SHORT"));
        toast.Call("show");
    }

    private IEnumerator GetPointCoroutine()
    {
        string userID = _userInfo.UserId;

        string fromServJson;
        WWWForm checkPointForm = new WWWForm();
        checkPointForm.AddField("Input_user", userID);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/show_point.php", checkPointForm))
        {
            yield return www.SendWebRequest();

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

    public void onClickGift2Btn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("gift_2", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void onClickGift3Btn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("gift_3", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void onClickGift4Btn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("gift_4", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void onClickButterflyBtn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("Butterfly", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void onClickTreeBtn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("tree", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void onClickGorillaBtn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("gorilla", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void onClickLightBtn()
    {
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        //Vector3 unityPosition = GpsCalulator.CoordinateDifference(_clientInfo.StartingLatitude, _clientInfo.StartingLongitude, _clientInfo.StartingAltitude, 37.31263f, 126.8481f, 0);
        createObject("light", unityPosition);
        //createObject("horse", 40, -1, 0);
    }

    public void createObject(string typeName, Vector3 unityPosition)
    {
        string x = _clientInfo.CurrentLatitude.ToString();
        string y = _clientInfo.CurrentLongitude.ToString();
        string z = _clientInfo.CurrentAltitude.ToString();
        string bearing = _clientInfo.CurrentBearing.ToString();
        string point = _userInfo.Point.ToString();
        StartCoroutine(ObjectCreateCoroutine(x, y, z, typeName, _userInfo.UserId, bearing, point));
    }

    private IEnumerator ObjectCreateCoroutine(string x, string y, string z, string typeName, string id, string bearing, string point)
    {
        WWWForm form = new WWWForm();
        form.AddField("latitude", x);
        form.AddField("longitude", y);
        form.AddField("altitude", z);
        form.AddField("typeName", typeName);
        form.AddField("user", id);
        form.AddField("bearing", bearing);
        form.AddField("point", point);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/add_3d_Object.php", form))
        {
            yield return www.SendWebRequest();

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

    public void HideCommnetView()
    {
        commentViewCanvas.SetActive(false);
        inAppCanvas.SetActive(true);
    }
    public void Object3DMenuShowAndHide()
    {
        if (object3DMenu.activeSelf)
            object3DMenu.SetActive(false);
        else
            object3DMenu.SetActive(true);
    }
    public void TestButton()
    {
        //_clientInfo.LodingCanvas.GetComponent<LoadingCanvasBehaviour>().ShowLodingCanvas();
    }
}