using UnityEngine;
using System.Collections;

/// <summary>
/// Behaviour 클래스 안에 있지 않은 함수에서 코루틴을 사용하기 위한 싱글톤 클래스.
/// </summary>
public class StaticCoroutine : MonoBehaviour
{
    private static StaticCoroutine _instance = null;
    private static StaticCoroutine Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType(typeof(StaticCoroutine)) as StaticCoroutine;

                if (_instance == null)
                {
                    _instance = new GameObject("StaticCoroutine").AddComponent<StaticCoroutine>();
                    DontDestroyOnLoad(_instance);  // scene이 전환되어도 이 객체는 파괴되지 않도록함.
                }
            }
            return _instance;
        }
    }

    public static void DoCoroutine(IEnumerator coroutine)
    {
        // 여기서 실제 코루틴이 시작됨
        Instance.StartCoroutine(Instance.Perform(coroutine));
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }

    private IEnumerator Perform(IEnumerator coroutine)
    {
        yield return StartCoroutine(coroutine);
        //Die();
    }

    private void Die()
    {
        _instance = null;
        Destroy(gameObject);
    }

    private void OnApplicationQuit()
    {
        _instance = null;
    }
}