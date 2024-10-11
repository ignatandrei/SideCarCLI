namespace SC_Interfaces;
public interface IRunWatcherDetect
{
    public string Folder { get; set; }
    public Task<IRunWatcher[]> DetectVersions();
}

public interface IRunWatcher
{
    public int Version { get; }
    public Task<int> Run(string[] args);

}

public interface IRunWatcherV1: IRunWatcher
{
    public int DelaySeconds { get; set; }

}

public interface IRunWatcherFile 
{
    public Version DetectFile();
}