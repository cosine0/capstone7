using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 scene의 실내외 스위치 스크립트.
/// 클릭 시 on/off로 변경되는 애니메이션 재생, ClientInfo.InsideOption 반전
/// </summary>
public class InsideOptionSwitch : MonoBehaviour {
    [Header("Toggle Object")]
    public Button Outside;
    public Button Inside;

    private ClientInfo _clientInfo;
    // Use this for initialization

    [Header("ANIMATORS")]
    public Animator OnAnimator;
    public Animator OffAnimator;

    [Header("ANIM NAMES")]
    public string OnTransition;
    public string OffTransition;

    void Start()
    {
        Outside.onClick.AddListener(OptionToggle);
        Inside.onClick.AddListener(OptionToggle);

        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();

        if (!_clientInfo.InsideOption)
        {
            OffAnimator.Play(OffTransition);
        }
        else
        {
            OnAnimator.Play(OnTransition);
        }

    }
	
	private void OptionToggle()
	{
        // 실내/외를 반전
	    _clientInfo.InsideOption = !_clientInfo.InsideOption;
        Debug.Log(_clientInfo.InsideOption);

	}
}