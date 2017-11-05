using UnityEngine;
using UnityEngine.SceneManagement;

public class Login : MonoBehaviour {

	public void ToInAppScene()
    {
        SceneManager.LoadScene("InApp");
    }
}
