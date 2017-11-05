using UnityEngine;

public class UserInfo : MonoBehaviour
{
    public string userid = "";
    public string name = "";
    public string session_id = "";
    public int point = -1;

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}