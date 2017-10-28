using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class get_distance : MonoBehaviour {

    IEnumerator InsertMemberData()
    {
        Debug.Log("Hi!");

        //insert data to table
        WWWForm form = new WWWForm();
        form.AddField("latitude", latitude);
        form.AddField("longitude", longitude);
        form.AddField("altitude", altitude);

        using (UnityWebRequest www = UnityWebRequest.Post("http://ec2-13-125-7-2.ap-northeast-2.compute.amazonaws.com:31337/main/getGPS_distance.php", form))
        {
            yield return www.Send();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
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
