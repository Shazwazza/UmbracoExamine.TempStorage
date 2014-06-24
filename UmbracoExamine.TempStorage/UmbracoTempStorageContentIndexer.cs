using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Examine;
using Examine.LuceneEngine.Config;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Umbraco.Core;
using Directory = System.IO.Directory;

namespace UmbracoExamine.TempStorage
{
    public class UmbracoTempStorageContentIndexer : UmbracoContentIndexer
    {
        private string _tempPath;
        private Lucene.Net.Store.Directory _directory;
        private static readonly object Locker = new object();
        private readonly SnapshotDeletionPolicy _snapshotter;
        private bool _syncStorage = false;

        public UmbracoTempStorageContentIndexer()
        {
            IndexDeletionPolicy policy = new KeepOnlyLastCommitDeletionPolicy();
            _snapshotter = new SnapshotDeletionPolicy(policy);
        }

        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            base.Initialize(name, config);

            var indexSet = IndexSets.Instance.Sets[IndexSetName];
            var configuredPath = indexSet.IndexPath;

            var codegenPath = HttpRuntime.CodegenDir;

            _tempPath = Path.Combine(codegenPath, configuredPath.TrimStart('~'));

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

            InitializeLocalIndexAndDirectory();
        }

        private void InitializeLocalIndexAndDirectory()
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
                        base.GetLuceneDirectory(),
                        IndexingAnalyzer,
                        _snapshotter,
                        IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        try
                        {
                            var commit = _snapshotter.Snapshot();
                            var fileNames = commit.GetFileNames();
                            foreach (var fileName in fileNames)
                            {
                                File.Copy(fileName, Path.Combine(_tempPath, Path.GetFileName(fileName)), true);
                            }
                        }
                        finally
                        {
                            _snapshotter.Release();
                        }
                    }

                    //create the custom lucene directory which will keep the main and temp FS's in sync

                    _directory = new TempStorageDirectory(
                        new DirectoryInfo(_tempPath),
                        base.GetLuceneDirectory());
                }
                else
                {
                    //just return a normal lucene directory that uses the codegen folder

                    _directory = FSDirectory.Open(new DirectoryInfo(_tempPath));
                }
                
            }
        }

        public override Lucene.Net.Store.Directory GetLuceneDirectory()
        {
            if (_directory == null)
            {
                throw new InvalidOperationException("The temp storage provider has not been initialized");
            }

            return _directory;
        }
        
        public override IndexWriter GetIndexWriter()
        {
            return new IndexWriter(GetLuceneDirectory(), IndexingAnalyzer, 
                //create the writer with the snapshotter, though that won't make too much a difference because we are not keeping the writer open unless using nrt
                // which we are not currently.
                _snapshotter, 
                IndexWriter.MaxFieldLength.UNLIMITED);
        }

    }
}
