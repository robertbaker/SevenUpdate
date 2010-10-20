﻿// ***********************************************************************
// <copyright file="Utility.cs"
//            project="SharpBits.Base"
//            assembly="SharpBits.Base"
//            solution="SevenUpdate"
//            company="Xidar Solutions">
//     Copyright (c) xidar solutions. All rights reserved.
// </copyright>
// <author username="xidar">xidar</author>
// <author username="sevenalive">Robert Baker</author>
// <license href="http://sharpbits.codeplex.com/license">BSD License</license> 
// ***********************************************************************
namespace SharpBits.Base
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    /// <summary>Various utility methods.</summary>
    internal static class Utilities
    {
        #region Properties

        /// <summary>Gets the bits version.</summary>
        /// <value>The bits version.</value>
        internal static BitsVersion BitsVersion
        {
            get
            {
                try
                {
                    var bits = FileVersionInfo.GetVersionInfo(Path.Combine(Environment.SystemDirectory, @"qmgr.dll"));



                    switch (bits.FileMajorPart)
                    {
                        case 6:
                            switch (bits.FileMinorPart)
                            {
                                case 0:
                                    return BitsVersion.Bits1;
                                case 2:
                                    return BitsVersion.Bits1Dot2;
                                case 5:
                                    return BitsVersion.Bits1Dot5;
                                case 6:
                                    return BitsVersion.Bits2;
                                case 7:
                                    return BitsVersion.Bits2Dot5;
                                default:
                                    return BitsVersion.BitsUndefined;
                            }

                        case 7:
                            return BitsVersion.Bits3;
                        default:
                            return BitsVersion.BitsUndefined;
                    }
                }
                catch (Exception)
                {
                    throw;
                    return BitsVersion.BitsUndefined;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>Converts a <see cref="DateTime"/> to <see cref="FileTime"/></summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns>The converted <see cref="DateTime"/> as a <see cref="FileTime"/></returns>
        internal static FileTime DateTimeToFileTime(DateTime dateTime)
        {
            long fileTime = 0;
            if (dateTime != DateTime.MinValue)
            {
                // Checking for MinValue
                fileTime = dateTime.ToFileTime();
            }

            var resultingFileTime = new FileTime
                {
                    DWLowDateTime = (uint)(fileTime & 0xFFFFFFFF),
                    DWHighDateTime = (uint)(fileTime >> 32)
                };
            return resultingFileTime;
        }

        /// <summary>Converts the <see cref="FileTime"/> to <see cref="DateTime"/></summary>
        /// <param name="fileTime">The file time.</param>
        /// <returns>The converted <see cref="FileTime"/> to <see cref="DateTime"/></returns>
        internal static DateTime FileTimeToDateTime(FileTime fileTime)
        {
            if (fileTime.DWHighDateTime == 0 && fileTime.DWLowDateTime == 0)
            {
                // Checking for MinValue
                return DateTime.MinValue;
            }

            var dateTime = (((long)fileTime.DWHighDateTime) << 32) + fileTime.DWLowDateTime;
            return DateTime.FromFileTime(dateTime);
        }

        /// <summary>Gets the name from a SID</summary>
        /// <param name="sid">The SID as a string</param>
        /// <returns>The name from the SID</returns>
        internal static string GetName(string sid)
        {
            long userNameSize = 255;
            long domainNameSize = 255;
            var pointerSid = new IntPtr(0);
            var use = 0;
            var userName = new StringBuilder(255);
            var domainName = new StringBuilder(255);
            if (NativeMethods.ConvertStringSidToSidW(sid, ref pointerSid))
            {
                if (NativeMethods.LookupAccountSidW(string.Empty, pointerSid, userName, ref userNameSize, domainName, ref domainNameSize, ref use))
                {
                    return string.Concat(domainName.ToString(), "\\", userName.ToString());
                }
            }

            return string.Empty;
        }

        #endregion
    }
}