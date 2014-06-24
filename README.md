UmbracoExamine.TempStorage
==========================

Umbraco Examine providers that allow temporary storage of the index in the local CodeGen folder if the site is hosted on a network file share to avoid latency issues.

This is not to be used in any type of load balancing setup if you have syncStorage enabled otherwise you will run in to problems.

## Docs

The purpose of these providers is to work on an index in local temporary storage on the current machine. The resulting index paths will result in a path similar to:

> C:\Users\SomeGuy\AppData\Local\Temp\Temporary ASP.NET Files\root\56bd1092\483dcbb9\App_Data\TEMP\ExamineIndexes\InternalMember\

Update ExamineSettings.config:

Change the content indexers (i.e. InternalIndexer, ExternalIndexer) to be of type:

    UmbracoExamine.TempStorage.UmbracoTempStorageContentIndexer, UmbracoExamine.TempStorage
  
Change the member indexers (i.e. InternalMemberIndexer) to be of type:

    UmbracoExamine.TempStorage.UmbracoTempStorageMemberIndexer, UmbracoExamine.TempStorage
  
Change the searchers to be of type:

    UmbracoExamine.TempStorage.UmbracoTempStorageSearcher, UmbracoExamine.TempStorage
  
## Storage Syncing

Optionally you can add another config parameter to each indexer/searcher:

    syncStorage="true"
  
If this is enabled, the index will be persisted to the normal path specified in the config (i.e. ~/App_Data/TEMP/ExamineIndexes/Internal/) however, on startup the index will be copied to the local temp storage and all searches will operate from this location. When the index is written to, it will write to both local storage and the normal storage (they will be kept in sync). 
