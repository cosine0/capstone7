using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 옵션 설정 창 내부에서 사용하는 코드를 갖는 Behaviour.
/// </summary>
public class OptionBehaviour : MonoBehaviour
{
    /// <summary>
    /// 클라이언트 정보를 갖는 DontDestroyOnLoad 객체에 대한 참조
    /// </summary>
    private ClientInfo _clientInfo;

    /// <summary>
    /// 사용자 정보를 갖는 DontDestroyOnLoad 객체에 대한 참조
    /// </summary>
    private UserInfo _userInfo;

    private JsonPointData _pointData;

    public GameObject viewPanel;
    void Start()
    {
        // DontDestroyOnLoad 객체 가져오기
        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();
        _userInfo = GameObject.FindGameObjectWithTag("UserInfo").GetComponent<UserInfo>();

        StartCoroutine(GetPointCoroutine());

        // 사용자 아이디 필드에 값 표시
        GameObject.FindGameObjectWithTag("OptionUserId").GetComponent<Text>().text = "    " + _userInfo.UserId;
        // 사용자 포인트 필드에 값 표시
        GameObject.FindGameObjectWithTag("OptionUserPoint").GetComponent<Text>().text = "    " + _userInfo.Point;

        // 현재 거리 옵션을 UI의 버튼에 적용
        switch (_clientInfo.DistanceOption)
        {
            case 1:
                GameObject.FindGameObjectWithTag("OptionRadioButton").GetComponent<Distance_Radio>().Meter10.isOn = true;
                break;
            case 2:
                GameObject.FindGameObjectWithTag("OptionRadioButton").GetComponent<Distance_Radio>().Meter20.isOn = true;
                break;
            case 3:
                GameObject.FindGameObjectWithTag("OptionRadioButton").GetComponent<Distance_Radio>().Meter30.isOn = true;
                break;
            default:
                Debug.Log("Distance Option Value Error");
                break;
        }

        // 버전 정보 필드에 값 표시
        GameObject.FindGameObjectWithTag("OptionVersionInfo").GetComponent<Text>().text = _clientInfo.VersionInfo;
    }

    void Update()
    {

    }

    /// <summary>
    ///  뒤로 가기 버튼에 바인드. 인앱 scene으로 돌아간다.
    /// </summary>
    public void ToInAppScene()
    {
        SceneManager.LoadScene("InApp");
    }

    public void OnClickLogout()
    {
        StartCoroutine(Logout());
    }

    private IEnumerator Logout()
    {
        // 서버에 로그아웃 리퀘스트
        using (UnityWebRequest www = UnityWebRequest.Get("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/logout_session.php"))
        {
            // Get 전송
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                ShowToastOnUiThread("Failed to log out. Cannot connect to the server.");
                Debug.Log(www.error);
            }
            else
            {
                ShowToastOnUiThread("Logout succeeded.");
                _clientInfo.OriginalValuesAreSet = false;
                SceneManager.LoadScene("login");
            }
        }
    }

    // 안드로이드 Toast를 띄울 때 사용되는 임시 객체
    private string _toastString;
    private AndroidJavaObject _currentActivity;

    /// <summary>
    /// 안드로이드 토스트를 띄운다.
    /// </summary>
    /// <param name="toastString">토스트에 표시할 문자열</param>
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
        AndroidJavaObject context = _currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
        AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", _toastString);
        AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>("makeText", context, javaString, toastClass.GetStatic<int>("LENGTH_SHORT"));
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
                GameObject.FindGameObjectWithTag("OptionUserPoint").GetComponent<Text>().text = "    " + _userInfo.Point;
            }
        }
    }
    
    public void clickViewBtn()
    {
        SceneManager.LoadScene("viewComment");
    }
}