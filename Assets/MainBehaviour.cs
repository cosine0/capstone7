using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

[System.Serializable]
public class JsonData
{
    public string name;
    public float latitude;
    public float longitude;
    public float altitude;
    public float bearing;
    public string banner_url;
}

[System.Serializable]
public class JsonDataArray
{
    public JsonData[] data;
}

public class MainBehaviour : MonoBehaviour
{
    /// <summary>
    /// 텍스트 출력창 (디버깅용)</summary>
    public GameObject TextBox;
    /// <summary>
    /// 이 앱에 로드된 모든 AR 오브젝트의 목록</summary>
    private List<ArObject> _arObjectList;
    /// <summary>
    /// 현재 사용자 정보</summary>
    private UserInfo _userInfo;

    private void Start()
    {
        //GameObject sessionInfo = GameObject.FindGameObjectWithTag("Loadingscenemanager");
        //Debug.Log("꺄" + sessionInfo.GetComponent<Text>().text);

        // 사용자 정보 생성
        _userInfo = new UserInfo();
        _userInfo.MainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        // GPS 코루틴 시작
        StartCoroutine(GetGps());

        // AR 오브젝트 리스트 생성
        _arObjectList = new List<ArObject>();

        // 테스트용 플레인 생성
        //StartCoroutine(CreateTestPlanes());
        StartCoroutine(GetPlaneList());
    }

    private IEnumerator CreateTestPlanes()
    {
        if (!_userInfo.OriginalValuesSet)
            yield return new WaitUntil(() => _userInfo.OriginalValuesSet);
    }

    private void Update()
    {
        UpdateCameraBearing();
        UpdateCameraPosition();

        //위치 정보 출력(디버그)
        TextBox.GetComponent<Text>().text =
            "Origin: " + _userInfo.StartingLatitude + ", " + _userInfo.StartingLongitude + ", " + _userInfo.StartingAltitude
            + "\nGPS: " + _userInfo.CurrentLatitude + ", " + _userInfo.CurrentLongitude + ", " + _userInfo.CurrentAltitude
            + "\ncamera position: " + _userInfo.MainCamera.transform.position
            + "\ncamera angle: " + _userInfo.MainCamera.transform.eulerAngles;

        //+ "\nplane position: " + _arObjectList[0].GameObj.transform.position.ToString()
        //// ARObject Update (animation)
        //foreach(ARObject entity in ARObjectList) {
        //    entity.Update();
        //}
    }

    private void UpdateCameraBearing()
    {
        // 방위각
        //          0.0:
        //          북
        // 270.0:서    동:90.0
        //          남
        //          :180.0
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

    private IEnumerator GetGps()
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
                Debug.Log("Timed out");
                yield break;
            }

            // Connection has failed
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

            //StaticCoroutine.DoCoroutine(GetPlaneList());
            // 아래 두개는 사용자 선택에 따라 렌더링 가능하도록 설정
            //StaticCoroutine.DoCoroutine(GetCommentList());
            //StaticCoroutine.DoCoroutine(GetUserObjectList());

            if (!_userInfo.OriginalValuesSet)
            {
                _userInfo.StartingLatitude = _userInfo.CurrentLatitude;
                _userInfo.StartingLongitude = _userInfo.CurrentLongitude;
                _userInfo.StartingAltitude = _userInfo.CurrentAltitude;
                _userInfo.StartingBearing = _userInfo.CurrentBearing;

                _userInfo.OriginalValuesSet = true;
            }
            Input.location.Stop();
        }
    }

    IEnumerator GetPlaneList()
    {
        if (!_userInfo.OriginalValuesSet)
            yield return new WaitUntil(() => _userInfo.OriginalValuesSet);

        while (true)
        {
            // 5초에 한번씩 실행
            yield return new WaitForSeconds(5.0f);

            string latitude = _userInfo.CurrentLatitude.ToString();
            string longitude = _userInfo.CurrentLongitude.ToString();
            string altitude = _userInfo.CurrentAltitude.ToString();

            //gps testset
            //string latitude = "37.450666";
            //string longitude = "126.656844";
            //string altitude = "0.000000";

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
                }
                else
                {
                    Debug.Log(www.downloadHandler.text);

                    // Json 데이터에서 값을 파싱하여 리스트 형태로 재구성
                    string fromServJson = www.downloadHandler.text;

                    JsonDataArray DataList = JsonUtility.FromJson<JsonDataArray>(fromServJson);

                    // Object List 정리
                    for (int i = 0; i < _arObjectList.Count; i++)
                    {
                        if (_arObjectList[i].ObjectType == ArObject.ArObjectType.AdPlane)
                        {
                            bool flag = false;

                            foreach (JsonData j_entity in DataList.data)
                            {
                                if (flag = _arObjectList[i].GameObj.name.Equals(j_entity.name))
                                {
                                    break; // 새로 받아온 리스트에 존재 할 경우 넘어감.
                                }
                            }

                            if (flag == false)
                            {
                                _arObjectList[i].Destroy(); // 새로 받아온 리스트를 조사하여 없는 경우 파괴
                                _arObjectList.RemoveAt(i); // 리스트에서 제거
                            }
                        }
                    }

                    // Object List에 추가
                    foreach (JsonData j_entity in DataList.data)
                    {
                        bool flag = true;

                        //기존 리스트에 이미 있는 경우 안만듦
                        foreach (ArObject entity in _arObjectList)
                        {
                            if (flag = j_entity.name.Equals(entity.GameObj.name))
                            {
                                flag = false;
                                break;
                            }
                        }

                        // 기존 리스트에 없는 경우 새로 생성
                        if (flag == true)
                        {
                            AdInfo tmpAdInfo = new AdInfo
                            {
                                Name = j_entity.name,
                                GpsInfo = new Vector3(j_entity.latitude, j_entity.longitude, j_entity.altitude),
                                Bearing = j_entity.bearing,
                                TextureUrl = "https://lh4.googleusercontent.com/-v0soe-ievYE/AAAAAAAAAAI/AAAAAAADwkE/KyrKDjjeV1o/photo.jpg",
                                TextureAlternateText = "",
                                AdTexture = null
                            };

                            _arObjectList.Add(new ArPlane(tmpAdInfo, _userInfo));
                        }
                    }
                }
            }
            Debug.Log("Object Count: " + _arObjectList.Count.ToString());
        }
    }
}