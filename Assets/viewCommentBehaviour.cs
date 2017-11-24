using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class viewCommentBehaviour : MonoBehaviour
{
    public string opjName;
    public string path;
    public GameObject ScrollViewGameObject;
    
    

    // Use this for initialization
    void Start()
    {
        
        //Cards is an array of data
        opjName = "optionComment";
        path = "Prefabs/" + opjName;
        
        for (int i = 0; i < 3; i++)
        {
            //ItemGameObject is my prefab pointer that i previous made a public property  
            //and  assigned a prefab to it
            GameObject card = Instantiate(Resources.Load(path)) as GameObject;

            //scroll = GameObject.Find("CardScroll");
            if (ScrollViewGameObject != null)
            {
                //ScrollViewGameObject container object
                card.transform.SetParent(ScrollViewGameObject.transform, false);
            }
        }



    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ToOptionScene()
    {
        SceneManager.LoadScene("Option");
    }



}
