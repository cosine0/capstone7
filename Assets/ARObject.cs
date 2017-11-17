using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// 오브젝트의 종류를 나타내는 타입
/// </summary>
public enum ArObjectType : int { ArObjectError = 0, AdPlane, ArCommentCanvas, Ar3dObject, ArComment };

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


public class Ad3dInfo
{
    public int ObjectNumber;
    public Vector3 GpsInfo;
    public float Bearing = 0.0f;
    public string typeName = null;
    public string TextAlternateToTexture = "";
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
    /// 오브젝트 종류.
    /// </summary>
    public ArObjectType ObjectType;
    
    /// <summary>
    /// Unity 공간 상에 있는 물체에 대한 참조.
    /// </summary>
    public GameObject GameObj;
    
    /// <summary>
    /// DontDestroyOnLoad 오브젝트인 클라이언트 정보
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
    public GameObject CommentCanvas;

    /// <summary>
    /// 생성자. 광고 정보를 바탕으로 Unity 공간에 물체를 생성한다.
    /// </summary>
    /// <param name="info">광고 정보 오브젝트</param>
    /// <param name="clientInfo">클라이언트 정보 오브젝트</param>
    public ArPlane(AdInfo info, ClientInfo clientInfo)
    {
        Info = info;
        ClientInfoObj = clientInfo;
        Create();
    }

    public sealed override void Create()
    {
        StaticCoroutine.DoCoroutine(CreateObject());
        if (Info.TextureUrl != null)
            StaticCoroutine.DoCoroutine(GetWebTexture());

    }

    /// <summary>
    /// Info.TextureUrl에 있는 URL에서 이미지를 다운로드해서 GameObj의 텍스처로 적용하는 코루틴.
    /// </summary>
    private IEnumerator GetWebTexture()
    {
        UnityWebRequest textureWebRequest = UnityWebRequestTexture.GetTexture(Info.TextureUrl);
        yield return textureWebRequest.Send();

        Texture texture = DownloadHandlerTexture.GetContent(textureWebRequest);

        yield return new WaitUntil(() => GameObj != null);
        GameObj.GetComponent<MeshRenderer>().material.mainTexture = texture;
    }

    /// <summary>
    /// <see cref="Info"/>를 바탕으로 Unity 공간에 Plane 오브젝트를 생성하고 GameObj 멤버에 그 참조를 할당하는 코루틴.
    /// </summary>
    private IEnumerator CreateObject()
    {
        ObjectType = ArObjectType.AdPlane; // 타입 지정
        GameObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameObj.name = Info.Name;
        // Plane object의 DataContainer에 값 패싱
        GameObj.AddComponent<DataContainer>().BannerUrl = Info.BannerUrl; // URL 정보를 담을 DataContainer Component추가
        GameObj.GetComponent<DataContainer>().AdNum = Info.AdNumber;
        GameObj.GetComponent<DataContainer>().CreatedCameraPosition = 
            new Vector3(ClientInfoObj.MainCamera.transform.position.x, ClientInfoObj.MainCamera.transform.position.y, ClientInfoObj.MainCamera.transform.position.z);
        GameObj.GetComponent<DataContainer>().ObjectType = ArObjectType.AdPlane;

        // GPS 정보를 사용하기 위해 GPS 초기화가 안된 경우 대기.
        if (Application.platform == RuntimePlatform.Android)
            yield return new WaitUntil(() => ClientInfoObj.OriginalValuesAreSet);

        // 초기 포지션 설정
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(ClientInfoObj.StartingLatitude, ClientInfoObj.StartingLongitude, ClientInfoObj.StartingAltitude,
            Info.GpsInfo[0], Info.GpsInfo[1], Info.GpsInfo[2]);

        unityPosition.y = 0; // 고도 사용 안함.

        GameObj.transform.localScale = new Vector3(Info.Width, Info.Height, 1.0f);
        GameObj.transform.position = unityPosition;
        GameObj.transform.eulerAngles = new Vector3(90.0f, Info.Bearing - 90.0f, 90.0f);
        GameObj.transform.RotateAround(ClientInfoObj.MainCamera.transform.position, new Vector3(0.0f, 1.0f, 0.0f), -ClientInfoObj.CorrectedBearingOffset); // 카메라 포지션 기준 회전
        // GameOBJ.transform.rotation = Quaternion.Euler(90.0f, -90.0f, 90.0f);
        // 모든 plane은 new Vector3(90.0f, -90.0f, 90.0f); 만큼 회전해야함 
    }

