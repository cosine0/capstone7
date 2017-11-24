using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

/// <summary>
/// loadscene scene 구동 스트립트. 로딩 scene 시작 시 비동기로 InApp scene을 로드하고 진행 사항을 프로그레스 바로 표시한다.
/// 완료되면 InApp scene으로 전환한다.
/// </summary>
public class LoadingSceneManager : MonoBehaviour
{
    public static string NextScene;
    
    public Image ProgressBar;
    public GameObject WelcomeText;

    private string _nextSceneName;

    private void Start()
    {
        StartCoroutine(LoadScene("InApp"));
    }

    IEnumerator LoadScene(string sceneName)
    {
        yield return null;

        // scene 로드 작업을 시작한다.
        AsyncOperation loadSceneOperation = SceneManager.LoadSceneAsync(sceneName);
        loadSceneOperation.allowSceneActivation = false;

        var uerInfo = GameObject.FindGameObjectWithTag("UserInfo").GetComponent<UserInfo>();

        // 로딩 화면에 유저 아이디를 표시한다.
        WWWForm loginInfoForm = new WWWForm();
        loginInfoForm.AddField("id", uerInfo.SessionId);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/login_info.php", loginInfoForm))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);

                string responseJsonString = www.downloadHandler.text;
                JsonLoginData dataList = JsonUtility.FromJson<JsonLoginData>(responseJsonString);

                WelcomeText.GetComponent<Text>().text = "Welcome,\n" + dataList.user_name + "!";
                Debug.Log(WelcomeText.GetComponent<Text>().text);
            }
        }
        
        // 이미지를 사용해 프로그레스 바 표시
        float timer = 0.0f;
        while (!loadSceneOperation.isDone)
        {
            yield return null;

            timer += Time.deltaTime;

            if (loadSceneOperation.progress >= 0.9f)
            {
                ProgressBar.fillAmount = Mathf.Lerp(ProgressBar.fillAmount, 1f, timer);
                    
                if (ProgressBar.fillAmount == 1.0f)
                    // 완료됐으면 scene 표시.
                    loadSceneOperation.allowSceneActivation = true;
            }
            else
            {
                ProgressBar.fillAmount = Mathf.Lerp(ProgressBar.fillAmount, loadSceneOperation.progress, timer);
                if (ProgressBar.fillAmount >= loadSceneOperation.progress)
                {
                    timer = 0f;
                }
            }
        }
    }
}

