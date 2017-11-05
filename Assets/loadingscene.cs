using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class Loadingscene : MonoBehaviour
{
    public Slider Slider;

    private bool _isDone = false;
    private float _fTime = 0f;
    private AsyncOperation _asyncOperation;


    private void Start()
    {
        StartCoroutine(StartLoad("capstone7"));
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
