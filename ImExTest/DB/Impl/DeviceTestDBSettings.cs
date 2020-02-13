namespace Sandbox.Db {
    internal class DeviceTestDBSettings : IDBSettingsProvider {
        public string ConnectionString { get => $"mongodb://{Host}:{Port}";}
        public string DatabaseName { get => "DeviceTestDB"; set => DatabaseName = value; }
        public int Port { get => 27317; set { Port = value; } }
        public string Host { get => "localhost"; set { Host = value; } }
    }
}
