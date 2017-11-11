using UnityEngine;
using UnityEngine.UI;

public class InsideOptionSwitch : MonoBehaviour {
    [Header("Toggle Object")]
    public Button Outside;
    public Button Inside;

    private ClientInfo _clientInfo;
    // Use this for initialization

    [Header("ANIMATORS")]
    public Animator onAnimator;
    public Animator offAnimator;

    [Header("ANIM NAMES")]
    public string onTransition;
    public string offTransition;

    void Start()
    {
        Outside.onClick.AddListener(OptionToggle);
        Inside.onClick.AddListener(OptionToggle);

        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();

        if (!_clientInfo.InsideOption)
        {
            offAnimator.Play(offTransition);
        }
        else
        {
            onAnimator.Play(onTransition);
        }

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