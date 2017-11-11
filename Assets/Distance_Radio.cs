using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 옵션 scene의 거리 설정 라디오 버튼 스크립트.
/// 한 버튼 클릭시 다른 버튼이 클릭 해제되도록 함.
/// </summary>
public class Distance_Radio : MonoBehaviour {

    [Header("Toggle Object")]
    public Toggle Meter10;
    public Toggle Meter20;
    public Toggle Meter30;

    private ClientInfo _clientInfo;

	void Start () {
        Meter10.onValueChanged.AddListener(Meter10Changed);
        Meter20.onValueChanged.AddListener(Meter20Changed);
        Meter30.onValueChanged.AddListener(Meter30Changed);

        _clientInfo = GameObject.FindGameObjectWithTag("ClientInfo").GetComponent<ClientInfo>();
    }

    private void Meter10Changed(bool value) {
        if (Meter10.isOn)
        {
            _clientInfo.DistanceOption = 1;
            Meter20.isOn = false;
            Meter30.isOn = false;
        }
    }

    private void Meter20Changed(bool value)
    {
        if (Meter20.isOn)
        {
            _clientInfo.DistanceOption = 2;
            Meter10.isOn = false;
            Meter30.isOn = false;
        }
    }

    private void Meter30Changed(bool value)
    {
        if (Meter30.isOn)
        {
            _clientInfo.DistanceOption = 3;
            Meter10.isOn = false;
            Meter20.isOn = false;
        }
    }
}
