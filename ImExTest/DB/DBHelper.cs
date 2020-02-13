using MongoDB.Bson;
using MongoDB.Driver;

namespace Sandbox.Db {
    public static class DBHelper {
        static DBHelper() {
            DeviceTestDB = LocalClient?.GetDatabase(DeviceTestDBSettings.DatabaseName);
            MeasurementCenterDB = LocalClient?.GetDatabase(MeasurementCenterDBSettings.DatabaseName);
        }

        private static IDBSettingsProvider DeviceTestDBSettings => new DeviceTestDBSettings();
        private static IDBSettingsProvider MeasurementCenterDBSettings => new MeasurementCenterDBSettings();

        public static MongoClient LocalClient => new MongoClient(
                    new MongoClientSettings() { Server = new MongoServerAddress(MeasurementCenterDBSettings.Host, MeasurementCenterDBSettings.Port) });

        public static IMongoDatabase DeviceTestDB { get; private set; }

        public static IMongoDatabase MeasurementCenterDB { get;  private set; }

        public static void dropCollectionIfExists(IMongoDatabase db, string name) {
            if ( isCollesctionExists(db, name) )
                db.DropCollection(name);
        }

        public static bool isCollesctionExists(IMongoDatabase db, string name) =>
            db.ListCollectionNames(new ListCollectionNamesOptions() { Filter = new BsonDocument("name", name) }).Any();
    }
}
