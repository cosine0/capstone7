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

        // User Id
        GameObject.FindGameObjectWithTag("OptionUserId").GetComponent<Text>().text = "    " + _userInfo.UserId;
        // User Point
        GameObject.FindGameObjectWithTag("OptionUserPoint").GetComponent<Text>().text = "    " + _userInfo.Point.ToString();

        // Inside option
        GameObject offSwitch = GameObject.FindGameObjectWithTag("OptionInsideOff");
        GameObject onSwitch = GameObject.FindGameObjectWithTag("OptionInsideOn");

        if (_clientInfo.InsideOption)
        {
            // inside
            offSwitch.GetComponent<CanvasGroup>().alpha = 0;
            offSwitch.GetComponent<CanvasGroup>().interactable = false;
            offSwitch.GetComponent<CanvasGroup>().blocksRaycasts = false;

            onSwitch.GetComponent<CanvasGroup>().alpha = 1;
            onSwitch.GetComponent<CanvasGroup>().interactable = true;
            onSwitch.GetComponent<CanvasGroup>().blocksRaycasts = true;
        }
        else
        {
            // outside
            offSwitch.GetComponent<CanvasGroup>().alpha = 1;
            offSwitch.GetComponent<CanvasGroup>().interactable = true;
            offSwitch.GetComponent<CanvasGroup>().blocksRaycasts = true;

            onSwitch.GetComponent<CanvasGroup>().alpha = 0;
            onSwitch.GetComponent<CanvasGroup>().interactable = false;
            onSwitch.GetComponent<CanvasGroup>().blocksRaycasts = false;
        }

        // Distance option
        switch (_clientInfo.DistanceOption)
        {
            case 1:
                GameObject.FindGameObjectWithTag("OptionRadioButton").GetComponent<Distance_Radio>().meter_10.isOn = true;
                break;
            case 2:
                GameObject.FindGameObjectWithTag("OptionRadioButton").GetComponent<Distance_Radio>().meter_20.isOn = true;
                break;
            case 3:
                GameObject.FindGameObjectWithTag("OptionRadioButton").GetComponent<Distance_Radio>().meter_30.isOn = true;
                break;
            default:
                Debug.Log("Distance Option Value Error");
                break;
        }

        // Version Info
        GameObject.FindGameObjectWithTag("OptionVersionInfo").GetComponent<Text>().text = _clientInfo.VersionInfo;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ToInAppScene()
    {
        SceneManager.LoadScene("InApp");
    }
}