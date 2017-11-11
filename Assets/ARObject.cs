using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

public enum ArObjectType : int { ArObjectError = 0, AdPlane, ArCommentCanvas, Ar3dObject, ArComment };

public class AdInfo
{
    public int Id;
    public string Name = "";
    public Vector3 GpsInfo;
    public float Bearing = 0.0f;
    public string TextureUrl = null;
    public string TextAlternateToTexture = "";
    public string BannerUrl = null;
    public Texture AdTexture = null;
    public float Width = 1.0f;
    public float Height = 1.0f;
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
    public int Id;

    public GameObject GameObj;

    public ClientInfo ClientInfoObj;

    public ArObjectType ObjectType;

    public abstract void Create();
    public abstract void Update();
    public abstract void Destroy();// delete가 없음 null로 수정해서 참조 횟수를 줄임
};

public class ArPlane : ArObject
{
    public AdInfo Info;
    public GameObject CommentCanvas;
    
    public ArPlane(AdInfo info, ClientInfo clientInfo)
    {
        Info = info;
        ClientInfoObj = clientInfo;
        Create();
    }

    private IEnumerator GetWebTexture()
    {
        UnityWebRequest textureWebRequest = UnityWebRequestTexture.GetTexture(Info.TextureUrl);
        yield return textureWebRequest.Send();

        Texture texture = DownloadHandlerTexture.GetContent(textureWebRequest);
        GameObj.GetComponent<MeshRenderer>().material.mainTexture = texture;
    }

    private IEnumerator CreateObject()
    {
        ObjectType = ArObjectType.AdPlane; // 타입 지정
        GameObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameObj.name = Info.Name;
        // Plane object의 DataContainer에 값 패싱
        GameObj.AddComponent<DataContainer>().BannerUrl = Info.BannerUrl; // URL 정보를 담을 DataContainer Component추가
        GameObj.GetComponent<DataContainer>().AdNum = Info.Id;
        GameObj.GetComponent<DataContainer>().CreatedCameraPosition = 
            new Vector3(ClientInfoObj.MainCamera.transform.position.x, ClientInfoObj.MainCamera.transform.position.y, ClientInfoObj.MainCamera.transform.position.z);
        GameObj.GetComponent<DataContainer>().ObjectType = ArObjectType.AdPlane;

        yield return new WaitUntil(() => ClientInfoObj.OriginalValuesAreSet); // 매번 확인하지 않도록 초기에 한번만 확인하도록 보완이 필요

        // 초기 포지션 설정
        Debug.Log("plane gps info : " + Info.GpsInfo[0] + " " + Info.GpsInfo[1] + " " + Info.GpsInfo[2]);
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(ClientInfoObj.StartingLatitude, ClientInfoObj.StartingLongitude, ClientInfoObj.StartingAltitude,
            Info.GpsInfo[0], Info.GpsInfo[1], Info.GpsInfo[2]);

        unityPosition.y = 0; // 고도 사용 안함.

        GameObj.transform.localScale = new Vector3(Info.Width, Info.Height, 1.0f);
        GameObj.transform.position = unityPosition;
        GameObj.transform.eulerAngles = new Vector3(90.0f, Info.Bearing - 90.0f, 90.0f); // gimbal lock이 발생하는 것 같음 90 0 -180으로 됨
        GameObj.transform.RotateAround(ClientInfoObj.MainCamera.transform.position, new Vector3(0.0f, 1.0f, 0.0f), -ClientInfoObj.StartingBearing); // 카메라 포지션 기준 회전
        // GameOBJ.transform.rotation = Quaternion.Euler(90.0f, -90.0f, 90.0f);
        // 모든 plane은 new Vector3(90.0f, -90.0f, 90.0f); 만큼 회전해야함 
    }

    public sealed override void Create()
    {
        if (Info.TextureUrl != null)
            StaticCoroutine.DoCoroutine(GetWebTexture());

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

public class ArCommentCanvas : ArObject
{
    Canvas arCommentCanvas;

    public ArCommentCanvas()
    {
        
    }

    public override void Create()
    {
        // Mesh Type Definition
        ObjectType = ArObjectType.ArCommentCanvas;
    }

    public override void Update()
    {
        throw new NotImplementedException();
    }

    public override void Destroy()
    {
        MonoBehaviour.Destroy(GameObj); // object 제거, Null ptr 설정
        GameObj = null;
    }
}