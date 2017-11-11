using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// loading scene 구동 스트립트. 로딩 scene 시작 시 비동기로 InApp scene을 로드하고 진행 사항을 프로그레스 바로 표시한다.
/// 완료되면 InApp scene으로 전환한다.
/// </summary>
public class Loadingscene : MonoBehaviour
{
    public Slider Slider;

    private bool _isDone = false;
    private float _fTime = 0f;
    private AsyncOperation _asyncOperation;


    private void Start()
    {
        StartCoroutine(StartLoad("InApp"));
    }

    private void Update()
    {
        _fTime += Time.deltaTime;
        Slider.value = _fTime;

        if (_fTime >= 5)
        {
            _asyncOperation.allowSceneActivation = true;
        }
    }

    private IEnumerator StartLoad(string strSceneName)
    {
        _asyncOperation = Application.LoadLevelAsync(strSceneName);
        _asyncOperation.allowSceneActivation = false;

        if (_isDone == false)
        {
            _isDone = true;

            while (_asyncOperation.progress < 0.9f)
            {
                Slider.value = _asyncOperation.progress;

                yield return true;
            }
        }
    }
}
