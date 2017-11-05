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
    public GameObject infotext2;
    //Text text;

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

        AsyncOperation op = SceneManager.LoadSceneAsync("InApp");
        op.allowSceneActivation = false;

        infotext = GameObject.FindGameObjectWithTag("session_gameobject");

        Debug.Log(infotext.GetComponent<Text>().text);

        //text.text = infotext.GetComponent<Text>().text;

        ////GameObject sessionInfo = GameObject.FindGameObjectWithTag("MainCamera");
        ////GameObject session_info = sessionInfo.GetComponent<Login>().idObject;

        string tt = infotext.GetComponent<Text>().text;

        WWWForm form2 = new WWWForm();
        form2.AddField("id", tt);



        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/login_info.php", form2))
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



                infotext2.GetComponent<Text>().text = "Welcome,\n" + DataList.user_name + "!";
                Debug.Log(infotext2.GetComponent<Text>().text);


                //infotext.GetComponent<Text>().text = "Welcome,\n" + DataList.user_name + "!";

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

