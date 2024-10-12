namespace SC_Interfaces;

public interface IRunWatcherDetect
{
    public string Folder { get; set; }
    public Task<IRunWatcher[]> DetectVersions();
}
