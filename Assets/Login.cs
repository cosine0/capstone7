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



public class Login : MonoBehaviour {

    public GameObject session_object;

    //public GameObject idObject;
    //public string session_;
    //public GameObject infotext2;
    [Header("LoginPanel")]
    public InputField IdInputField;
    public InputField PwInputField;
    [Header("CreateAccountPanel")]
    public InputField New_IdInputField;
    public InputField New_PwInputField;
    public InputField NameInputField;
    public GameObject CreateAccountPanelObj;

    

    public void LoginCheck()
    {
        StartCoroutine(LoginCo());
        //SceneManager.LoadScene("loading");
        //SceneManager.LoadScene("loadscene");
    }

    //IEnumerator LoginCo() {

    //    string a = IdInputField.text;
    //    string b = PwInputField.text;

    //    WWWForm form = new WWWForm();
    //    form.AddField("Input_user", a);
    //    form.AddField("Input_pass", b);

    //    using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/login.php", form))
    //    {
    //        yield return www.Send();

    //        if (www.isNetworkError || www.isHttpError)
    //        {
    //            Debug.Log(www.error);
    //        }
    //        else
    //        {
    //            Debug.Log(www.downloadHandler.text);
    //            //SceneManager.LoadScene("loadscene");

    //        }
    //    }



    //}

    IEnumerator LoginCo()
    {

        string a = IdInputField.text;
        string b = PwInputField.text;

        WWWForm form = new WWWForm();
        form.AddField("Input_user", a);
        form.AddField("Input_pass", b);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/login_session.php", form))
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

                //fromServJson = "{\"user_id\":\"a\"}";

                JsonLoginData DataList = JsonUtility.FromJson<JsonLoginData>(fromServJson);

                Debug.Log(DataList.sessionID);

                session_object = GameObject.FindGameObjectWithTag("session_gameobject");

                DontDestroyOnLoad(session_object);

                session_object.GetComponent<Text>().text = DataList.sessionID;
                

                //session_object.GetComponent<Text>().text = DataList.sessionID;

                //Debug.Log(session_object.GetComponent<Text>().text);

                //infotext2.GetComponent<Text>().text = "Welcome,\n" + "!";

                // 서버로부터 현재 로그인 된 user_id랑 user_name 받아옴.

                if (DataList.user_id == "") Debug.Log("failed login");
                else {
                    Debug.Log("successed login");
                    SceneManager.LoadScene("loadscene");
                }

            }
        }


    }

    public void openSignUp()
    {
        //StartCoroutine(SignUpCo());
        CreateAccountPanelObj.SetActive(true);

        //SceneManager.LoadScene("loading");
        //SceneManager.LoadScene("loadscene");
    }

    public void SignUp()
    {
        StartCoroutine(SignUpCo());
        //CreateAccountPanelObj.SetActive(true);

        //SceneManager.LoadScene("loading");
        //SceneManager.LoadScene("loadscene");
    }

    IEnumerator SignUpCo() {

        WWWForm form = new WWWForm();
        form.AddField("Input_user", New_IdInputField.text);
        form.AddField("Input_pass", New_PwInputField.text);
        form.AddField("Input_name", NameInputField.text);
        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/createaccount.php", form))
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                CreateAccountPanelObj.SetActive(false);

            }
        }
    }
    

    //public void Click() {
    //    SceneManager.LoadScene(1);
    //}
}
