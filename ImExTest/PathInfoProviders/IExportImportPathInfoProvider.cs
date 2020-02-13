namespace Sandbox.PathInfoProviders {
    /// <summary>Information provider for file and directory operations</summary>
    interface IExportImportPathInfoProvider {
        
        /// <summary>Full path to root of <seealso cref="IExportImportPathInfoProvider.WorkingDirName"/> </summary>
        string RootPath { get; }
        
        /// <summary>Name of directory to work</summary>
        string WorkingDirName { get; }

        /// <summary>Extension for files in <seealso cref="IExportImportPathInfoProvider.WorkingDirName"/></summary>
        /// <remarks>without stars and dots. Also "exe" is correct and "*.exe" - not</remarks>
        string FileExtension { get; }
    }
}
