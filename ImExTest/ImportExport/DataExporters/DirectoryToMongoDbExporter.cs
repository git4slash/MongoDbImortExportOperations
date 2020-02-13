using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.PathInfoProviders;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Sandbox.ProgressReporters;
using Sandbox.Db;

namespace Sandbox.ImEx {
    class DirectoryToMongoDbExporter : IMongoDbToFromFileDataExporter {
        private DirectoryToMongoDbExporter() { }

        internal static DirectoryToMongoDbExporter Create(IMongoDatabase mongoDb,
                                                          IExportImportPathInfoProvider pathInfoProvider,
                                                          ISimmpleProgressReporter progressReporter = null) =>
            new DirectoryToMongoDbExporter {
                MongoDb = mongoDb,
                PathInfo = pathInfoProvider,
                ProgressReporter = progressReporter
            };

        #region Interface implementations
        public IMongoDatabase MongoDb { get; set; }
        public IExportImportPathInfoProvider PathInfo { get; set; }
        public ISimmpleProgressReporter ProgressReporter { get; set; }

        public void Export() => this.Export(codeApproach: CodeApproachType.AsyncFunctional);
        #endregion

        public void Export(CodeApproachType codeApproach = CodeApproachType.AsyncFunctional) =>
            loadFromDirThenDeserializeFromJsonThenSaveToDb(MongoDb, PathInfo, ProgressReporter, codeApproach);

        public static void loadFromDirThenDeserializeFromJsonThenSaveToDb(IMongoDatabase db, IExportImportPathInfoProvider pathInfoProvider,
                             ISimmpleProgressReporter progressReporter = null, CodeApproachType codeApproach = CodeApproachType.AsyncFunctional) {

            var dirPath = $@"{pathInfoProvider.RootPath}\{pathInfoProvider.WorkingDirName}";

            if ( Directory.Exists(dirPath) && db != null ) {

                var localFunctions = new Dictionary<CodeApproachType, Action>() { { CodeApproachType.AsyncFunctional,   doItAsync },
                                                                                  { CodeApproachType.SyncFunctional,    doItSync },
                                                                                  { CodeApproachType.SyncNotFunctional, doItSyncAndNotFuncional } };

                progressReporter?.Start($"Start importing data from {dirPath} to {db.DatabaseNamespace.DatabaseName}");

                localFunctions[codeApproach].Invoke();

                progressReporter?.End($"All data from {dirPath} was loaded successfully");

            }

            #region Local functions

            void doItAsync() =>
                    Directory.EnumerateFiles(dirPath, $"*.{pathInfoProvider.FileExtension}", SearchOption.TopDirectoryOnly)
                             .AsParallel()

                             .Select(fullPath => new {
                                 Name = Path.GetFileNameWithoutExtension(fullPath),
                                 Lines = readLinesFromFile(fullPath)
                             })

                             .Select(file => new {
                                 Collection = dropThenCreateCollection(db, file.Name),
                                 Documents = file.Lines.Select(deserializeFunc)
                             })

                             .ForAll(async deserialized => {
                                 var name = deserialized.Collection.CollectionNamespace.CollectionName;
                                 progressReporter?.Report($"Loading {name}");
                                 await deserialized.Collection.InsertManyAsync(deserialized.Documents);
                                 progressReporter?.Report($"Complete {name}");
                             });

            void doItSync() =>
                    Directory.EnumerateFiles(dirPath, $"*.{pathInfoProvider.FileExtension}", SearchOption.TopDirectoryOnly)
                             .Select(fullPath => new {
                                 Name = Path.GetFileNameWithoutExtension(fullPath),
                                 Lines = readLinesFromFile(fullPath)
                             })

                             .Select(file => new {
                                 Collection = dropThenCreateCollection(db, file.Name),
                                 Documents = file.Lines.Select(deserializeFunc)
                             })
                             .ToList()
                             .ForEach(deserialized => {
                                 var name = deserialized.Collection.CollectionNamespace.CollectionName;
                                 progressReporter?.Report($"Loading {name}");
                                 deserialized.Collection.InsertMany(deserialized.Documents);
                                 progressReporter?.Report($"Complete {name}");
                             });

            void doItSyncAndNotFuncional() {
                foreach ( var fullPath in Directory.EnumerateFiles(dirPath, $"*.{pathInfoProvider.FileExtension}", SearchOption.TopDirectoryOnly) ) {

                    var collectionName = Path.GetFileNameWithoutExtension(fullPath);

                    var collection = dropThenCreateCollection(db, collectionName);

                    progressReporter?.Report($"Loading {collectionName}");

                    using ( var streamReader = new StreamReader(fullPath) ) {

                        string line;

                        while ( ( line = streamReader.ReadLine() ) != null ) {
                            var document = BsonSerializer.Deserialize<BsonDocument>(line);

                            collection.InsertOne(document);
                        }
                    }

                    progressReporter?.Report($"Complete {collectionName}");
                }
            }

            BsonDocument deserializeFunc(string line) => BsonSerializer.Deserialize<BsonDocument>(line);

            #endregion
        }

        private static IMongoCollection<BsonDocument> dropThenCreateCollection(IMongoDatabase db, string colName) {
            DBHelper.dropCollectionIfExists(db, colName);
            return db.GetCollection<BsonDocument>(colName);
        }

        private static IEnumerable<string> readLinesFromFile(string file) {
            using ( var streamReader = new StreamReader(file) ) {
                string line;
                while ( ( line = streamReader.ReadLine() ) != null )
                    yield return line;
            }
        }

    }
}
