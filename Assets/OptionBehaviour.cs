using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionBehaviour : MonoBehaviour {
    private ClientInfo _clientInfo;
    private UserInfo _userInfo;

	// Use this for initialization
	void Start () {
        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();
        _userInfo = GameObject.FindGameObjectWithTag("UserInfo").GetComponent<UserInfo>();

        GameObject.FindGameObjectWithTag("OptionUserId").GetComponent<Text>().text = "    " + _userInfo.UserName;

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ToInAppScene()
    {
        SceneManager.LoadScene("InApp");
    }
}
