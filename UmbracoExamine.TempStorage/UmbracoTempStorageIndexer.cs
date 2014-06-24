using System.Collections.Specialized;
using System.IO;
using System.Web;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Umbraco.Core;
using Umbraco.Core.IO;
using Directory = System.IO.Directory;

namespace UmbracoExamine.TempStorage
{
    internal class UmbracoTempStorageIndexer
    {
        private string _tempPath;
        public Lucene.Net.Store.Directory LuceneDirectory { get; private set; }
        private static readonly object Locker = new object();
        public SnapshotDeletionPolicy Snapshotter { get; private set; }
        private bool _syncStorage = false;

        public UmbracoTempStorageIndexer()
        {
            IndexDeletionPolicy policy = new KeepOnlyLastCommitDeletionPolicy();
            Snapshotter = new SnapshotDeletionPolicy(policy);
        }

        public void Initialize(NameValueCollection config, string configuredPath, Lucene.Net.Store.Directory baseLuceneDirectory, Analyzer analyzer)
        {
            var codegenPath = HttpRuntime.CodegenDir;

            _tempPath = Path.Combine(codegenPath, configuredPath.TrimStart('~', '/').Replace("/", "\\"));

            if (config != null)
            {
                if (config["syncStorage"] != null)
                {
                    var attempt = config["syncStorage"].TryConvertTo<bool>();
                    if (attempt)
                    {
                        _syncStorage = attempt.Result;
                    }
                }
            }

            InitializeLocalIndexAndDirectory(baseLuceneDirectory, analyzer, configuredPath);
        }

        private void InitializeLocalIndexAndDirectory(Lucene.Net.Store.Directory baseLuceneDirectory, Analyzer analyzer,string configuredPath)
        {
            lock (Locker)
            {
                if (!Directory.Exists(_tempPath))
                {
                    Directory.CreateDirectory(_tempPath);
                }
                else
                {
                    //if we are syncing storage to the main file system to temp files, then clear out whatever is
                    //currently there since we'll re-copy it over
                    if (_syncStorage)
                    {
                        //clear it!
                        Directory.Delete(_tempPath, true);
                        //recreate it
                        Directory.CreateDirectory(_tempPath);
                    }
                }


                //if we are syncing storage to the main file system to temp files, then sync from the main FS to our temp FS
                if (_syncStorage)
                {
                    //copy index

                    using (new IndexWriter(
                        //read from the underlying/default directory, not the temp codegen dir
                        baseLuceneDirectory,
                        analyzer,
                        Snapshotter,
                        IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        try
                        {
                            var basePath = IOHelper.MapPath(configuredPath);

                            var commit = Snapshotter.Snapshot();
                            var fileNames = commit.GetFileNames();
                            
                            foreach (var fileName in fileNames)
                            {
                                File.Copy(
                                    Path.Combine(basePath, "Index", fileName),
                                    Path.Combine(_tempPath, Path.GetFileName(fileName)), true);
                            }

                            var segments = commit.GetSegmentsFileName();
                            if (segments.IsNullOrWhiteSpace() == false)
                            {
                                File.Copy(
                                    Path.Combine(basePath, "Index", segments),
                                    Path.Combine(_tempPath, Path.GetFileName(segments)), true);
                            }
                        }
                        finally
                        {
                            Snapshotter.Release();
                        }
                    }

                    //create the custom lucene directory which will keep the main and temp FS's in sync

                    LuceneDirectory = new TempStorageDirectory(
                        new DirectoryInfo(_tempPath),
                        baseLuceneDirectory);
                }
                else
                {
                    //just return a normal lucene directory that uses the codegen folder

                    LuceneDirectory = FSDirectory.Open(new DirectoryInfo(_tempPath));
                }

            }
        }
    }
}