namespace Sandbox.Db {
    /// <summary>Provides necessary data to connect to db</summary>
    public interface IDBSettingsProvider {
        int Port { get; set; }
        string Host { get; set; }
        string ConnectionString { get; }
        string DatabaseName { get; set; }
    }
}
