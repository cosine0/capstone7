using UnityEngine;
using UnityEngine.UI;

public class Distance_Radio : MonoBehaviour {

    [Header("Toggle Object")]
    public Toggle meter_10;
    public Toggle meter_20;
    public Toggle meter_30;

	// Use this for initialization
	void Start () {
        meter_10.onValueChanged.AddListener(meter_10_changed);
        meter_20.onValueChanged.AddListener(meter_20_changed);
        meter_30.onValueChanged.AddListener(meter_30_changed);
    }
	
	void meter_10_changed(bool value) {
        if (meter_10.isOn)
        {
            meter_20.isOn = false;
            meter_30.isOn = false;
        }
    }

    void meter_20_changed(bool value)
    {
        if (meter_20.isOn)
        {
            meter_10.isOn = false;
            meter_30.isOn = false;
        }
    }

    void meter_30_changed(bool value)
    {
        if (meter_30.isOn)
        {
            meter_10.isOn = false;
            meter_20.isOn = false;
        }
    }
}
