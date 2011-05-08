// ***********************************************************************
// <copyright file="BGJobProgress.cs" project="SharpBits.Base" assembly="SharpBits.Base" solution="SevenUpdate" company="Xidar Solutions">
//     Copyright (c) xidar solutions. All rights reserved.
// </copyright>
// <author username="xidar">xidar</author>
// <author username="sevenalive">Robert Baker</author>
// <license href="http://sharpbits.codeplex.com/license">BSD License</license> 
// ***********************************************************************

namespace SharpBits.Base
{
    using System.Runtime.InteropServices;

    /// <summary>The BG_JOB_PROGRESS structure provides job-related progress information, such as the number of bytes and files transferred.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 0)]
    internal struct BGJobProgress
    {
        /// <summary>Total number of bytes to transfer for the job.</summary>
        public readonly ulong BytesTotal;

        /// <summary>Number of bytes transferred.</summary>
        public readonly ulong BytesTransferred;

        /// <summary>Total number of files to transfer for this job.</summary>
        public readonly uint FilesTotal;

        /// <summary>Number of files transferred.</summary>
        public readonly uint FilesTransferred;
    }
}