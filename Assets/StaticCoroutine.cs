using UnityEngine;
using System.Collections;

public class StaticCoroutine : MonoBehaviour
{
    private static StaticCoroutine mInstance = null;
    private static StaticCoroutine instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = GameObject.FindObjectOfType(typeof(StaticCoroutine)) as StaticCoroutine;

                if (mInstance == null)
                {
                    mInstance = new GameObject("StaticCoroutine").AddComponent<StaticCoroutine>();
                }
            }
            return mInstance;
        }
    }

    void Awake()
    {
        if (mInstance == null)
        {
            mInstance = this as StaticCoroutine;
        }
    }

    IEnumerator Perform(IEnumerator coroutine)
    {
        yield return StartCoroutine(coroutine);
        //Die();
    }

    public static void DoCoroutine(IEnumerator coroutine)
    {
        //actually this point will be start coroutine
        instance.StartCoroutine(instance.Perform(coroutine));
    }

    void Die()
    {
        mInstance = null;
        Destroy(gameObject);
    }

    void OnApplicationQuit()
    {
        mInstance = null;
    }
}