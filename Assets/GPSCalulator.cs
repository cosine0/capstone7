using UnityEngine;

/// <summary>
/// GPS 관련 계산 함수를 갖는 static 클래스.
/// </summary>
static class GpsCalulator
{
    /// <summary>
    /// GPS (위도, 경도) 좌표가 (latitude1, longitude1)에서 (latitude2, longitude2)로 바뀔 때 이동 거리(미터)와 방위각(도)
    /// </summary>
    /// <returns>(이동 거리, 방위각)</returns>
    private static Vector2 DistanceAndBrearing(float latitude1, float longitude1, float latitude2, float longitude2)
    {
        const float earthRadiusMeter = 6378137.0f;
        float radianLatitude1 = latitude1 * Mathf.PI / 180.0f;
        float radianLatitude2 = latitude2 * Mathf.PI / 180.0f;
        float latitudeDifference = radianLatitude2 - radianLatitude1;

        float radianLongitude1 = longitude1 * Mathf.PI / 180.0f;
        float radianLongitude2 = longitude2 * Mathf.PI / 180.0f;
        float longitudeDifference = radianLongitude2 - radianLongitude1;

        float a = Mathf.Sin(latitudeDifference / 2.0f) * Mathf.Sin(latitudeDifference / 2.0f) +
                Mathf.Cos(radianLatitude1) * Mathf.Cos(radianLatitude2) *
                Mathf.Sin(longitudeDifference / 2.0f) * Mathf.Sin(longitudeDifference / 2.0f);
        float angualrDistance = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        float distance = earthRadiusMeter * angualrDistance;

        float y = Mathf.Sin(longitudeDifference) * Mathf.Cos(radianLatitude1);
        float x = Mathf.Cos(radianLatitude1) * Mathf.Sin(radianLatitude2) -
                Mathf.Sin(radianLatitude1) * Mathf.Cos(radianLatitude2) * Mathf.Cos(longitudeDifference);
        float bearing = Mathf.Atan2(y, x);

        return new Vector2(distance, bearing);
    }
    /// <summary>
    /// GPS (위도, 경도, 고도) 좌표가 (latitude1, longitude1, altitude1)에서 (latitude2, longitude2, altitude2)로 바뀔 때 직교 좌표계에서의 이동 거리(미터).
    /// </summary>
    /// <returns>(동-서 이동거리, 높이 변화, 북-남 이동거리)</returns>
    public static Vector3 CoordinateDifference(float latitude1, float longitude1, float altitude1, float latitude2, float longitude2, float altitude2)
    {
        Vector3 distanceBearingVector = DistanceAndBrearing(latitude1, longitude1, latitude2, longitude2);
        float distance = distanceBearingVector[0];
        float bearing = distanceBearingVector[1];
        float northDifference = distance * Mathf.Cos(bearing);
        float eastDifference = distance * Mathf.Sin(bearing);
        float heightDifference = altitude2 - altitude1;
        return new Vector3(eastDifference, heightDifference, northDifference);
    }
}