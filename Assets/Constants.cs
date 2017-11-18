/// <summary>
/// 글로벌 상수들을 갖는 static 클래스.
/// </summary>
static class Constants
{
    public const float SmoothFactorCompass = 0.125f;
    public const float SmoothThresholdCompass = 45.0f;
    public const float CompassMeasureIntervalInSecond = 0.1f;
    public const float GpsMeasureIntervalInSecond = 0.3f;
    public const int BearingDifferenceBufferSize = 600;
}