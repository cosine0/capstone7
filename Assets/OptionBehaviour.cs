using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 옵션 설정 창 내부에서 사용하는 코드를 갖는 Behaviour.
/// </summary>
public class OptionBehaviour : MonoBehaviour {
    /// <summary>
    /// 클라이언트 정보를 갖는 글로벌 DontDestroyOnLoad 객체에 대한 참조
    /// </summary>
    private ClientInfo _clientInfo;

    /// <summary>
    /// 사용자 정보를 갖는 글로벌 DontDestroyOnLoad 객체에 대한 참조
    /// </summary>
    private UserInfo _userInfo;

	void Start () {
        // 글로벌 DontDestroyOnLoad 객체 가져오기
        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();
        _userInfo = GameObject.FindGameObjectWithTag("UserInfo").GetComponent<UserInfo>();

        // 사용자 아이디 필드에 값 표시
        GameObject.FindGameObjectWithTag("OptionUserId").GetComponent<Text>().text = "    " + _userInfo.UserId;
        // 사용자 포인트 필드에 값 표시
        GameObject.FindGameObjectWithTag("OptionUserPoint").GetComponent<Text>().text = "    " + _userInfo.Point;

        // 현재 거리 옵션을 UI의 버튼에 적용
        switch (_clientInfo.DistanceOption)
        {
            case 1:
                GameObject.FindGameObjectWithTag("OptionRadioButton").GetComponent<Distance_Radio>().Meter10.isOn = true;
                break;
            case 2:
                GameObject.FindGameObjectWithTag("OptionRadioButton").GetComponent<Distance_Radio>().Meter20.isOn = true;
                break;
            case 3:
                GameObject.FindGameObjectWithTag("OptionRadioButton").GetComponent<Distance_Radio>().Meter30.isOn = true;
                break;
            default:
                Debug.Log("Distance Option Value Error");
                break;
        }

        // 버전 정보 필드에 값 표시
        GameObject.FindGameObjectWithTag("OptionVersionInfo").GetComponent<Text>().text = _clientInfo.VersionInfo;
    }
	
	void Update ()
    {
		
	}

    /// <summary>
    ///  뒤로 가기 버튼에 바인드. 인앱 scene으로 돌아간다.
    /// </summary>
    public void ToInAppScene()
    {
        SceneManager.LoadScene("InApp");
    }
}