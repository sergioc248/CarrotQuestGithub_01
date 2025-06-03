public interface ISlowable
{
    /// <summary>
    /// scale < 1 slows movement/animation,
    /// scale == 1 restores normal speed.
    /// </summary>
    void SetTimeScale(float scale);
}
