using Sandbox.PathInfoProviders;
using MongoDB.Driver;
using Sandbox.ProgressReporters;

namespace Sandbox.ImEx {

    /// <summary></summary>
    interface IMongoDbToFromFileDataExporter {

        IMongoDatabase MongoDb { get; set; }

        IExportImportPathInfoProvider PathInfo { get; set; }

        ISimmpleProgressReporter ProgressReporter { get; set; }

        void Export();
    }
}