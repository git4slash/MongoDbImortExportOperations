using System;

namespace Sandbox.PathInfoProviders {
    internal class StaticDataPathInfoProvider : IExportImportPathInfoProvider {
        public string RootPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public string WorkingDirName { get; set; } = $"Export {DateTime.Now.ToString("ddMMMMyyyy")}";

        public string FileExtension { get; set; } = "json";

        internal static StaticDataPathInfoProvider Create() => new StaticDataPathInfoProvider();
    }
}
