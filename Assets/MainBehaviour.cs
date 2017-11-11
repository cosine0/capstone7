﻿using UnityEngine;
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
public class JsonPlaneDataArray
{
    public JsonPlaneData[] data;
}

/// <summary>
/// 앱 시작 시 실행되는 메인 Behaviour.
/// </summary>
public class MainBehaviour : MonoBehaviour
{
    /// <summary>
    /// 텍스트 출력창 (디버깅용)
    /// </summary>
    public GameObject TextBox;

    /// <summary>
    /// 이 앱에 로드된 모든 AR 오브젝트의 목록. Ad Number를 키, ArObject를 값으로 가진다.
    /// </summary>
    private Dictionary<int, ArObject> _arObjects;

    /// <summary>
    /// 현재 사용자 정보
    /// </summary>
    private ClientInfo _clientInfo;
    private UserInfo _userInfo;

    private void Start()
    {
        // 글로벌 DontDestroyOnLoad 객체 사용자 정보 가져오기
        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();
        // scene에 있는 AR 카메라 가져오기
        _clientInfo.MainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        // GPS 좌표 정보 갱신용 코루틴 시작
        StartCoroutine(GetGps());

        // AR 오브젝트 리스트 초기화
        _arObjects = new Dictionary<int, ArObject>();
        // 주변 오브젝트 목록 주기적 업데이트를 위한 코루틴 시작
        StartCoroutine(UpdateArObjectList(5.0f));
    }

    private void Update()
    {
        //UpdateCameraBearing();
        UpdateCameraPosition();

        if (Input.touchCount > 0)
        {
            // 화면을 터치했을 때
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
                    // 터치를 뗀 경우 - 터치한 위치의 광선에 닿는 물체의 BannerUrl을 브라우저에서 연다.
                    RaycastHit hitObject;
                    Physics.Raycast(ray, out hitObject, Mathf.Infinity);
                    Application.OpenURL(hitObject.collider.GetComponent<DataContainer>().BannerUrl);
                    break;

                case TouchPhase.Canceled:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // 위치 정보 출력 (디버그)
        TextBox.GetComponent<Text>().text =
            "Origin: " + _clientInfo.StartingLatitude + ", " + _clientInfo.StartingLongitude + ", " + _clientInfo.StartingAltitude
            + "\nGPS: " + _clientInfo.CurrentLatitude + ", " + _clientInfo.CurrentLongitude + ", " + _clientInfo.CurrentAltitude
            + "\nBearing: " + _clientInfo.CurrentBearing
            + "\ncamera position: " + _clientInfo.MainCamera.transform.position
            + "\ncamera angle: " + _clientInfo.MainCamera.transform.eulerAngles.x + ", " + (_clientInfo.MainCamera.transform.eulerAngles.y + _clientInfo.StartingBearing) + ", "
            + _clientInfo.MainCamera.transform.eulerAngles.z
            + "\nObject Count: " + _arObjects.Count
            + "\nCamera to object: ";

        // 물체 위치 출력 (디버그)
        foreach (ArObject entity in _arObjects.Values)
        {
            Vector3 cameraToObject = entity.GameObj.transform.position - _clientInfo.MainCamera.transform.position;
            TextBox.GetComponent<Text>().text += cameraToObject + "\n";
        }
    }

    /// <summary>
    /// 나침반 센서 값을 카메라 방위각에 적용한다.
    /// </summary>
    private void UpdateCameraBearing()
    {
        // Input.compass.trueHeading의 값과 방향 매칭:
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

        // GPS 고도 무시
        coordinateDifferenceFromStart.y = 0.0f;
        //coordinateDifferenceFromStart.x *= 2;
        //coordinateDifferenceFromStart.z *= 2;

        // 카메라를 유니티 상의 현재 사용자 위치로 옮기기
        _clientInfo.MainCamera.transform.position = coordinateDifferenceFromStart;
    }

    /// <summary>
    /// 코루틴 주기마다 <see cref="_clientInfo"/>의 GPS 값을 업데이트한다.
    /// </summary>
    private IEnumerator GetGps()
    {
        // 앱이 켜져 있는 동안 계속 실행.
        while (true)
        {
            // 위치 서비스가 켜져 있는지 체크
            if (!Input.location.isEnabledByUser)
                yield break;

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

            // 글로벌 DontDestroyOnLoad 오브젝트인 _clientInfo의 현재 위치 업데이트
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

            // 측정 주기: 0.3초
            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// 일정 시간마다 서버에 사용자의 GPS정보를 HTTP request로 보내서 현재 위치 주변에 있는 Plane List를 받아 온다.
    /// 그 리스트를 이용해 <see cref="_arObjects"/>를 업데이트한다.
    /// </summary>
    private IEnumerator UpdateArObjectList(float intervalInSecond = 5.0f)
    {
        // GPS 초기화가 될 때까지 대기
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

            // GPS 정보를 서버에 POST
            using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/getGPS_distance.php", form))
            {
                // POST 전송
                yield return www.Send();

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

            // `intervalInSecond`초마다 한번씩 실행
            yield return new WaitForSeconds(intervalInSecond);
        }
    }

    /// <summary>
    /// 옵션 버튼에 바인드. 옵션 버튼을 눌렀을 때 옵션 scene으로 이동한다.
    /// </summary>
    public void ToOptionScene()
    {
        SceneManager.LoadScene("Option");
    }
}