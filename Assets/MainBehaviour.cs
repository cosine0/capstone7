using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

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
        // 사용자 정보 생성
        _userInfo = new UserInfo();
        _userInfo.MainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        // GPS 코루틴 시작
        StartCoroutine(GetGps());

        // AR 오브젝트 리스트 생성
        _arObjectList = new List<ArObject>();

        // 테스트용 플레인 생성
        StartCoroutine(CreateTestPlanes());

        StartCoroutine(InsertMemberData());
    }

    private IEnumerator CreateTestPlanes()
    {
        const float gpsInterval = 0.00001f;
        const float gpsPrecision = 100000f;
        if (!_userInfo.OriginalValuesSet)
            yield return new WaitUntil(() => _userInfo.OriginalValuesSet);

        AdInfo tmpAdInfo = new AdInfo
        {
            Name = "Google",
            GpsInfo = new Vector3(_userInfo.CurrentLatitude + gpsInterval, _userInfo.CurrentLongitude, _userInfo.CurrentAltitude),
            Bearing = 0.0f,
            TextureUrl = "https://lh4.googleusercontent.com/-v0soe-ievYE/AAAAAAAAAAI/AAAAAAADwkE/KyrKDjjeV1o/photo.jpg",
            TextureAlternateText = "",
            AdTexture = null
        };
        _arObjectList.Add(new ArPlane(tmpAdInfo, _userInfo));
        //        var roundedLat = Mathf.Round(_userInfo.CurrentLatitude * gpsPrecision) / gpsPrecision;
        //        var roundedLon = Mathf.Round(_userInfo.CurrentLongitude * gpsPrecision) / gpsPrecision;
        //        var roundedAlt = Mathf.Round(_userInfo.CurrentAltitude * gpsPrecision) / gpsPrecision;

        //        for (var latDiff = -5; latDiff < 5; latDiff++)
        //        {
        //            for (var lonDiff = -5; lonDiff < 5; lonDiff++)
        //            {
        //                for (var altDiff = -2; altDiff < 2; altDiff++)
        //                {
        //                    Debug.Log("In loop " + latDiff + " " + lonDiff + " " + altDiff);
        //                    var plainInfo = new AdInfo
        //                    {
        //                        Name = "plain",
        //                        Bearing = 0.0f,
        //                        GpsInfo = new Vector3(
        //                            roundedLat + latDiff * gpsInterval,
        //                            roundedLon + lonDiff * gpsInterval,
        //                            roundedAlt + altDiff * gpsInterval),
        //                        TextureUrl = "https://lh4.googleusercontent.com/-v0soe-ievYE/AAAAAAAAAAI/AAAAAAADwkE/KyrKDjjeV1o/photo.jpg"
        //                    };
        //                    _arObjectList.Add(new ArPlane(plainInfo, _userInfo));
        //                }
        //            }
        //        }
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

    IEnumerator InsertMemberData()
    {
        Debug.Log("Hi!");

        ////insert data to table
        //WWWForm form = new WWWForm();

        //string currlati = "37.450670";
        //string currlong = "126.656895";
        //string curralti = "123.123";

        //form.AddField("currlati", currlati);
        //form.AddField("currlong", currlong);
        //form.AddField("curralti", curralti);

        //using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/getGPS_distance.php", form))
        //{
        //    www.SetRequestHeader("Content-Type", "application/json");
        //    yield return www.Send();

        //    if (www.isNetworkError || www.isHttpError)
        //    {
        //        Debug.Log(www.error);
        //        Debug.Log("Oh my god");
        //    }
        //    else
        //    {
        //        Debug.Log("Form upload complete!");
        //        Debug.Log(www.downloadHandler.text);

        //        string a = www.downloadHandler.text;
        //    }
        //}


    }


}