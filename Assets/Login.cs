using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary>
/// 로그인 성공 시 서버에서 Json 응답으로 주는 사용자 정보를 담기 위한 객체.
/// </summary>
[System.Serializable]
public class JsonLoginData
{
    public string user_id;
    public string user_name;
    public string sessionID;
    public int point;
}

/// <summary>
/// 로그인 scene에 필요한 스크립트를 갖는 Behaviour.
/// </summary>
public class Login : MonoBehaviour
{

    private UserInfo _userInfo;

    // 안드로이드 Toast를 띄울 때 사용되는 임시 객체
    private string _toastString;
    private AndroidJavaObject _currentActivity;

    //public GameObject idObject;
    //public string session_;
    //public GameObject infoText;
    [Header("LoginPanel")]
    public InputField IdInputField;
    public InputField PwInputField;
    [Header("CreateAccountPanel")]
    public InputField NewIdInputField;
    public InputField NewPwInputField;
    public InputField NameInputField;
    public GameObject CreateAccountPanelObj;
    public GameObject _clientInfo;


    /// <summary>
    /// LoginButton의 OnClink에 바인드. 클릭 시 로그인 코루틴을 시작한다.
    /// </summary>
    public void OnClickLogin()
    {
        StartCoroutine(LoginCoroutine());
    }

    /// <summary>
    /// 서버에 로그인 정보를 전송하고 로그인 성공 시 <see cref="_userInfo"/> 멤버에 정보를 저장하는 코루틴.
    /// </summary>
    private IEnumerator LoginCoroutine()
    {
        string userId = IdInputField.text;
        string password = PwInputField.text;

        WWWForm loginForm = new WWWForm();
        loginForm.AddField("Input_user", userId);
        loginForm.AddField("Input_pass", password);

        _clientInfo.GetComponent<ClientInfo>().LodingCanvas.GetComponent<LoadingCanvasBehaviour>().ShowLodingCanvas();
        // 로그인 정보를 서버에 POST
        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/login_session.php", loginForm))
        {
            // POST 전송
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                ShowToastOnUiThread("Failed to sign in. Cannot connect to the server.");
                _clientInfo.GetComponent<ClientInfo>().LodingCanvas.GetComponent<LoadingCanvasBehaviour>().HideLodingCanvas();
                Debug.Log(www.error);
            }
            else
            {
                // 서버에서 Json 응답으로 유저 정보를 UserInfo 오브젝트에 적용
                string responseJsonString = www.downloadHandler.text;

                JsonLoginData loginInfo = JsonUtility.FromJson<JsonLoginData>(responseJsonString);
                _userInfo = GameObject.FindGameObjectWithTag("UserInfo").GetComponent<UserInfo>();

                _userInfo.SessionId = loginInfo.sessionID;
                _userInfo.UserName = loginInfo.user_name;
                _userInfo.UserId = loginInfo.user_id;
                _userInfo.Point = loginInfo.point;

                // 서버로부터 현재 로그인된 user_id와 user_name를 받아온다
                if (loginInfo.user_id == "")
                {
                    ShowToastOnUiThread("ID or Password is incorrect.");
                    _clientInfo.GetComponent<ClientInfo>().LodingCanvas.GetComponent<LoadingCanvasBehaviour>().HideLodingCanvas();
                }
                else
                {
                    Debug.Log("succeeded to sign in");
                    PwInputField.text = "";
                    _clientInfo.GetComponent<ClientInfo>().OriginalValuesAreSet = false;
                    SceneManager.LoadScene("InApp");
                }

            }
        }
    }

    /// <summary>
    /// 로그인 창의 Sign Up 버튼에 바인드. 클릭 시 회원 가입 창을 표시한다.
    /// </summary>
    public void OpenSignUp()
    {
        // 회원 가입 창 표시
        CreateAccountPanelObj.SetActive(true);
    }
    public void CloseSignUp()
    {
        CreateAccountPanelObj.SetActive(false);
    }

    /// <summary>
    /// 회원 가입 창의 Sign Up 버튼에 바인드. 클릭 시 회원 가입 정보를 서버에 요청하는 코루틴을 시작한다.
    /// </summary>
    public void OnClickSignUp()
    {
        if (NameInputField.text == "") ShowToastOnUiThread("Input Name");
        else if (NewIdInputField.text == "") ShowToastOnUiThread("Input ID");
        else if (NewPwInputField.text == "") ShowToastOnUiThread("Input Password");
        else StartCoroutine(SignUpCoroutine());
    }

    /// <summary>
    /// 서버에 회원 가입 정보를 전송하는 코루틴.
    /// </summary>
    private IEnumerator SignUpCoroutine()
    {
        WWWForm signUpForm = new WWWForm();
        signUpForm.AddField("Input_name", NameInputField.text);
        signUpForm.AddField("Input_user", NewIdInputField.text);
        signUpForm.AddField("Input_pass", NewPwInputField.text);

        // 회원가입 정보를 서버에 POST
        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/createaccount.php", signUpForm))
        {
            // POST 전송
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                ShowToastOnUiThread("Failed to sign up. Cannot connect to the server.");
                Debug.Log(www.error);
            }
            else
            {
                ShowToastOnUiThread("Sign up succeeded.");
                NameInputField.text = "";
                NewIdInputField.text = "";
                NewPwInputField.text = "";
            }
            // 회원가입 창 감추기
            CreateAccountPanelObj.SetActive(false);
        }
    }

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
        AndroidJavaClass toast_class = new AndroidJavaClass("android.widget.Toast");
        AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", _toastString);
        AndroidJavaObject toast = toast_class.CallStatic<AndroidJavaObject>("makeText", context, javaString, toast_class.GetStatic<int>("LENGTH_SHORT"));
        toast.Call("show");
    }

    public void OnClickPreviousButton() {
        CreateAccountPanelObj.SetActive(false);
    }
}