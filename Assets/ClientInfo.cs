using UnityEngine;

public class ClientInfo
{
    public bool OriginalValuesAreSet = false;

    public float StartingBearing = 0.0f;
    public float StartingLatitude = 0.0f;
    public float StartingLongitude = 0.0f;
    public float StartingAltitude = 0.0f;

    public float CurrentBearing = 0.0f;
    public float CurrentLatitude = 0.0f;
    public float CurrentLongitude = 0.0f;
    public float CurrentAltitude = 0.0f;
    public float LastGpsMeasureTime = 0.0f;

    public GameObject MainCamera = null;
}