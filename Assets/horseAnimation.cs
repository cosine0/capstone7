using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class horseAnimation : MonoBehaviour
{
    //public GameObject obj;
    //public Animator horseAnim;
    
    // Use this for initialization
    void Start()
    {
        //horseAnim = gameObject.GetComponent<Animator>();
        
    }

    // Update is called once per frame
    void Update()
    {
        //if (!horseAnim.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.idle"))
        //{
        //    //Debug.Log("walk");
        //    //transform.position += new Vector3(Time.deltaTime, 0, 0);
        //    transform.Rotate(0, -0.5f, 0);
        //}
    }

    public void onClickBtn()
    {
        //createObject("horse", _clientInfo.CurrentLatitude, _clientInfo.CurrentLongitude, 0);
        createObject("momobject", 70, -1, 0);
    }


    public void createObject(string objName, float x, float y, float z)
    {
        //Instantiate(obj, new Vector3(40, -1, 0.0f), Quaternion.identity);
        Instantiate(Resources.Load("Prefabs/" + objName), new Vector3(x, y, z), Quaternion.identity);
    }


}
