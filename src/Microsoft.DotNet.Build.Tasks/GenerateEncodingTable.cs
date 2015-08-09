﻿using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GenerateEncodingTable : Task
    {
        private const string CommentIndicator = "#";
        private const char FieldDelimiter = ';';

        [Required]
        public string IANAMappings { get; set; }

        [Required]
        public string PreferedIANANames { get; set; }

        [Required]
        public string OutputDataTable { get; set; }

        public string Namespace { get; set; }
        public string ClassName { get; set; }

        public override bool Execute()
        {
            Dictionary<string, ushort> nameMappings = ParseNameMappings(IANAMappings);
            Dictionary<ushort, KeyValuePair<string, string>> preferredNames = ParsePreferredNames(PreferedIANANames);

            return true;
        }

        private Dictionary<string, ushort> ParseNameMappings(string path)
        {
            Dictionary<string, ushort> mapping = new Dictionary<string, ushort>();

            foreach (var line in DelimitedFileRows(path, 2))
            {
                string name = line.Value[0].Trim().ToLowerInvariant();

                if (name != line.Value[0])
                {
                    Log.LogWarning("Code page name in file {0} at line {1} has whitespace or upper-case characters.  Was: ->{2}<-, Using ->{3}<-", path, line.Key, line.Value[0], name);
                }

                ushort codepage;
                if (!ushort.TryParse(line.Value[1], out codepage))
                {
                    Log.LogError("Code page in file {0} at line {1} is not valid, expecting numeric entry in range [" + ushort.MinValue + ", " + ushort.MaxValue + "]  Was: ->{2}<-", path, line.Key, line.Value[1]);
                    continue;
                }

                ushort existing;
                if (mapping.TryGetValue(name, out existing))
                {
                    if (existing == codepage)
                    {
                        Log.LogWarning("Code page mapping {0} to {1} in file {2} at line {3} is a duplicate entry, and can be removed.", name, codepage, path, line.Key);
                    }
                    else
                    {
                        Log.LogError("Code page name {0} in file {1} at line {2} is mapped to multiple code pages; new is {3}, old was {4}", name, path, line.Key, name, codepage, existing);
                    }
                }
                else
                {
                    mapping[name] = codepage;
                }
            }

            return mapping;
        }

        private Dictionary<ushort, KeyValuePair<string, string>> ParsePreferredNames(string path)
        {
            Dictionary<ushort, KeyValuePair<string, string>> preferredNames = new Dictionary<ushort, KeyValuePair<string, string>>();

            foreach (var line in DelimitedFileRows(path, 3))
            {
                ushort codepage;
                if (!ushort.TryParse(line.Value[0], out codepage))
                {
                    Log.LogError("Code page in file {0} at line {1} is not valid, expecting numeric entry in range [" + ushort.MinValue + ", " + ushort.MaxValue + "]  Was: ->{2}<-", path, line.Key, line.Value[0]);
                    continue;
                }

                string name = line.Value[1].Trim().ToLowerInvariant();
                if (name != line.Value[1])
                {
                    Log.LogWarning("Code page name in file {0} at line {1} has whitespace or upper-case characters.  Was: ->{2}<-, Using ->{3}<-", path, line.Key, line.Value[1], name);
                }

                string englishName = line.Value[2].Trim();
                if (englishName != line.Value[2])
                {
                    Log.LogWarning("English name in file {0} at line {1} has whitespace.  Was: ->{2}<-, Using ->{3}<-", path, line.Key, line.Value[2], englishName);
                }

                KeyValuePair<string, string> names = KeyValuePair.Create(name, englishName);

                KeyValuePair<string, string> existing;
                if (preferredNames.TryGetValue(codepage, out existing))
                {
                    if (names.Equals(existing))
                    {
                        Log.LogWarning("Code page names {0} for code page {1} in file {2} at line {3} is a duplicate entry, and can be removed.", names, codepage, path, line.Key);
                    }
                    else
                    {
                        Log.LogError("Code page {0} in file {1} at line {2} is mapped to multiple names; new is {3}, old was {4}", codepage, path, line.Key, names, existing);
                    }
                }
                else
                {
                    preferredNames[codepage] = names;
                }
            }

            return preferredNames;
        }

        private IEnumerable<KeyValuePair<int, string[]>> DelimitedFileRows(string path, int columns = 0)
        {
            using (var input = new StreamReader(path))
            {
                int lineNumber = 1;
                string line;

                for (; (line = input.ReadLine()) != null; ++lineNumber)
                {
                    if (line.StartsWith(CommentIndicator) || string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] values = line.Split(FieldDelimiter);

                    if (columns > 0 && values.Length != columns)
                    {
                        Log.LogError("Parsing mapping in file {0}, line {1}.  Expected {2} fields, saw {3}: {4}", path, lineNumber, columns, values.Length, line);
                    }

                    yield return KeyValuePair.Create(lineNumber, line.Split(FieldDelimiter));
                }
            }
        }

        private const string Header =
@"// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// THIS IS AN AUTOGENERATED FILE
// IT IS GENERATED FROM A BUILDTOOLS TASK
//

namespace {0}
{{
    internal static partial class {1}
    {{
";

        // The format is:
        //     0 - IANA name
        //     1 - codepage
        // Ordered by alphabetized IANA name
        private const string EncodingNames =
@"
        // s_encodingNames is the concatenation of all supported IANA names for each codepage.
        // This is done rather than using a large readonly array of strings to avoid
        // generating a large amount of code in the static constructor.
        // Using indices from s_encodingNamesIndices, we binary search this string when mapping
        // an encoding name to a codepage. Note that these names are all lowercase and are
        // sorted alphabetically.
        private const string s_encodingNames =|
            ""{0}"" + // {1:D}|
            """";
";

        // The format is:
        //     0 - IANA name
        //     1 - codepage
        //     2 - Start index of encoding name
        // The layout is to properly populate the end value
        // Ordered by alphabetized IANA name
        private const string EncodingNameIndices =
@"
        // s_encodingNameIndices contains the start index of every encoding name in the string
        // s_encodingNames. We infer the length of each string by looking at the start index
        // of the next string.
        private static readonly int[] s_encodingNameIndices = new int[]
        {
            0|, // {0} ({1:D})
            {2:D}|
        };
";

        // The format is:
        //     0 - codepage
        //     1 - IANA name
        // Ordered by alphabetized IANA name
        private const string CodePagesByName =
@"
        // s_codePagesByName contains the list of supported codepages which match the encoding
        // names listed in s_encodingNames. The way mapping works is we binary search
        // s_encodingNames using s_encodingNamesIndices until we find a match for a given name.
        // The index of the entry in s_encodingNamesIndices will be the index of codepage in s_codePagesByName.
        private static readonly UInt16[] s_codePagesByName = new UInt16[]
        {|
            {0:D}, // {1}|
        };
";

        // The format is:
        //     0 - codepage
        //     1 - IANA name
        // Ordered by codepage
        private const string MappedCodePages =
@"
        // When retrieving the value for System.Text.Encoding.WebName or
        // System.Text.Encoding.EncodingName given System.Text.Encoding.CodePage,
        // we perform a linear search on s_mappedCodePages to find the index of the
        // given codepage. This is used to index WebNameIndices to get the start
        // index of the web name in the string WebNames, and to index
        // s_englishNameIndices to get the start of the English name in s_englishNames.
        private static readonly UInt16[] s_mappedCodePages = new UInt16[]
        {|
            {0:D}, // {1}|
        };
";

        // The format is:
        //     0 - IANA name
        //     1 - codepage
        // Ordered by codepage
        private const string WebNames =
@"
        // s_webNames is a concatenation of the default encoding names
        // for each code page. It is used in retrieving the value for
        // System.Text.Encoding.WebName given System.Text.Encoding.CodePage.
        // This is done rather than using a large readonly array of strings to avoid
        // generating a large amount of code in the static constructor.
        private const string s_webNames =|
            ""{0}"" + // {1:D}|
            """";
";

        // The format is:
        //     0 - IANA name
        //     1 - codepage
        //     2 - Start index of (default) web name
        // The layout is to properly populate the end value
        // Ordered by codepage
        private const string WebNameIndices =
@"
        // s_webNameIndices contains the start index of each code page's default
        // web name in the string s_webNames. It is indexed by an index into
        // s_mappedCodePages.
        private static readonly int[] s_webNameIndices = new int[]
        {
            0|, // {0} ({1:D})
            {2:D}|
        };
";

        // The format is:
        //     0 - English name
        //     1 - codepage
        // Ordered by codepage
        private const string EnglishNames =
@"
        // s_englishNames is the concatenation of the English names for each codepage.
        // It is used in retrieving the value for System.Text.Encoding.EncodingName
        // given System.Text.Encoding.CodePage.
        // This is done rather than using a large readonly array of strings to avoid
        // generating a large amount of code in the static constructor.
        private const string s_englishNames =|
            ""{0}"" + // {1:D}|
            """";
";

        // The format is:
        //     0 - English name
        //     1 - codepage
        //     2 - Start index of English name
        // The layout is to properly populate the end value
        // Ordered by codepage
        private const string EnglishNameIndices =
@"
        // s_englishNameIndices contains the start index of each code page's English
        // name in the string s_englishNames. It is indexed by an index into s_mappedCodePages.
        private static readonly int[] s_englishNameIndices = new int[]
        {
            0|, // {0} ({1:D})
            {2:D}|
        };
";

        private const string Footer =
@"
    }
}
";

        private static class KeyValuePair
        {
            public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
            {
                return new KeyValuePair<TKey, TValue>(key, value);
            }
        }
    }
}
