#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

#endregion

namespace SevenUpdate.Sdk
{
    public sealed class RegistryParser
    {
        // Event system

        #region Delegates

        public delegate void WriteOutputDelegate(string output);

        #endregion

        public const string Version = "0.45";

        // Constants
        private const string RegV4Signature = "REGEDIT4\r\n";
        private const string RegV5Signature = "Windows Registry Editor Version 5.00\r\n";
        private Collection<RegistryItem> RegItem = new Collection<RegistryItem>();

        #region Static Declarations

        // Let the games Begin !!!!! Init static RegExs for optimization
        private static readonly Regex RegexRootKey = new Regex(@"^\[-?(?<RootKey>(HKEY_LOCAL_MACHINE|HKEY_CURRENT_USER|HKEY_CLASSES_ROOT|HKEY_USERS|HKLM|HKCU|HKCR|HKU))",
                                                               RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        private static readonly Regex RegexSubKey = new Regex(@"^\[-?(HKEY_LOCAL_MACHINE|HKEY_CURRENT_USER|HKEY_CLASSES_ROOT|HKEY_USERS|HKLM|HKCU|HKCR|HKU)\\(?<Subkey>.*)\]",
                                                              RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        private static readonly Regex LineSplitter = new Regex(@"^[^\r\n\v\t]*[\t\x20]*=[\t\x20]*((\\[\x20\t]*\s*)|[^\r\n])*",
                                                               RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture);

        private static Regex regexCommentData = new Regex(@"^\s*;(\s*.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        private static readonly Regex RegexStringValueMatcher = new Regex(@"^(@|""(?<Value>.*)"")\s*=\s*\""(?<Data>.*)""([\x20\s]*;+[^\r\n]*)?",
                                                                          RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        private static readonly Regex RegexOtherValueMatcher = new Regex(@"^(@|""(?<Value>.*)"")\s*=\s*(?<Data>[^"";]*)([\x20\s]*;+[^\r\n]*)?",
                                                                         RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        private static readonly Encoding UDecoder = Encoding.Unicode;
        private static readonly Encoding ADecoder = Encoding.Default;
        private static readonly string SplitToken = @"_!Split" + new Random().Next(99) + "!_";
        private static int regVersionSignature;

        /// <summary>
        ///   Method for converting the hex bytestring to a byte value
        /// </summary>
        private static byte String2Byte(string byteString)
        {
            return byte.Parse(byteString, NumberStyles.HexNumber);
        }

        /// <summary>
        ///   Method for converting the hex bytestring to a byte value + a check
        ///   that converts all above ASCII bytes to ?.
        /// </summary>
        private static byte String2ByteForAscii(string byteString)
        {
            var b = byte.Parse(byteString, NumberStyles.HexNumber);
            if (b < 32 || b > 127)
                return 63;
            return b;
        }

        /// <summary>
        ///   Method for converting the hex bytestring to a byte value + a check
        ///   that converts all above ASCII bytes to ? but allows CRLF characters.
        /// </summary>
        private static byte String2ByteForAsciiAllowCrlf(string byteString)
        {
            var b = byte.Parse(byteString, NumberStyles.HexNumber);
            if (b == 13 || b == 10)
                return b;
            if (b < 32 || b > 127)
                return 63;
            return b;
        }

        /// <summary>
        ///   Method for finding empty strings
        /// </summary>
        private static bool EmptyString(string item)
        {
            return item.Length == 0;
        }

        #endregion

        #region Public Method

        /// <summary>
        ///   Main method for parsing a reg file
        /// </summary>
        /// <returns>String of lines to be returned</returns>
        public Collection<RegistryItem> Parse(string file)
        {
            if (!File.Exists(file))
                return null;

            // Raw Data
            Encoding encoder = Encoding.GetEncoding(0);
            var fileStream = new StreamReader(file, encoder, true);
            string rawRegFileData = fileStream.ReadToEnd();

            //
            // Nuke all comments (these have given me a royal headache 
            // to try to workaround but it is unfortunately the ONLY way I can do the
            // conversion correctly, the comments confuse the Regular Expression filters)
            //
            // Also the way Reg2Inf processes the lines makes it impossible to know which
            // comment goes where esp. after addition of DelReg support.
            //
            rawRegFileData = Regex.Replace(rawRegFileData, @"^\s*;\s*.*", "\r\n", RegexOptions.Multiline | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

            // Split Data into Array, where each element is a complete registry block
            rawRegFileData = RegexRootKey.Replace(rawRegFileData, SplitToken + @"$0");
            string[] dataArray = Regex.Split(rawRegFileData, SplitToken, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            // Free memory (this method takes long to end and GC may not run wasting memory)
            GC.Collect();

            // Check for Windows REG file signature to determine how to
            // process hex(2) and hex(7) (DBCS or SBCS) and remove from the array
            if (dataArray[0].StartsWith(RegV5Signature, StringComparison.OrdinalIgnoreCase))
            {
                regVersionSignature = 5;
                dataArray[0] = dataArray[0].Remove(0, RegV5Signature.Length);
            }
            else if (dataArray[0].StartsWith(RegV4Signature, StringComparison.OrdinalIgnoreCase))
            {
                regVersionSignature = 4;
                dataArray[0] = dataArray[0].Remove(0, RegV4Signature.Length);
            }
            else
                return null;


            for (int x = 0; x < dataArray.Length; x++)
                ProcessRegBlock(dataArray[x]);

            return RegItem;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///   Internal method for processing extracted REG format blocks. Real processing takes place here.
        /// </summary>
        /// <param name = "regBlock">Reg Block</param>
        /// <returns>Object containing AddReg or DelReg INF line equivalent to block</returns>
        private void ProcessRegBlock(string regBlock)
        {
            // Define variable for RootKey
            string infRootKey = "_unknown";

            // Extract Root Key
            switch (RegexRootKey.Match(regBlock).Groups["RootKey"].Value.ToLower())
            {
                case "hkey_local_machine":
                    infRootKey = "HKLM";
                    break;

                case "hkey_current_user":
                    infRootKey = "HKCU";
                    break;

                case "hkey_classes_root":
                    infRootKey = "HKCR";
                    break;

                case "hkey_users":
                    infRootKey = "HKU";
                    break;

                case "hklm":
                    infRootKey = "HKLM";
                    break;

                case "hkcu":
                    infRootKey = "HKCU";
                    break;

                case "hkcr":
                    infRootKey = "HKCR";
                    break;

                case "hku":
                    infRootKey = "HKU";
                    break;
            }
            infRootKey = infRootKey + @"\";
            // Drop line if no valid rootkey found (This will drop all comments too)
            if (infRootKey == "_unknown")
                return;

            // Extract SubKey
            string rawSubKeyName = RegexSubKey.Match(regBlock).Groups["Subkey"].Value;
            if (rawSubKeyName.Length == 0)
                return;
            // rawSubKeyName
            string infSubKeyValue = ApplyFixes(rawSubKeyName, true);

            // Check for removal of RegBlock - [-...
            if (Regex.IsMatch(regBlock, @"^\[-HK", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture))
            {
                // Then return data to DelReg instead of AddReg property of INFConversionResult instance

                var regBlockResult = new RegistryItem {Key = infRootKey + infSubKeyValue, Action = RegistryAction.DeleteKey};
                RegItem.Add(regBlockResult);
                return;
            }

            // Put RegBlock header out of the way to process lines (Remove header)
            regBlock = Regex.Replace(regBlock, RegexSubKey + @"\r\n", @"", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Multiline);

            // CleanUp RegBlock from extra carriage returns
            regBlock = regBlock.Trim();

            // Check for empty RegBlock (will happen if subkey is empty)
            if (String.IsNullOrEmpty(regBlock))
                return;
            // Filter bad regex
            var badLinesMatches = Regex.Match(regBlock, @"\\\\n\r\r\n", RegexOptions.Singleline);
            if (badLinesMatches.Success)
                regBlock = regBlock.Replace("\\\\n\r\r\n", @"\r\n ");

            // Split block into appropriate lines by adding marker after each line
            regBlock = LineSplitter.Replace(regBlock, "$0" + SplitToken);
            // Do the actual splitting and cleanup
            var regLines = new List<string>();
            regLines.AddRange(Regex.Split(regBlock, SplitToken + @"\s*", RegexOptions.Multiline | RegexOptions.ExplicitCapture));

            // Internal check for splitter validity
            // The last entry of the array MUST be empty, otherwise
            // something didn't match and that can never be a comment 
            // because we strip them in an earlier stage of processing.
            if (regLines[regLines.Count - 1].Length != 0)
                return;

            // Force empty RegBlock now (since this block takes long to execute and
            // GC may not collect RegBlock until else block ends, wasting memory
            GC.Collect();

            // Pass RegLines to method for figuring out the FLAGS, VALUENAME and VALUEDATA
            for (int x = 0; x < regLines.Count; x++)
            {
                var regBlockResult = ProcessRegLine(regLines[x]);
                regBlockResult.Key = infRootKey + infSubKeyValue;
                if (regBlockResult.Data == null && regBlockResult.KeyValue == null)
                    continue;
                RegItem.Add(regBlockResult);
            }
        }

        /// <summary>
        ///   Internal method for extracting the data part of the Reg line
        /// </summary>
        /// <param name = "line">Reg Line</param>
        /// <returns>Object containing AddReg or DelReg INF format partial lines for further processing.</returns>
        private RegistryItem ProcessRegLine(string line)
        {
            // Create new INFConversionResult to hold result of this method
            var methodResult = new RegistryItem();

            // Return empty INFConversionResult instance if Line is empty
            if (String.IsNullOrEmpty(line))
                return methodResult;

            // ValueNameData string definition
            // ValueData string definition
            // Is ValueData string ?
            bool valueDataIsString = false;

            // Define Match object that will test all criteria
            Match criteriaMatch = RegexStringValueMatcher.Match(line);
            if (!criteriaMatch.Success)
                criteriaMatch = RegexOtherValueMatcher.Match(line);
            else
                valueDataIsString = true;
            if (!criteriaMatch.Success)
                return methodResult;

            // Set the value name (blank if default, else valuename)
            string valueName = criteriaMatch.Groups["Value"].Value;

            // Apply fixes to value name data regardless of value data
            valueName = ApplyFixes(valueName, false);

            // Set the value data (string or otherwise, this will be checked later)
            string valueData = criteriaMatch.Groups["Data"].Value;

            if (valueDataIsString)
            {
                // Apply fixes
                valueData = ApplyFixes(valueData, false);
                methodResult.Action = RegistryAction.Add;
                methodResult.Data = valueData;
                methodResult.KeyValue = valueName;
                methodResult.ValueKind = RegistryValueKind.String;
                return methodResult;
            }

            // Fix ValueData - Remove SPACE / TAB / CR / LF from beginning and end
            valueData = valueData.Trim();

            // If ValueData is equal to -, this means that this is a removal instruction
            // and the line should be added to the DelReg part of the INFConversionResult instance
            if (String.Compare(valueData, "-") == 0)
            {
                methodResult.Data = null;
                methodResult.Action = RegistryAction.DeleteValue;
                methodResult.KeyValue = valueName;
                return methodResult;
            }

            // If ValueData is still empty, that means a normal string value with its
            // value explicitly set to blank
            if (String.IsNullOrEmpty(valueData))
            {
                methodResult.KeyValue = valueName;
                methodResult.Data = valueData;
                methodResult.ValueKind = RegistryValueKind.String;
                return methodResult;
            }
            // Test for the aforementioned possibilities

            // Binary: [hex:] | [hex(3):] - No reverse of order needed
            if (Regex.IsMatch(valueData, @"^hex(\(0*3\))?:(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
            {
                // Put everything on one line
                valueData = PutOnOneLineAndTrim(valueData);
                // Remove hex: or hex(3): at the beginning
                valueData = Regex.Replace(valueData, @"^hex(\(0*3\))?:", String.Empty, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
                methodResult.ValueKind = RegistryValueKind.Binary;
                methodResult.Data = valueData;
                methodResult.KeyValue = valueName;
                // Return Partial Line
                return methodResult;
            }

            // Dword: | [hex(4):]AABBCCDDEEFF (no reverse) | [hex(4):]FF,EE,DD,CC,BB,AA (reverse)
            if (Regex.IsMatch(valueData, @"^(dword:([0-9A-Fa-f]){1,8})|(hex\(0*4\):([0-9A-Fa-f]{1,2},?)+)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline))
            {
                // Remove dword: at the beginning
                valueData = Regex.Replace(valueData, @"^(dword|hex\(0*4\)):", String.Empty, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
                // Check for empty ValueData
                if (String.IsNullOrEmpty(valueData.Trim()))
                {
                    methodResult.ValueKind = RegistryValueKind.DWord;
                    methodResult.Data = valueData;
                    methodResult.KeyValue = valueName;
                    return methodResult;
                }
                // In case of hex(4) notation, very little changes are needed - no
                // reverse needed as in hex(4) they are already revered and in the correct format for INF
                if (Regex.IsMatch(valueData, @"^([0-9A-Fa-f]{1,2},)+[0-9A-Fa-f]{1,2}$", RegexOptions.ExplicitCapture))
                    valueData = valueData.TrimEnd(new[] {','});
                else
                {
                    // Check if length is equal to 8, else pad with 0s to 8
                    if (valueData.Length < 8)
                    {
                        int numberOfZeroesNeeded = 8 - valueData.Length;
                        for (int i = 0; i < numberOfZeroesNeeded; i++)
                            valueData = "0" + valueData;
                    }
                    // Put a comma after each 2 digits
                    var digitsSeparated = new StringBuilder();
                    MatchCollection splitThem = Regex.Matches(valueData, @"(([0-9]|[A-F]){2})", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.RightToLeft);
                    digitsSeparated.Append(splitThem[0].Value + ",");
                    digitsSeparated.Append(splitThem[1].Value + ",");
                    digitsSeparated.Append(splitThem[2].Value + ",");
                    digitsSeparated.Append(splitThem[3].Value);
                    valueData = digitsSeparated.ToString();
                }
                methodResult.ValueKind = RegistryValueKind.DWord;
                methodResult.Data = valueData;
                methodResult.KeyValue = valueName;
                return methodResult;
            }

            // hex(2):  |  REG_EXPAND_SZ
            if (Regex.IsMatch(valueData, @"^hex\(0*2\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
            {
                // Put everything on one line
                valueData = PutOnOneLineAndTrim(valueData);
                // Remove hex(2): at the beginning
                valueData = Regex.Replace(valueData, @"^hex\(0*2\):", String.Empty, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
                // Remove trailing 00,00s (v5) or 00s (v4)
                valueData = Regex.Replace(valueData, @",00,00$", String.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

                // Check for empty ValueData
                if (String.IsNullOrEmpty(valueData.Trim()))
                {
                    methodResult.ValueKind = RegistryValueKind.ExpandString;
                    methodResult.Data = valueData;
                    methodResult.KeyValue = valueName;
                    return methodResult;
                }
                // Get the string, with UnicodeEncoding if v5 else ASCIIEncoding
                if (regVersionSignature == 5)
                {
                    // Create an array to hold the hex values
                    var temporaryArray = new List<string>(valueData.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));

                    // Treat as DBCS (Double byte characters)
                    List<byte> byteArray = temporaryArray.ConvertAll(String2Byte);
                    valueData = UDecoder.GetString(byteArray.ToArray());
                }
                else
                {
                    // Nuke all 00s to prevent conversion to null, if this line causes
                    // changes ValueData then its 99% possible that the wrong REG signature
                    // is present in the REG file being processed.
                    valueData = Regex.Replace(valueData, @"00,?", String.Empty);

                    // Create an array to hold the hex values
                    var temporaryArray = new List<string>(valueData.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));

                    // Treat as SBCS (Single byte characters)
                    List<byte> byteArray = temporaryArray.ConvertAll(String2ByteForAscii);
                    valueData = ADecoder.GetString(byteArray.ToArray());
                }
                // Apply Fixes
                valueData = ApplyFixes(valueData, true);
                // Set Flag

                // Return Partial Line
                methodResult.ValueKind = RegistryValueKind.ExpandString;
                methodResult.Data = valueData;
                methodResult.KeyValue = valueName;
                return methodResult;
            }

            // hex(7):  |  REG_MULTI_SZ
            if (Regex.IsMatch(valueData, @"^hex\(0*7\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
            {
                // Put everything on one line
                valueData = PutOnOneLineAndTrim(valueData);
                // Remove hex(7): at the beginning
                valueData = Regex.Replace(valueData, @"^hex\(0*7\):", String.Empty, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
                // Remove trailing 00,00s is now done by the RegEx below the following block

                // Check for empty ValueData
                if (String.IsNullOrEmpty(valueData.Trim()))
                {
                    // Set Flag - Undocumented but used inside XP Setup's own files to specify 
                    // REG_MULTI_SZ. The documented method seems to be to use 0x10000
                    // Return Partial Line
                    methodResult.Data = valueName;
                    methodResult.ValueKind = RegistryValueKind.MultiString;
                    return methodResult;
                }

                // Create a List<string> for holding the split parts
                var multiStringEntries = new List<string>(5);

                // Create a StringBuilder for holding the semi-processed string
                var valueDataBuilder = new StringBuilder(valueData.Length);

                // Convert the bytes back to the string using the new
                // UnicodeEncoding method for v5 signature (DBCS) and using
                // the old method for v4 signature which uses SBCS.
                if (regVersionSignature == 5)
                {
                    // RegEx match all pairs of bytes
                    MatchCollection readinTwos = Regex.Matches(valueData, @"[a-zA-Z0-9]{2},[a-zA-Z0-9]{2}", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                    const string stringTerminator = "00,00";
                    foreach (Match found in readinTwos)
                    {
                        if (String.Compare(found.Value, stringTerminator) != 0)
                        {
                            var z = new List<string>(found.Value.Split(new[] {','}));
                            List<byte> y = z.ConvertAll(String2Byte);
                            valueDataBuilder.Append(UDecoder.GetString(y.ToArray()));
                        }
                        else
                            valueDataBuilder.Append("\r\n");
                    }
                }
                else
                {
                    // Use old behaviour - invalid and non-printable chars will convert to ? (63)
                    // Convert 00 to a carriage return / line-feed (CR-LF): 0d 0a
                    valueData = Regex.Replace(valueData, @"00", @"0d,0a", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                    // Convert to byte and back to characters using ASCIIEncoding
                    var z = new List<string>(valueData.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));
                    List<byte> y = z.ConvertAll(String2ByteForAsciiAllowCrlf);
                    valueDataBuilder.Append(ADecoder.GetString(y.ToArray()));
                }

                multiStringEntries.AddRange(Regex.Split(valueDataBuilder.ToString(), @"\r\n", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture));
                // multiStringEntries.RemoveAt((multiStringEntries.Count-1));
                multiStringEntries.RemoveAll(EmptyString);

                // Reinit StringBuilder to clear
                valueDataBuilder = new StringBuilder(valueData.Length);

                for (int i = 0; i < multiStringEntries.Count; i++)
                {
                    // Apply Fixes
                    multiStringEntries[i] = ApplyFixes(multiStringEntries[i], true);
                    // Append to StringBuilder
                    if ((i + 1) == multiStringEntries.Count)
                        valueDataBuilder.Append("\"" + multiStringEntries[i] + "\"");
                    else
                        valueDataBuilder.Append("\"" + multiStringEntries[i] + "\",");
                }

                // Set ValueData
                valueData = valueDataBuilder.ToString();
                // Set Flag - REG_MULTI_SZ overwrite
                methodResult.ValueKind = RegistryValueKind.MultiString;
                methodResult.Data = valueData;
                methodResult.KeyValue = valueName;
                return methodResult;
            }

            // hex(1):  |  REG_SZ expressed in Hex notation
            if (Regex.IsMatch(valueData, @"^hex\(0*1\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
            {
                // Put everything on one line
                valueData = PutOnOneLineAndTrim(valueData);
                // Remove hex(1): at the beginning
                valueData = Regex.Replace(valueData, @"^hex\(0*1\):", String.Empty, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
                // Remove trailing 00,00s
                valueData = Regex.Replace(valueData, @",00,00$", String.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

                // Check for empty ValueData
                if (String.IsNullOrEmpty(valueData.Trim()))
                {
                    methodResult.ValueKind = RegistryValueKind.String;
                    methodResult.Data = valueData;
                    methodResult.KeyValue = valueName;
                    return methodResult;
                }
                // Get the string with UnicodeEncoding
                // Create an array to hold the hex values
                var temporaryArray = new List<string>(valueData.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries));

                // Treat as DBCS (Double byte characters)
                List<byte> byteArray = temporaryArray.ConvertAll(String2Byte);
                valueData = UDecoder.GetString(byteArray.ToArray());
                // Apply Fixes
                valueData = ApplyFixes(valueData, true);

                // Look for CR + LF and append warning if found
                if (valueData.Contains("\r\n"))
                    valueData = valueData.Replace("\r\n", "_");
                methodResult.ValueKind = RegistryValueKind.String;
                methodResult.Data = valueData;
                methodResult.KeyValue = valueName;
                return methodResult;
            }

            // hex(a):  | REG_RESOURCE_REQUIRED
            if (Regex.IsMatch(valueData, @"^hex\(0*a\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
                return ProcessBinaryType('a', ref valueName, ref valueData, RegistryValueKind.None, ref methodResult);

            // hex(b):  | REG_QWORD
            if (Regex.IsMatch(valueData, @"^hex\(0*b\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline))
                return ProcessBinaryType('b', ref valueName, ref valueData, RegistryValueKind.QWord, ref methodResult);

            // hex(8):  |  REG_RESOURCE_LIST
            if (Regex.IsMatch(valueData, @"^hex\(0*8\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
                return ProcessBinaryType('8', ref valueName, ref valueData, RegistryValueKind.None, ref methodResult);

            // hex(9):  |  REG_FULL_RESOURCE_DESCRIPTORS
            if (Regex.IsMatch(valueData, @"^hex\(0*9\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
                return ProcessBinaryType('9', ref valueName, ref valueData, RegistryValueKind.None, ref methodResult);

            // hex(5):  |  REG_DWORD_BIG_ENDIAN
            if (Regex.IsMatch(valueData, @"^hex\(0*5\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
                return ProcessBinaryType('5', ref valueName, ref valueData, RegistryValueKind.DWord, ref methodResult);

            // hex(6):  |  REG_LINK
            if (Regex.IsMatch(valueData, @"^hex\(0*6\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
                return ProcessBinaryType('6', ref valueName, ref valueData, RegistryValueKind.None, ref methodResult);

            // hex(0):  |  REG_NONE
            if (Regex.IsMatch(valueData, @"^hex\(0*0\):(([0-9|A-F]{2}),?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Multiline))
                return ProcessBinaryType('0', ref valueName, ref valueData, RegistryValueKind.None, ref methodResult);

            // Fallback in case nothing matches
            return methodResult;
        }

        #region Helper methods

        /// <summary>
        ///   Common processing for normal binary types
        /// </summary>
        /// <param name = "hexType">Single char for hex type eg 'b' in hex(b)</param>
        /// <param name = "valueNameData">Value Name for generating INF format line</param>
        /// <param name = "valueData">ValueData to operate on</param>
        /// <param name = "flag">INF flag for this binary type</param>
        /// <param name = "methodResult">Instance to return result in</param>
        /// <returns>Finished INFConversionResult instance</returns>
        private static RegistryItem ProcessBinaryType(char hexType, ref string valueNameData, ref string valueData, RegistryValueKind flag, ref RegistryItem methodResult)
        {
            // Put everything on one line
            valueData = PutOnOneLineAndTrim(valueData);
            // Remove hex: at the beginning
            valueData = Regex.Replace(valueData, @"^hex\(0*" + hexType + @"\):", String.Empty, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline);
            methodResult.ValueKind = flag;
            // Return Partial Line
            methodResult.Data = valueData;
            return methodResult;
        }

        /// <summary>
        ///   Puts ValueData on single line and removes from the beginning and end the following:
        ///   space, tab, CR, LF, extra commas
        /// </summary>
        /// <param name = "valueData">ValueData string to operate on</param>
        /// <returns>Cleaned up and fixed string</returns>
        private static string PutOnOneLineAndTrim(string valueData)
        {
            return Regex.Replace(valueData, @",\\\r\n\s*", @",", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture).Trim(new[] {'\r', '\n', '\x20', '\t', ','});
        }

        /// <summary>
        ///   Apply fixes (\\ > \)|(\" > "")|(%% > %)
        /// </summary>
        /// <param name = "line">Line to apply fixes to</param>
        /// <param name = "skipQuotesConversion">REG_MULTI_SZ, REG_SZ (hex notation) and REG_EXPAND_SZ 
        ///   already put the quotes correctly so if you fix them again you will damage the value, so for 
        ///   those put true here to skip that part and only to do the double-quote and percent fixes.</param>
        /// <returns>New Data</returns>
        private static string ApplyFixes(string line, bool skipQuotesConversion)
        {
            if (!skipQuotesConversion)
            {
                // Change Double backslashes to only one
                line = Regex.Replace(line, @"\\\\", @"\", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ExplicitCapture);

                // Change escaped quotes to single quote
                line = Regex.Replace(line, @"\\""", @"""", RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            }

            // Change single quote to double quote
            line = Regex.Replace(line, @"""", @"""""", RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

            // Code to fix the percentages
            line = Regex.Replace(line, @"%", @"%%", RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return line;
        }

        #endregion

        #endregion
    }
}