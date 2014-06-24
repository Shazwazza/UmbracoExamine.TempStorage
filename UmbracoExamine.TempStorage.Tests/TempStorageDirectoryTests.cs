using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using Directory = System.IO.Directory;
using Version = Lucene.Net.Util.Version;

namespace UmbracoExamine.TempStorage.Tests
{
    [TestFixture]
    public class TempStorageDirectoryTests
    {

        /// <summary>
        /// Gets the current assembly directory.
        /// </summary>
        /// <value>The assembly directory.</value>
        static public string CurrentAssemblyDirectory
        {
            get
            {
                var codeBase = typeof(TempStorageDirectoryTests).Assembly.CodeBase;
                var uri = new Uri(codeBase);
                var path = uri.LocalPath;
                return Path.GetDirectoryName(path);
            }
        }

        [SetUp]
        public void Init()
        {
            var dir = Path.Combine(CurrentAssemblyDirectory, "TestData");
            if (Directory.Exists(dir))
            {
                Directory.GetDirectories("", "", SearchOption.AllDirectories);
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(dir, "TempStorageDirectoryTests"));
            Directory.CreateDirectory(Path.Combine(dir, "TempStorageDirectoryTests", "RealStorage"));
            Directory.CreateDirectory(Path.Combine(dir, "TempStorageDirectoryTests", "TempStorage"));
        }

        [Test]
        public void Search_Uses_Temp_Directory()
        {
            var dir = Path.Combine(CurrentAssemblyDirectory, "TestData");
            var realStorage = new DirectoryInfo(Path.Combine(dir, "TempStorageDirectoryTests", "RealStorage"));
            var tempStorage = new DirectoryInfo(Path.Combine(dir, "TempStorageDirectoryTests", "TempStorage"));

            using (var writer = new IndexWriter(
                new TempStorageDirectory(tempStorage, 
                    FSDirectory.Open(realStorage)),
                new StandardAnalyzer(Version.LUCENE_29), IndexWriter.MaxFieldLength.UNLIMITED))
            {

                var doc = new Document();
                doc.Add(new Field("testKey", "test Val", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES));
                writer.AddDocument(doc);
                writer.Commit();

                Assert.DoesNotThrow(() =>
                {
                    //search
                    using (var searcher = new IndexSearcher(
                        new TempStorageDirectory(tempStorage,
                        //the 'true' directory we'll set to throw errors - since the searcher should not use it!
                            new ErrorDirectory()),
                        true))
                    {
                        var parser = new QueryParser(Version.LUCENE_29, "contents", new StandardAnalyzer(Version.LUCENE_29));
                        var query = parser.Parse("testKey: test");
                        var hits = searcher.Search(query, 10);
                        Assert.AreEqual(1, hits.TotalHits);
                    }
                });

                
            }
        }

        [Test]
        public void Files_Written_To_Both_Folders()
        {
            var dir = Path.Combine(CurrentAssemblyDirectory, "TestData");
            var realStorage = new DirectoryInfo(Path.Combine(dir, "TempStorageDirectoryTests", "RealStorage"));
            var tempStorage = new DirectoryInfo(Path.Combine(dir, "TempStorageDirectoryTests", "TempStorage"));

            using (var writer = new IndexWriter(
                new TempStorageDirectory(tempStorage, FSDirectory.Open(realStorage)),
                new StandardAnalyzer(Version.LUCENE_29), IndexWriter.MaxFieldLength.UNLIMITED))
            {

                var doc = new Document();
                doc.Add(new Field("testKey", "testVal", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.YES));
                writer.AddDocument(doc);
                writer.Commit();
            }

            Assert.Greater(Directory.GetFiles(realStorage.FullName).Count(), 0);
            Assert.AreEqual(Directory.GetFiles(realStorage.FullName).Count(), Directory.GetFiles(tempStorage.FullName).Count());
        }

        private class ErrorDirectory : Lucene.Net.Store.Directory
        {
            public override string[] List()
            {
                return Enumerable.Empty<string>().ToArray();
            }

            public override bool FileExists(string name)
            {
                throw new NotImplementedException();
            }

            public override long FileModified(string name)
            {
                throw new NotImplementedException();
            }

            public override void TouchFile(string name)
            {
                throw new NotImplementedException();
            }

            public override void DeleteFile(string name)
            {
                throw new NotImplementedException();
            }

            public override void RenameFile(string @from, string to)
            {
                throw new NotImplementedException();
            }

            public override long FileLength(string name)
            {
                throw new NotImplementedException();
            }

            public override IndexOutput CreateOutput(string name)
            {
                throw new NotImplementedException();
            }

            public override IndexInput OpenInput(string name)
            {
                throw new NotImplementedException();
            }

            public override void Close()
            {
            }

            public override void Dispose()
            {
            }
        }
    }
}
