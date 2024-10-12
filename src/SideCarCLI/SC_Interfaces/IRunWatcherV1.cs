namespace SC_Interfaces;

public interface IRunWatcherV1: IRunWatcher
{
    public int DelaySeconds { get; set; }

}
