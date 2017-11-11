using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class horseAnimation : MonoBehaviour
{

    //public Animator horseAnim;

    public GameObject obj;

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
        //    Debug.Log("walk");
        //    //transform.position += new Vector3(Time.deltaTime, 0, 0);
        //    //transform.Rotate(0, -0.5f, 0);
        //}

    }

    public void createObject() {
        Instantiate(obj, new Vector3(15, -0.5835799f, 0.0f), Quaternion.identity);
    }

   
}