    public override void Update()
    {
        // 위치 또는 애니메이션 업데이트
    }

    /// <summary>
    /// GameObj에 있는 물체를 Unity 공간에서 파괴한다.
    /// </summary>
    public override void Destroy()
    {
        MonoBehaviour.Destroy(GameObj);
        GameObj = null;
        Info = null;
    }
}



/// <summary>
/// 3d 형태의 광고 오브젝트.
/// </summary>
public class Ar3dPlane : ArObject
{
    public Ad3dInfo Info;
    public GameObject CommentCanvas;

    /// <summary>
    /// 생성자. 광고 정보를 바탕으로 Unity 공간에 물체를 생성한다.
    /// </summary>
    /// <param name="info">광고 정보 오브젝트</param>
    /// <param name="clientInfo">클라이언트 정보 오브젝트</param>
    public Ar3dPlane(Ad3dInfo info, ClientInfo clientInfo)
    {
        Info = info;
        ClientInfoObj = clientInfo;
        Create();
    }

    public sealed override void Create()
    {
        StaticCoroutine.DoCoroutine(CreateObject());

    }

    /// <summary>
    /// <see cref="Info"/>를 바탕으로 Unity 공간에 Plane 오브젝트를 생성하고 GameObj 멤버에 그 참조를 할당하는 코루틴.
    /// </summary>
    private IEnumerator CreateObject()
    {
        // 초기 포지션 설정
        Vector3 unityPosition = GpsCalulator.CoordinateDifference(ClientInfoObj.StartingLatitude, ClientInfoObj.StartingLongitude, ClientInfoObj.StartingAltitude,
            Info.GpsInfo[0], Info.GpsInfo[1], Info.GpsInfo[2]);

        GameObj = createObject(Info.typeName, unityPosition);
        GameObj.name = Info.typeName;
        ObjectType = ArObjectType.Ar3dObject; // 타입 지정
        // Plane object의 DataContainer에 값 패싱
        GameObj.GetComponent<DataContainer>().AdNum = Info.ObjectNumber;
        GameObj.GetComponent<DataContainer>().CreatedCameraPosition =
            new Vector3(ClientInfoObj.MainCamera.transform.position.x, ClientInfoObj.MainCamera.transform.position.y, ClientInfoObj.MainCamera.transform.position.z);
        GameObj.GetComponent<DataContainer>().ObjectType = ArObjectType.Ar3dObject;

        // GPS 정보를 사용하기 위해 GPS 초기화가 안된 경우 대기.
        if (Application.platform == RuntimePlatform.Android)
            yield return new WaitUntil(() => ClientInfoObj.OriginalValuesAreSet);

        

        unityPosition.y = 0; // 고도 사용 안함.

        GameObj.transform.position = unityPosition;
        GameObj.transform.eulerAngles = new Vector3(90.0f, Info.Bearing - 90.0f, 90.0f);
        GameObj.transform.RotateAround(ClientInfoObj.MainCamera.transform.position, new Vector3(0.0f, 1.0f, 0.0f), -ClientInfoObj.CorrectedBearingOffset); // 카메라 포지션 기준 회전
        // GameOBJ.transform.rotation = Quaternion.Euler(90.0f, -90.0f, 90.0f);
        // 모든 plane은 new Vector3(90.0f, -90.0f, 90.0f); 만큼 회전해야함 

    }

    public GameObject createObject(string typeName, Vector3 unityPosition)
    {
        //Instantiate(obj, new Vector3(40, -1, 0.0f), Quaternion.identity);
        var transform = MainBehaviour.Instantiate(Resources.Load("Prefabs/" + typeName), unityPosition, Quaternion.identity) as Transform;

        return transform.gameObject;
    }

    public override void Update()
    {
        // 위치 또는 애니메이션 업데이트
    }

    /// <summary>
    /// GameObj에 있는 물체를 Unity 공간에서 파괴한다.
    /// </summary>
    public override void Destroy()
    {
        MonoBehaviour.Destroy(GameObj);
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
        MonoBehaviour.Destroy(GameObj);
        GameObj = null;
    }
}