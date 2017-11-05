using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;

[System.Serializable]
public class JsonData
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
public class JsonDataArray
{
    public JsonData[] data;
}

/// <summary>
/// 앱 시작 시 실행되는 메인 Behavior.
/// </summary>
public class MainBehaviour : MonoBehaviour
{
    /// <summary>
    /// 텍스트 출력창 (디버깅용)</summary>
    public GameObject TextBox;

    /// <summary>
    /// 이 앱에 로드된 모든 AR 오브젝트의 목록</summary>
    private Dictionary<int, ArObject> _arObjects;

    /// <summary>
    /// 현재 사용자 정보</summary>
    private UserInfo _userInfo;

    private void Start()
    {
        // 사용자 정보 생성
        _userInfo = new UserInfo();
        _userInfo.MainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        // GPS 좌표 정보 갱신용 코루틴 시작
        StartCoroutine(GetGps());
        // AR 오브젝트 리스트 초기화
        _arObjects = new Dictionary<int, ArObject>();

        // 주변 오브젝트 목록 주기적 업데이트 코루틴 시작
        StartCoroutine(GetPlaneList());
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
                    Application.OpenURL(hitObject.collider.GetComponent<DataContainer>().banner_url);
                    break;

                case TouchPhase.Canceled:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //위치 정보 출력(디버그)
        TextBox.GetComponent<Text>().text =
            "Origin: " + _userInfo.StartingLatitude + ", " + _userInfo.StartingLongitude + ", " + _userInfo.StartingAltitude
            + "\nGPS: " + _userInfo.CurrentLatitude + ", " + _userInfo.CurrentLongitude + ", " + _userInfo.CurrentAltitude
            + "\ncamera position: " + _userInfo.MainCamera.transform.position
            + "\ncamera angle: " + _userInfo.MainCamera.transform.eulerAngles.x + ", " + (_userInfo.MainCamera.transform.eulerAngles.y + _userInfo.StartingBearing) + ", "
            + _userInfo.MainCamera.transform.eulerAngles.z
            + "\nObject Count: " + _arObjects.Count
            + "\nCamera to object: ";

        foreach (ArObject entity in _arObjects.Values)
        {
            Vector3 cameraToObject = entity.GameObj.transform.position - _userInfo.MainCamera.transform.position;
            TextBox.GetComponent<Text>().text += cameraToObject + ";";
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
        if (Mathf.Abs(newBearing - _userInfo.CurrentBearing) < 180)
        {
            if (Math.Abs(newBearing - _userInfo.CurrentBearing) > Constants.SmoothThresholdCompass)
            {
                _userInfo.CurrentBearing = newBearing;
            }
            else
            {
                _userInfo.CurrentBearing = _userInfo.CurrentBearing + Constants.SmoothFactorCompass * (newBearing - _userInfo.CurrentBearing);
            }
        }
        else
        {
            if (360.0 - Math.Abs(newBearing - _userInfo.CurrentBearing) > Constants.SmoothThresholdCompass)
            {
                _userInfo.CurrentBearing = newBearing;
            }
            else
            {
                if (_userInfo.CurrentBearing > newBearing)
                {
                    _userInfo.CurrentBearing = (_userInfo.CurrentBearing + Constants.SmoothFactorCompass * ((360 + newBearing - _userInfo.CurrentBearing) % 360) +
                                      360) % 360;
                }
                else
                {
                    _userInfo.CurrentBearing = (_userInfo.CurrentBearing - Constants.SmoothFactorCompass * ((360 - newBearing + _userInfo.CurrentBearing) % 360) +
                                      360) % 360;
                }
            }
        }
        Vector3 newCameraAngle = _userInfo.MainCamera.transform.eulerAngles;
        newCameraAngle.y = _userInfo.CurrentBearing;
        _userInfo.MainCamera.transform.eulerAngles = newCameraAngle;
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
            _userInfo.StartingLatitude, _userInfo.StartingLongitude, _userInfo.StartingAltitude,
            _userInfo.CurrentLatitude, _userInfo.CurrentLongitude, _userInfo.StartingAltitude);

        _userInfo.MainCamera.transform.position = coordinateDifferenceFromStart;
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

            _userInfo.LastGpsMeasureTime = Time.time;

            _userInfo.CurrentLatitude = Input.location.lastData.latitude;
            _userInfo.CurrentLongitude = Input.location.lastData.longitude;
            _userInfo.CurrentAltitude = Input.location.lastData.altitude;
            _userInfo.CurrentBearing = Input.compass.trueHeading;


            // 초기 위치 정보 저장
            if (!_userInfo.OriginalValuesAreSet)
            {
                _userInfo.StartingLatitude = _userInfo.CurrentLatitude;
                _userInfo.StartingLongitude = _userInfo.CurrentLongitude;
                _userInfo.StartingAltitude = _userInfo.CurrentAltitude;
                _userInfo.StartingBearing = _userInfo.CurrentBearing;

                _userInfo.OriginalValuesAreSet = true;
            }
            Input.location.Stop();
        }
    }

    /// <summary>
    /// 5초마다 서버에 사용자의 GPS정보로 HTTP request를 보내서 현재 위치 주변에 있는 Plane List를 받아온다.
    /// </summary>
    IEnumerator GetPlaneList()
    {
        if (!_userInfo.OriginalValuesAreSet)
            yield return new WaitUntil(() => _userInfo.OriginalValuesAreSet);

        while (true)
        {
            string latitude = _userInfo.CurrentLatitude.ToString();
            string longitude = _userInfo.CurrentLongitude.ToString();
            string altitude = _userInfo.CurrentAltitude.ToString();

            // 테스트용 GPS
            //latitude = "37.450571";
            //longitude = "126.656903";
            //altitude = "53.000000";

            WWWForm form = new WWWForm();
            form.AddField("latitude", latitude);
            form.AddField("longitude", longitude);
            form.AddField("altitude", altitude);

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

                    JsonDataArray newObjectList = JsonUtility.FromJson<JsonDataArray>(fromServJson);

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
                        foreach (JsonData jsonArObject in newObjectList.data)
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
                            _arObjects[jsonArObject.ad_no] = new ArPlane(tmpAdInfo, _userInfo);
                        }
                    }
                }
            }

            // 5초에 한번씩 실행
            yield return new WaitForSeconds(5.0f);
        }
    }
}