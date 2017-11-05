using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;


[System.Serializable]
public class JsonLoginData
{
    public string user_id;
    public string user_name;
    public string sessionID;
}

public class Login : MonoBehaviour
{
    public string Session;

    [Header("LoginPanel")]
    public InputField IdInputField;
    public InputField PwInputField;
    [Header("CreateAccountPanel")]
    public InputField NewIdInputField;
    public InputField NewPwInputField;
    public InputField NameInputField;
    public GameObject CreateAccountPanelObj;

    /// <summary>
    /// LoginButton의 OnClink에 바인드. 클릭 시 로그인 코루틴을 시작한다.
    /// </summary>
    public void OnClickLogin()
    {
        StartCoroutine(LoginCoroutine());
        //SceneManager.LoadScene("loading");
        //SceneManager.LoadScene("loadscene");
    }

    /// <summary>
    /// 서버에 로그인 정보를 전송하고 로그인 성공 시 <see cref="Session"/> 멤버에 정보를 저장하는 코루틴.
    /// </summary>
    private IEnumerator LoginCoroutine()
    {
        string userId = IdInputField.text;
        string password = PwInputField.text;

        WWWForm loginForm = new WWWForm();
        loginForm.AddField("Input_user", userId);
        loginForm.AddField("Input_pass", password);

        // 로그인 정보를 서버에 POST
        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/login_session.php", loginForm))
        {
            // POST 전송
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                // JSON 형태의 응답 파싱
                string jsonLoginResult = www.downloadHandler.text;

                JsonLoginData loginInfo = JsonUtility.FromJson<JsonLoginData>(jsonLoginResult);
                Session = loginInfo.sessionID;

                // 서버로부터 현재 로그인된 user_id랑 user_name를 받아옴.
                if (loginInfo.user_id == "")
                {
                    Debug.Log("failed login");
                }
                else
                {
                    Debug.Log("successed login");
                    SceneManager.LoadScene("loadscene");
                }

            }
        }
    }

    public void OpenSignUp()
    {
        //StartCoroutine(SignUpCoroutine());
        CreateAccountPanelObj.SetActive(true);

        //SceneManager.LoadScene("loading");
        //SceneManager.LoadScene("loadscene");
    }

    /// <summary>
    /// SignUpButton의 OnClink에 바인드. 클릭 시 회원 가입 코루틴을 시작한다.
    /// </summary>
    public void OnClickSignUp()
    {
        StartCoroutine(SignUpCoroutine());
        //CreateAccountPanelObj.SetActive(true);

        //SceneManager.LoadScene("loading");
        //SceneManager.LoadScene("loadscene");
    }

    /// <summary>
    /// 서버에 회원 가입 정보를 전송하는 코루틴.
    /// </summary>
    private IEnumerator SignUpCoroutine()
    {
        WWWForm signUpForm = new WWWForm();
        signUpForm.AddField("Input_user", NewIdInputField.text);
        signUpForm.AddField("Input_pass", NewPwInputField.text);
        signUpForm.AddField("Input_name", NameInputField.text);

        // 로그인 정보를 서버에 POST
        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/createaccount.php", signUpForm))
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
                Debug.Log(www.error);
            else
                CreateAccountPanelObj.SetActive(false);
        }
    }
}
