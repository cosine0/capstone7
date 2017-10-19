using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

public class ADInfo
{
    public string name = "";
    public Vector3 GPSInfo;
    public float bearing = 0.0f;
    public string bannerUrl = "";
    public string sub = "";
    public Texture tex = null;
};

public class CommentInfo
{
    public string id = "";
    public DateTime dateTime;
    public string comment = "";
}

// abstract ARObject
abstract public class ARObject
{
    public enum ARObjectType : int { ARObjectError = 0, ADPlane, ARComment };

    public GameObject GameOBJ;

    public UserInfo userInfo;

    public ARObjectType ObjectType;

    abstract public void Create();
    abstract public void Update();
    abstract public void Destroy();// delete가 없음 null로 수정해서 참조 횟수를 줄임
};

public class ARPlane : ARObject
{
    public ADInfo AdInfo;

    public ARPlane(ADInfo info, UserInfo info2)
    {
        AdInfo = info;
        userInfo = info2;
        Create();
    }

    IEnumerator GetWebTex()
    {
        Texture tmpTexture;

        UnityWebRequest textureWebRequest = UnityWebRequestTexture.GetTexture(AdInfo.bannerUrl);
        Debug.Log(AdInfo.name + " Request to server!");
        yield return textureWebRequest.Send();

        Debug.Log(AdInfo.name + " Create Texture!");
        tmpTexture = DownloadHandlerTexture.GetContent(textureWebRequest);
        Debug.Log(AdInfo.name + "GetWeb " + tmpTexture.GetInstanceID());

        GameOBJ.GetComponent<MeshRenderer>().material.mainTexture = tmpTexture;
    }

    IEnumerator CreateObject()
    {
        ObjectType = ARObjectType.ADPlane;
        GameOBJ = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameOBJ.name = AdInfo.name;


        yield return new WaitUntil(() => (userInfo.setOriginalValues == false)); // 매번 확인하지 않도록 초기에 한번만 확인하도록 보완이 필요

        // 초기 포지션 설정
        Debug.Log("plane gps info : " + AdInfo.GPSInfo[0] + " " + AdInfo.GPSInfo[1] + " " + AdInfo.GPSInfo[2]);
        Vector3 tmp = GPSCalulator.CoordinateDifference(userInfo.currentLatitude, userInfo.currentLongitude, AdInfo.GPSInfo[0], AdInfo.GPSInfo[1]);
        //tmp.y = userInfo.currentAltitude - AdInfo.GPSInfo[2];
        tmp.y = userInfo.currentAltitude - userInfo.currentAltitude;

        GameOBJ.transform.position = tmp + userInfo.mainCamera.transform.position;
        //GameOBJ.transform.position = new Vector3(0.0f, 0.0f, 30.0f);
        GameOBJ.transform.eulerAngles = new Vector3(90.0f, -90.0f, 90.0f); // gimbal lock이 발생하는 것 같음 90 0 -180으로 됨
        //GameOBJ.transform.rotation = Quaternion.Euler(90.0f, -90.0f, 90.0f);
        // 모든 plane은 new Vector3(90.0f, -90.0f, 90.0f); 만큼 회전해야함 
    }

    public override void Create()
    {
        // 텍스쳐 생성
        // StaticCorutine은 처음 호출시 생성되며 수행 이후 파괴되지 않고 필요할때 마다 이용됨.
        StaticCoroutine.DoCoroutine(GetWebTex());
        StaticCoroutine.DoCoroutine(CreateObject());
    }

    public override void Update()
    {
        // position update or animation update
    }

    public override void Destroy()
    {
        MonoBehaviour.Destroy(GameOBJ); // object 제거, Null ptr 설정
        GameOBJ = null;
        AdInfo = null;
    }
}

public class ARComment : ARObject
{
    public CommentInfo Comment
    {
        get { return Comment; }
        set { Comment = value; }
    }

    public ARComment(CommentInfo info)
    {
        Comment = info;
    }

    public override void Create()
    {
        // Mesh Type Definition
        ObjectType = ARObjectType.ARComment;
    }

    public override void Update()
    {
        // Billboard? - calculate camera's inverse matrix
        throw new NotImplementedException();
    }

    public override void Destroy()
    {
        MonoBehaviour.Destroy(GameOBJ); // object 제거, Null ptr 설정
        GameOBJ = null;
        Comment = null;
    }
}