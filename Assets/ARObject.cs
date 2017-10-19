using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

public class AdInfo
{
    public string Name = "";
    public Vector3 GpsInfo;
    public float Bearing = 0.0f;
    public string BannerUrl = "";
    public string Sub = "";
    public Texture Tex = null;
};

public class CommentInfo
{
    public string Id = "";
    public DateTime DateTime;
    public string Comment = "";
}

// abstract ARObject
public abstract class ArObject
{
    public enum ArObjectType : int { ArObjectError = 0, AdPlane, ArComment };

    public GameObject GameObj;

    public UserInfo UserInfoObj;

    public ArObjectType ObjectType;

    abstract public void Create();
    abstract public void Update();
    abstract public void Destroy();// delete가 없음 null로 수정해서 참조 횟수를 줄임
};

public class ArPlane : ArObject
{
    public AdInfo Info;

    public ArPlane(AdInfo info, UserInfo info2)
    {
        Info = info;
        UserInfoObj = info2;
        Create();
    }

    private IEnumerator GetWebTex()
    {
        UnityWebRequest textureWebRequest = UnityWebRequestTexture.GetTexture(Info.BannerUrl);
        Debug.Log(Info.Name + " Request to server!");
        yield return textureWebRequest.Send();

        Debug.Log(Info.Name + " Create Texture!");
        Texture tmpTexture = DownloadHandlerTexture.GetContent(textureWebRequest);
        Debug.Log(Info.Name + "GetWeb " + tmpTexture.GetInstanceID());

        GameObj.GetComponent<MeshRenderer>().material.mainTexture = tmpTexture;
    }

    private IEnumerator CreateObject()
    {
        ObjectType = ArObjectType.AdPlane;
        GameObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameObj.name = Info.Name;


        yield return new WaitUntil(() => (UserInfoObj.OriginalValuesSet == true)); // 매번 확인하지 않도록 초기에 한번만 확인하도록 보완이 필요

        // 초기 포지션 설정
        Debug.Log("plane gps info : " + Info.GpsInfo[0] + " " + Info.GpsInfo[1] + " " + Info.GpsInfo[2]);
        Vector3 tmp = GpsCalulator.CoordinateDifference(UserInfoObj.CurrentLatitude, UserInfoObj.CurrentLongitude, UserInfoObj.CurrentAltitude, Info.GpsInfo[0], Info.GpsInfo[1], Info.GpsInfo[2]);
        //tmp.y = UserInfoObj.currentAltitude - Info.GPSInfo[2];
        tmp.y = UserInfoObj.CurrentAltitude - UserInfoObj.CurrentAltitude;

        GameObj.transform.position = tmp + UserInfoObj.MainCamera.transform.position;
        //GameOBJ.transform.position = new Vector3(0.0f, 0.0f, 30.0f);
        GameObj.transform.eulerAngles = new Vector3(90.0f, -90.0f, 90.0f); // gimbal lock이 발생하는 것 같음 90 0 -180으로 됨
        //GameOBJ.transform.rotation = Quaternion.Euler(90.0f, -90.0f, 90.0f);
        // 모든 plane은 new Vector3(90.0f, -90.0f, 90.0f); 만큼 회전해야함 
    }

    public sealed override void Create()
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
        MonoBehaviour.Destroy(GameObj); // object 제거, Null ptr 설정
        GameObj = null;
        Info = null;
    }
}

public class ArComment : ArObject
{
    public CommentInfo Comment { get; set; }

    public ArComment(CommentInfo info)
    {
        Comment = info;
    }

    public override void Create()
    {
        // Mesh Type Definition
        ObjectType = ArObjectType.ArComment;
    }

    public override void Update()
    {
        // Billboard? - calculate camera's inverse matrix
        throw new NotImplementedException();
    }

    public override void Destroy()
    {
        MonoBehaviour.Destroy(GameObj); // object 제거, Null ptr 설정
        GameObj = null;
        Comment = null;
    }
}