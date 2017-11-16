using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 scene의 실내외 스위치 스크립트.
/// 클릭 시 on/off로 변경되는 애니메이션 재생, ClientInfo.InsideOption 반전
/// </summary>
public class Object3dOptionSwitch : MonoBehaviour {
    [Header("Toggle Object")]
    public Button Off;
    public Button On;

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
        Off.onClick.AddListener(OptionToggle);
        On.onClick.AddListener(OptionToggle);

        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();

        if (!_clientInfo.Object3dViewOption)
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
	    _clientInfo.Object3dViewOption = !_clientInfo.Object3dViewOption;
	}
}