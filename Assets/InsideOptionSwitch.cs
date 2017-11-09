using UnityEngine;
using UnityEngine.UI;

public class InsideOptionSwitch : MonoBehaviour {
    [Header("Toggle Object")]
    public Button Outside;
    public Button Inside;

    private ClientInfo _clientInfo;
    // Use this for initialization
    void Start () {
        Outside.onClick.AddListener(OptionToggle);
        Inside.onClick.AddListener(OptionToggle);

        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();
    }
	
	private void OptionToggle()
    {
        if (_clientInfo.InsideOption)
        {
            _clientInfo.InsideOption = false;
        }
        else
        {
            _clientInfo.InsideOption = true;
        }
    }
}