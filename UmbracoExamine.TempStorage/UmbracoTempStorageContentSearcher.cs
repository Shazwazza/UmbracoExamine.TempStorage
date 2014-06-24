using System.IO;
using System.Web;
using Examine.LuceneEngine.Config;
using Lucene.Net.Store;
using Umbraco.Core;

namespace UmbracoExamine.TempStorage
{
    public class UmbracoTempStorageContentSearcher : UmbracoExamineSearcher
    {
        private volatile Lucene.Net.Store.Directory _directory;
        private static readonly object Locker = new object();
        private string _tempPath;
        private bool _syncStorage = false;

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
        }

        protected override Lucene.Net.Store.Directory GetLuceneDirectory()
        {
            if (_directory == null)
            {
                lock(Locker)
                {
                    if (_directory == null)
                    {
                        if (_syncStorage)
                        {
                            _directory = new TempStorageDirectory(
                                new DirectoryInfo(_tempPath),
                                base.GetLuceneDirectory());
                        }
                        else
                        {
                            //not syncing just use a normal lucene directory
                            _directory = FSDirectory.Open(new DirectoryInfo(_tempPath));
                        }                        
                    }
                }
            }

            return _directory;
        }
    }
}