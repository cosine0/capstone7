using UnityEngine;

public class LoadingCanvasBehaviour : MonoBehaviour {
    public bool activeFlag = false;

	public void ShowLodingCanvas()
    {
        activeFlag = true;
        gameObject.SetActive(true);
    }

    public void HideLodingCanvas()
    {
        activeFlag = false;
        gameObject.SetActive(false);
    }
}
