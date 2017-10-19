using UnityEngine;

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