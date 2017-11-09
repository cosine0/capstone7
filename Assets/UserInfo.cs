using UnityEngine;

public class UserInfo : MonoBehaviour
{
    public string UserId = "";
    public string UserName = "";
    public string SessionId = "";
    public int Point = -1;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}