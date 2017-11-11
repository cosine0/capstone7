using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// 광고 하나를 나타내는 객체.
/// </summary>
public class AdInfo
{
    public int AdNumber;
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

/// <summary>
/// 댓글 하나를 나타내는 객체.
/// </summary>
public class CommentInfo
{
    public string Id = "";
    public DateTime DateTime;
    public string Comment = "";
}

/// <summary>
/// AR 공간상에 나타낼 수 있는 오브젝트.
/// </summary>
public abstract class ArObject
{
    public int Id;
    /// <summary>
    /// 오브젝트의 종류를 나타내는 타입: 광고판, 댓글, 3D 오브젝트
    /// </summary>
    public enum ArObjectType : int { ArObjectError = 0, AdPlane, ArComment };
    
    /// <summary>
    /// 오브젝트 종류
    /// </summary>
    public ArObjectType ObjectType;
    
    /// <summary>
    /// Unity 공간 상에 있는 물체.
    /// </summary>
    public GameObject GameObj;
    
    /// <summary>
    /// 글로벌 DontDestroyOnLoad 오브젝트인 클라이언트 정보
    /// </summary>
    public ClientInfo ClientInfoObj;


    /// <summary>
    /// Unity 공간에 물체를 생성해서 <see cref="GameObj"/>멤버로 가져오는 함수.
    /// </summary>
    public abstract void Create();

    /// <summary>
    /// 물체의 애니메이션 등 업데이트를 처리하는 함수.
    /// </summary>
    public abstract void Update();
    
    /// <summary>
    /// Unity 공간에서 <see cref="GameObj"/>에 있는 물체를 제거하는 함수.
    /// </summary>
    public abstract void Destroy();
};

/// <summary>
/// 직사각형 판 형태의 광고 오브젝트.
/// </summary>
public class ArPlane : ArObject
{
    public AdInfo Info;

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
        GameObj.AddComponent<DataContainer>().BannerUrl = Info.BannerUrl; // URL 정보를 담을 DataContainer Component추가
        GameObj.GetComponent<DataContainer>().AdNum = Info.AdNumber;
        GameObj.GetComponent<DataContainer>().CreatedCameraPosition = 
            new Vector3(ClientInfoObj.MainCamera.transform.position.x, ClientInfoObj.MainCamera.transform.position.y, ClientInfoObj.MainCamera.transform.position.z);

        yield return new WaitUntil(() => ClientInfoObj.OriginalValuesAreSet); // 매번 확인하지 않도록 초기에 한번만 확인하도록 보완이 필요

        // 초기 포지션 설정
        Debug.Log("plane gps info : " + Info.GpsInfo[0] + " " + Info.GpsInfo[1] + " " + Info.GpsInfo[2]);
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(ClientInfoObj.StartingLatitude, ClientInfoObj.StartingLongitude, ClientInfoObj.StartingAltitude,
            Info.GpsInfo[0], Info.GpsInfo[1], Info.GpsInfo[2]);

        unityPosition.y = 0; // 고도 사용 안함.

        GameObj.transform.localScale = new Vector3(Info.Width, Info.Height, 1.0f);
        GameObj.transform.position = unityPosition;
        GameObj.transform.eulerAngles = new Vector3(90.0f, Info.Bearing - 90.0f, 90.0f);
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
        MonoBehaviour.Destroy(GameObj);
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
        MonoBehaviour.Destroy(GameObj);
        GameObj = null;
        Comment = null;
    }
}