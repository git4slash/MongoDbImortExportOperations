using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Sandbox {
    public static class FIleSaveLoadHelper {

        /// <summary></summary>
        /// <returns></returns>
        public static string GetFileName() {
            using ( var fileDialog = CreateSaveFileDialog(defaultPath, getDefaultFileName) ) {
                var dialogResult = fileDialog.ShowDialog();
                if ( dialogResult == DialogResult.OK ) {
                    return fileDialog.FileName;
                } else {
                    throw new Exception();
                }
            }
        }

        /// <summary>Serialize given <paramref name="data"/> to JSON format and saves result in file by given <paramref name="path"/></summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="path">String representation of path.</param>
        /// <param name="fileName"></param>
        public static Task SerializeThenSaveAsync<T>(IEnumerable<T> data, string path, string fileName = null) {
            using ( var fileDialog = CreateSaveFileDialog(path, fileName) ) {
                var dialogResult = fileDialog.ShowDialog();
                if ( dialogResult == DialogResult.OK && fileDialog.FileName != null )
                    return write(fileDialog.FileName, fileDialog.FilterIndex, data);
                else
                    return Task.FromException(new OperationCanceledException());
            }
        }

        /// <summary>Serialize given <paramref name="data"/> to JSON format and saves result in file by auto-generated path</summary>
        /// <remarks> Default value is: <see cref="defaultPath"/> </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        public static void SerializeThenSaveAsync<T>(IEnumerable<T> data) =>
            SerializeThenSaveAsync(data, defaultPath, getDefaultFileName);

        private static string getDefaultFileName => $"Export from {DateTime.Now.ToString("MM_dd_yyyy")}";

        private static Task write<T>(string filePath, int informationFormatIndex, IEnumerable<T> data) {
            switch ( ( InformationFormat ) informationFormatIndex ) {
                case InformationFormat.JSON:
                    return writeJson(filePath, data);
                default:
                    return Task.FromException(new FormatException("Unknown format of data. Can't choose serializer."));
            }
        }

        private async static Task writeJson<T>(string file, IEnumerable<T> data) =>
            await Task.Run(() => {
                using ( var fs = new FileStream(file, FileMode.Create) )
                using ( var streamWriter = new StreamWriter(fs) )
                using ( var jsonWriter = new JsonWriter(streamWriter) ) {
                    var context = BsonSerializationContext.CreateRoot(jsonWriter);
                    jsonWriter.WriteStartDocument();
                    var serializedCollection =
                        data.AsParallel()
                            .ToJson()
                            .ToString();
                    streamWriter.WriteLine(serializedCollection);
                    jsonWriter.WriteEndDocument();
                }
            });

        private static SaveFileDialog CreateSaveFileDialog(string path, string fileName) =>
            new SaveFileDialog() {
                Filter = fileFilter,
                Title = "Export to file",
                InitialDirectory = path,
                FileName = fileName,
                CheckPathExists = true,
                FilterIndex = defaultFilterIndex,
                AddExtension = true,
                DefaultExt = "json"
            };


        /// <summary>Loads data from file by given <paramref name="path"/> then deserilizes them</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns><see cref="Task"/> with result <see cref="DialogResult"/>, <see cref="FileDialog.FileName"/>, <see cref="IEnumerable{T}"/></returns>
        public async static Task<(DialogResult, string, IEnumerable<T>)> LoadFromJsonLikeFileAndDeserealizeAsync<T>(string path = defaultPath) {
            using ( var fileDialog = new OpenFileDialog() {
                Filter = fileFilter,
                Title = "Import from file",
                InitialDirectory = path,
                CheckPathExists = true,
                FilterIndex = defaultFilterIndex
            } ) {
                var dialogResult = fileDialog.ShowDialog();
                var result = (dialogResult, fileDialog.SafeFileName, Enumerable.Empty<T>());
                if ( dialogResult != DialogResult.OK || fileDialog.FileName == null )
                    return result;
                switch ( fileDialog.FilterIndex ) {
                    case 1:
                        using ( FileStream fs = fileDialog.OpenFile() as FileStream ) {
                            //result.Item3 = await JsonSerializer.DeserializeAsync<IEnumerable<T>>(fs);
                            await Task.CompletedTask; // fake call
                            return result;
                        };
                    default:
                        return result;
                }
            }
        }

        #region Variables
        private static readonly string fileExtension = ".json";
        private const string defaultPath = "%USERPROFILE%\\Desktop\\";
        private static readonly string fileFilter = $"*.{fileExtension}|";
        private const int defaultFilterIndex = (int) InformationFormat.JSON;
        #endregion

        [System.Flags]
        internal enum InformationFormat {
            JSON = 1
        }
    }
}
