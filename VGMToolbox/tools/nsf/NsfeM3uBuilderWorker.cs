﻿using System.ComponentModel;

using VGMToolbox.format.util;
using VGMToolbox.plugin;

namespace VGMToolbox.tools.nsf
{
    class NsfeM3uBuilderWorker : AVgmtWorker
    {
        public struct NsfeM3uBuilderStruct : IVgmtWorkerStruct
        {
            public bool OnePlaylistPerFile;

            private string[] sourcePaths;
            public string[] SourcePaths
            {
                get { return sourcePaths; }
                set { sourcePaths = value; }
            }
        }

        public NsfeM3uBuilderWorker() : base() { }

        protected override void doTaskForFile(string pPath, IVgmtWorkerStruct pNsfeM3uBuilderStruct, 
            DoWorkEventArgs e)
        {
            NsfeM3uBuilderStruct nsfeM3uBuilderStruct = (NsfeM3uBuilderStruct)pNsfeM3uBuilderStruct;
          
            NsfUtil.NsfM3uBuilderStruct nsfM3uBuilderStruct = new NsfUtil.NsfM3uBuilderStruct();
            nsfM3uBuilderStruct.OnePlaylistPerFile = nsfeM3uBuilderStruct.OnePlaylistPerFile;
            nsfM3uBuilderStruct.Path = pPath;
            NsfUtil.BuildM3uForFile(nsfM3uBuilderStruct);                            
        }                          
    }
}
