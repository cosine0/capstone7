using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionBehaviour : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ToInAppScene()
    {
        SceneManager.LoadScene("InApp");
    }
}
