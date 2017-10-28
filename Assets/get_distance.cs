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

        string latitude = "37.450670";
        string longitude = "126.656895";
        string altitude = "123.123";

        form.AddField("latitude", latitude);
        form.AddField("longitude", longitude);
        form.AddField("altitude", altitude);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/main/getGPS_distance.php", form))
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
