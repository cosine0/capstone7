using UnityEngine;

/// <summary>
/// 사용자 ID, 이름, 세션ID, 포인트 값을 갖는 DontDestroyOnLoad오브젝트.
/// "UserInfo" 태그로 가져올 수 있음.
/// </summary>
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