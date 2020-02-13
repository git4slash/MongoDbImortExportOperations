using System;
using System.Linq;
using System.Threading;
using Sandbox.Db;
using Sandbox.PathInfoProviders;
using Sandbox.ProgressReporters;

namespace Sandbox.ImEx {
    static class ExportImportTests {
        internal static void RunTests() {
            var progressReporter = StandardOutputStreamProgressReporter.Create();

            progressReporter.Start("Test was started");

            var deviceGuid = smallCollectionNamePrefix;
            //deviceGuid = veryLargeCollectionNamePrefix;

            var exportPathInfoProvider = StaticDataPathInfoProvider.Create();

            var importPathInfoProvider = new StaticDataPathInfoProvider {
                RootPath = exportPathInfoProvider.RootPath,
                WorkingDirName = $"{exportPathInfoProvider.WorkingDirName} {deviceGuid}"
            };

            var exportOperation = MongoDbToDirectoryExporter.Create(DBHelper.MeasurementCenterDB, exportPathInfoProvider);
            var importOperation = DirectoryToMongoDbExporter.Create(DBHelper.DeviceTestDB, importPathInfoProvider);

            ( ( CodeApproachType[] ) Enum.GetValues(typeof(CodeApproachType)) )
                        //.Where(type => type == CodeApproachType.AsyncFunctional)
                        //.Where(type => type == CodeApproachType.SyncFunctional)
                        //.Where(type => type == CodeApproachType.SyncNotFunctional)
                        .ToDictionary(codeApproach => codeApproach,
                                    codeApproach => {
                                        var durations = getDurationsOfImExOperationsUsing(codeApproach);
                                        var waitTimeInSeconds = 5;
                                        progressReporter.Report($"Wait {waitTimeInSeconds} seconds...\n");
                                        Thread.Sleep(waitTimeInSeconds * 1000); // lets give OS chance to end all IO operations before starting next test
                                        return durations;
                                    })
                        .Select(toReportLine)
                        .ToList()
                        .ForEach(Console.WriteLine);

            progressReporter.End("Test was ended.");

            Console.Write("\n\nPress any key.");
            Console.ReadKey();

            #region Local functions

            Tuple<TimeSpan, TimeSpan> getDurationsOfImExOperationsUsing(CodeApproachType codeApproach) {

                var startExport = DateTime.Now;
                exportOperation.Export(deviceGuid, codeApproach);
                var exportDuration = DateTime.Now.Subtract(startExport);
                progressReporter.Report($"Task type: {codeApproach}Export ended in {exportDuration.TotalSeconds} seconds");

                var startImport = DateTime.Now;
                importOperation.Export(codeApproach);
                var importDuration = DateTime.Now.Subtract(startImport);
                progressReporter.Report($"Task type: {codeApproach}Import ended in {importDuration.TotalSeconds} seconds");

                var totalDuration = exportDuration.Add(importDuration);
                progressReporter.Report($"Task type: {codeApproach} duration: {totalDuration.TotalSeconds} seconds");

                return Tuple.Create(exportDuration, importDuration);
            }

            string toReportLine(System.Collections.Generic.KeyValuePair<CodeApproachType, Tuple<TimeSpan, TimeSpan>> pair, int i) =>
                $"{i} | {pair.Key,-20} | Export: {pair.Value.Item1.TotalSeconds,-12} sec | Import: {pair.Value.Item2.TotalSeconds,-12} sec | Total: {pair.Value.Item1.Add(pair.Value.Item2).TotalSeconds,-12} sec";

            #endregion
        }

        #region Variables

        private const string smallCollectionNamePrefix = "437789d32ea045c59e643eb27c61d68c";
        private const string veryLargeCollectionNamePrefix   = "c7176ce28ed14f4dab4641a4991329ad";

        #endregion
    }
}