using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class get_distance : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Hello");
        StartCoroutine(InsertMemberData());
    }

    IEnumerator InsertMemberData()
    {
        Debug.Log("Hi!");

        //insert data to table
        WWWForm form = new WWWForm();

        string currlati = "37.450670";
        string currlong = "126.656895";
        string curralti = "123.123";

        form.AddField("currlati", currlati);
        form.AddField("currlong", currlong);
        form.AddField("curralti", curralti);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/capstone/getGPS_distance.php", form))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                Debug.Log("Oh my god");
            }
            else
            {
                Debug.Log("Form upload complete!");
                Debug.Log(www.downloadHandler.text);

                string a = www.downloadHandler.text;
            }
        }


    }
}
