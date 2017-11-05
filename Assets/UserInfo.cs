using UnityEngine;

public class UserInfo : MonoBehaviour
{
    public string user_id = "";
    public string user_name = "";
    public string session_id = "";
    public int point = -1;

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}