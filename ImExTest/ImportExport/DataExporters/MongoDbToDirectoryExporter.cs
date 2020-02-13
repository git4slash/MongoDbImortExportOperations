using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.PathInfoProviders;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Sandbox.ProgressReporters;

namespace Sandbox.ImEx {
    class MongoDbToDirectoryExporter : IMongoDbToFromFileDataExporter {
        private MongoDbToDirectoryExporter() { }
        internal static MongoDbToDirectoryExporter Create(IMongoDatabase mongoDb,
                                                          IExportImportPathInfoProvider pathInfoProvider,
                                                          ISimmpleProgressReporter progressReporter = null) =>
            new MongoDbToDirectoryExporter {
                MongoDb = mongoDb,
                PathInfo = pathInfoProvider,
                ProgressReporter = progressReporter
            };

        #region Interface implementations
        public IMongoDatabase MongoDb { get; set; }

        public IExportImportPathInfoProvider PathInfo { get; set; }

        public ISimmpleProgressReporter ProgressReporter { get; set; }

        public void Export() => this.Export(string.Empty, CodeApproachType.AsyncFunctional);
        #endregion

        public void Export(string namePrefix, CodeApproachType testType = CodeApproachType.AsyncFunctional) =>
            loadFromDbThenSerializeToJsonThenSaveToDir(this.MongoDb, this.PathInfo, namePrefix, this.ProgressReporter, testType);

        public static void loadFromDbThenSerializeToJsonThenSaveToDir(IMongoDatabase db,
                        IExportImportPathInfoProvider pathInfo, string collectionNamePrefix,
                        ISimmpleProgressReporter progressReporter, CodeApproachType codeApproach = CodeApproachType.AsyncFunctional) {

            if ( db == null )
                return;

            var dirName = $@"{pathInfo.RootPath}\{pathInfo.WorkingDirName} {collectionNamePrefix}";

            progressReporter?.Start($"Start exporting data from {db.DatabaseNamespace.DatabaseName} to {dirName}");

            Directory.CreateDirectory(dirName);

            progressReporter?.Report($"{dirName} was created");

            var filter = Builders<BsonDocument>.Filter.Regex("name", $"^{collectionNamePrefix}");

            var localFunctions = new Dictionary<CodeApproachType, Action>() { { CodeApproachType.AsyncFunctional, doItAsync },
                                                                            { CodeApproachType.SyncFunctional, doItSync },
                                                                            { CodeApproachType.SyncNotFunctional, doItSyncAndNotFuncional } };

            localFunctions[codeApproach].Invoke();

            progressReporter?.End("Export complete");

            #region Local functions

            IMongoCollection<BsonDocument> getCollectionByName(string name) => db.GetCollection<BsonDocument>(name);

            void reportProgressAndSaveSyncFunc(Tuple<string, IEnumerable<string>> tuple) {
                progressReporter?.Report($"Saving {tuple.Item1}");
                saveSyncFunc(tuple.Item1, tuple.Item2);
                progressReporter?.Report($"Complete {tuple.Item1}");
            }

            void saveSyncFunc(string fileName, IEnumerable<string> linesToWrite) {
                using ( var streamWriter = new StreamWriter($"{dirName}/{fileName}.{pathInfo.FileExtension}") )
                    linesToWrite
                        .ToList()
                        .ForEach(line => streamWriter.WriteLine(line));
            }

            void doItAsync() =>
                db.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter })
                        .ToEnumerable()
                        .AsParallel()
                        .Select(getCollectionByName)
                        .Select(col => Tuple.Create(col.CollectionNamespace.CollectionName, serializeCollectionParallel(col)))
                        .ForAll(reportProgressAndSaveSyncFunc);

            void doItSync() =>
                db.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter })
                        .ToEnumerable()
                        .Select(getCollectionByName)
                        .Select(col => Tuple.Create(col.CollectionNamespace.CollectionName, serializeCollection(col)))
                        .ToList()
                        .ForEach(reportProgressAndSaveSyncFunc);

            void doItSyncAndNotFuncional() {
                using ( IAsyncCursor<string> cursor = db.ListCollectionNames(new ListCollectionNamesOptions { Filter = filter }) ) {
                    while ( cursor.MoveNext() ) {
                        foreach ( var collectionName in cursor.Current ) {

                            var collection = getCollectionByName(collectionName);

                            var colName = collection.CollectionNamespace.CollectionName;

                            progressReporter?.Report($"Saving {colName}");

                            using ( var fluentCursor = collection.Find(new BsonDocument()).ToCursor() )
                                while ( fluentCursor.MoveNext() ) {
                                    foreach ( var document in fluentCursor.Current ) {
                                        using ( var streamWriter = new StreamWriter($"{dirName}/{colName}.{pathInfo.FileExtension}") )
                                        using ( var stringWriter = new StringWriter() )
                                        using ( var jsonWriter = new JsonWriter(stringWriter) ) {

                                            var serializationContext = BsonSerializationContext.CreateRoot(jsonWriter);

                                            collection.DocumentSerializer.Serialize(serializationContext, document);

                                            var serializedLine = stringWriter.ToString();

                                            streamWriter.WriteLine(serializedLine);
                                        }
                                    }
                                }
                            progressReporter?.Report($"Complete");
                        }
                    }
                }
            }
            #endregion
        }

        private static IEnumerable<string> serializeCollection(IMongoCollection<BsonDocument> collection) =>
            collection.Find(new BsonDocument())
                .ToEnumerable()
                .Select(document => {
                    using ( var stringWriter = new StringWriter() )
                    using ( var jsonWriter = new JsonWriter(stringWriter) ) {
                        var serializationContext = BsonSerializationContext.CreateRoot(jsonWriter);
                        collection.DocumentSerializer.Serialize(serializationContext, document);
                        return stringWriter.ToString();
                    }
                });

        private static IEnumerable<string> serializeCollectionParallel(IMongoCollection<BsonDocument> collection) =>
            collection.Find(new BsonDocument())
                .ToEnumerable()
                .AsParallel()
                .Select(document => {
                    using ( var stringWriter = new StringWriter() )
                    using ( var jsonWriter = new JsonWriter(stringWriter) ) {
                        var serializationContext = BsonSerializationContext.CreateRoot(jsonWriter);
                        collection.DocumentSerializer.Serialize(serializationContext, document);
                        return stringWriter.ToString();
                    }
                });
    }
}