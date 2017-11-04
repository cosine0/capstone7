using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class LoadingSceneManager : MonoBehaviour
{
    public static string nextScene;

    [SerializeField]
    Image progressBar;
    public GameObject infotext;
    private void Start()
    {
        StartCoroutine(LoadScene());
    }

    string nextSceneName;
    public static void LoadScene(string sceneName)
    {
        nextScene = sceneName;
        SceneManager.LoadScene("loadscene");
    }

    IEnumerator LoadScene()
    {
        yield return null;

        AsyncOperation op = SceneManager.LoadSceneAsync("capstone7");
        op.allowSceneActivation = false;


        using (UnityWebRequest www = UnityWebRequest.Get("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/login_info.php"))
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


                infotext.GetComponent<Text>().text = "Welcome,\n" + DataList.user_name + "!";

            }
        }


        float timer = 0.0f;
        while (!op.isDone)
        {
            yield return null;

            timer += Time.deltaTime;

            if (op.progress >= 0.9f)
            {

                progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, 1f, timer);

                if (progressBar.fillAmount == 1.0f)
                    op.allowSceneActivation = true;

                
            }
            else
            {
                progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, op.progress, timer);
                if (progressBar.fillAmount >= op.progress)
                {
                    timer = 0f;
                }
            }
        }
    }
}

