using UnityEngine;

/// <summary>
/// 클라이언트 위치, 카메라 오브젝트, 옵션 값을 갖는 DontDestroyOnLoad오브젝트.
/// "ClientInfo" 태그로 가져올 수 있음.
/// </summary>
public class ClientInfo : MonoBehaviour
{
    public bool OriginalValuesAreSet = false;
    public bool BearingDifferenceBufferFilled = false;

    public float CorrectedBearingOffset = 0.0f;
    public float StartingLatitude = 0.0f;
    public float StartingLongitude = 0.0f;
    public float StartingAltitude = 0.0f;

    public float CurrentBearing
    {
        get
        {
            return MainCamera.transform.eulerAngles.y + CorrectedBearingOffset;
        }
    }
    public float CurrentLatitude = 0.0f;
    public float CurrentLongitude = 0.0f;
    public float CurrentAltitude = 0.0f;
    public float LastGpsMeasureTime = 0.0f;

    public GameObject MainCamera = null;
    public GameObject LodingCanvas = null;

    public bool InsideOption = false;
    public bool CommentViewOption = true;
    public bool Object3dViewOption = true;
    public int DistanceOption = 1;
    public string VersionInfo = "0.1";

    public float[] BearingDifferenceBuffer = new float[Constants.BearingDifferenceBufferSize];
    public int BearingDifferenceIndex = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(LodingCanvas);
        LodingCanvas.SetActive(false);
    }
}