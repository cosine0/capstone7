using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class LoadingSceneManager : MonoBehaviour
{
    public static string NextScene;

    [SerializeField]
    private Image _progressBar;
    public GameObject Infotext;
    private void Start()
    {
        StartCoroutine(LoadScene());
    }

    private string _nextSceneName;
    public static void LoadScene(string sceneName)
    {
        NextScene = sceneName;
        SceneManager.LoadScene("loadscene");
    }

    private IEnumerator LoadScene()
    {
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync("capstone7");
        op.allowSceneActivation = false;

        GameObject sessionInfo = GameObject.FindGameObjectWithTag("MainCamera");
        string sessionInfoString = sessionInfo.GetComponent<Login>().Session;

        WWWForm form = new WWWForm();
        form.AddField("Input_Session_ID", sessionInfoString);

        Debug.Log("꺄"+sessionInfoString);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/login_info.php",form))
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

                JsonLoginData dataList = JsonUtility.FromJson<JsonLoginData>(fromServJson);
                
                Infotext.GetComponent<Text>().text = "Welcome,\n" + dataList.user_name + "!";
            }
        }


        float timer = 0.0f;
        while (!op.isDone)
        {
            yield return null;

            timer += Time.deltaTime;

            if (op.progress >= 0.9f)
            {

                _progressBar.fillAmount = Mathf.Lerp(_progressBar.fillAmount, 1f, timer);

                if (_progressBar.fillAmount == 1.0f)
                    op.allowSceneActivation = true;

                
            }
            else
            {
                _progressBar.fillAmount = Mathf.Lerp(_progressBar.fillAmount, op.progress, timer);
                if (_progressBar.fillAmount >= op.progress)
                {
                    timer = 0f;
                }
            }
        }
    }
}

