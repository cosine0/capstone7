using UnityEngine;

public class LoadingCanvasBehaviour : MonoBehaviour {
    public bool IsActive = false;

	public void ShowLodingCanvas()
    {
        IsActive = true;
        gameObject.SetActive(true);
    }

    public void HideLodingCanvas()
    {
        IsActive = false;
        gameObject.SetActive(false);
    }
}
