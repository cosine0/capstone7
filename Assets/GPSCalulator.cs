using UnityEngine;

static class GPSCalulator
{
    static public Vector2 DistanceAndBrearing(float latitude1, float longitude1, float latitude2, float longitude2)
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

    static public Vector3 CoordinateDifference(float latitude1, float longitude1, float latitude2, float longitude2)
    {
        Vector3 distanceBearingVector = DistanceAndBrearing(latitude1, longitude1, latitude2, longitude2);
        float distance = distanceBearingVector[0];
        float bearing = distanceBearingVector[1];
        float xDifference = distance * Mathf.Cos(bearing);
        float yDifference = distance * Mathf.Sin(bearing);
        return new Vector3(yDifference, 0.0f, xDifference);
    }
}