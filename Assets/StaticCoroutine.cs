using UnityEngine;
using System.Collections;

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
                }
            }
            return _instance;
        }
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

    public static void DoCoroutine(IEnumerator coroutine)
    {
        //actually this point will be start coroutine
        Instance.StartCoroutine(Instance.Perform(coroutine));
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