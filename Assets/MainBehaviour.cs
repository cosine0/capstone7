﻿using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;

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

    private IEnumerator CreateTestPlanes()
    {
        // 약 1미터
        const float gpsInterval = 0.00001f;
        const float gpsPrecision = 100000f;
        if (!_userInfo.OriginalValuesSet)
            yield return new WaitUntil(() => _userInfo.OriginalValuesSet);

        var roundedLat = Mathf.Round(_userInfo.CurrentLatitude * gpsPrecision) / gpsPrecision;
        var roundedLon = Mathf.Round(_userInfo.CurrentLongitude * gpsPrecision) / gpsPrecision;
        var roundedAlt = Mathf.Round(_userInfo.CurrentAltitude * gpsPrecision) / gpsPrecision;

        for (var latDiff = -25; latDiff < 25; latDiff += 5)
        {
            for (var lonDiff = -25; lonDiff < 25; lonDiff += 5)
            {
                for (var altDiff = -10; altDiff < 10; altDiff += 5)
                {
                    Debug.Log("In loop " + latDiff + " " + lonDiff + " " + altDiff);
                    var eulerAngle = new Vector3(0, 0, 0);
                    if (latDiff < 0)
                    {
                        eulerAngle.y = 0f;
                    }
                    else if (latDiff == 0)
                    {
                        if (lonDiff < 0)
                            eulerAngle.y = -90f;
                        else
                            eulerAngle.y = 90f;
                    }
                    else
                    {
                        eulerAngle.y = 180f;
                    }
                    var plainInfo = new AdInfo
                    {
                        Name = "plain_" + latDiff + "_" + lonDiff + "_" + altDiff,
                        EulerAngle = eulerAngle,
                        GpsInfo = new Vector3(
                            roundedLat + latDiff * gpsInterval,
                            roundedLon + lonDiff * gpsInterval,
                            roundedAlt + altDiff * gpsInterval),
                        TextureUrl = "https://lh4.googleusercontent.com/-v0soe-ievYE/AAAAAAAAAAI/AAAAAAADwkE/KyrKDjjeV1o/photo.jpg"
                    };
                    _arObjectList.Add(new ArPlane(plainInfo, _userInfo));
                }
            }
        }
    }

    private void Update()
    {
        UpdateCameraBearing();
        UpdateCameraPosition();

        // 위치 정보 출력 (디버그)
        TextBox.GetComponent<Text>().text =
            "Origin: " + _userInfo.StartingLatitude + ", " + _userInfo.StartingLongitude + ", " + _userInfo.StartingAltitude
            + "\nGPS: " + _userInfo.CurrentLatitude + ", " + _userInfo.CurrentLongitude + ", " + _userInfo.CurrentAltitude
            + "\nplane position: " + _arObjectList[0].GameObj.transform.position
            + "\ncamera position: " + _userInfo.MainCamera.transform.position
            + "\ncamera angle: " + _userInfo.MainCamera.transform.eulerAngles;

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
}