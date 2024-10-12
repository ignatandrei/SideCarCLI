namespace SC_Interfaces;

public interface IRunWatcher
{
    public int Version { get; }
    public Task<int> Run(string[] args);

}
