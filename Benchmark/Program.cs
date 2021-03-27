using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;
using SdlangSharp;
using System;
using System.Diagnostics;
using System.Text;

namespace BenchmarkRawParsing
{
    [MemoryDiagnoser]
    public class LargeInputFile
    {
        [Params(1, 10, 25, 50, 75, 100)]
        public int Megabytes;

        private string _input;
        private ISdlTokenVisitorBase _nullVisitor = new NullSdlTokenVisitor();
        private ISdlTokenVisitorBase _nullRawVisitor = new NullSdlTokenRawVisitor();
        private ISdlTokenVisitorBase _astVisitor = new AstSdlTokenVisitor();

        [GlobalSetup]
        public void Setup()
        {
            var bytesRoughly = 1024 * 1024 * this.Megabytes;
            var stream = new StringBuilder(bytesRoughly);
            for (int i = 0; i < bytesRoughly / Program.EXAMPLE_FILE.Length; i++)
                stream.Append(Program.EXAMPLE_FILE);
            this._input = stream.ToString();
            GC.Collect();
        }

        [Benchmark]
        public void RawLogiclessParse()
        {
            var parser = new SdlReader(this._input.AsSpan());
            while (parser.TokenType != SdlTokenType.EndOfFile)
                parser.Read();
        }

        [Benchmark]
        public void NullTokenPushing()
        {
            var parser = new SdlReader(this._input.AsSpan());
            SdlTokenPusher.ParseAndVisit(parser, this._nullVisitor);
        }

        [Benchmark]
        public void NullTokenRawPushing()
        {
            var parser = new SdlReader(this._input.AsSpan());
            SdlTokenPusher.ParseAndVisit(parser, this._nullRawVisitor);
        }

        [Benchmark]
        public void AstParse()
        {
            var parser = new SdlReader(this._input.AsSpan());
            SdlTokenPusher.ParseAndVisit(parser, this._astVisitor);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<LargeInputFile>();
/*            var b = new LargeInputFile
            {
                Megabytes = 100
            };
            b.Setup();
            for (int i = 0; i < 10; i++)
                b.AstParse();*/
        }

        public const string EXAMPLE_FILE = @"name ""vibe-d""
description ""Event driven web and concurrency framework""
homepage ""https://vibed.org/""

license ""MIT""
copyright ""Copyright © 2012-2020 Sönke Ludwig""
authors ""Sönke Ludwig"" ""Mathias 'Geod24' Lang"" ""Etienne Cimon"" ""Martin Nowak"" \
	""Mihails 'Dicebot' Strasuns"" ""150 contributors total""

systemDependencies ""Optionally OpenSSL 1.1.x""
dependency "":redis"" version=""*""
dependency "":data"" version=""*""
dependency "":core"" version=""*""
dependency "":mongodb"" version=""*""
dependency "":web"" version=""*""
dependency "":utils"" version=""*""
dependency "":http"" version=""*""
dependency "":mail"" version=""*""
dependency "":stream"" version=""*""
dependency "":tls"" version=""*""
dependency "":crypto"" version=""*""
dependency "":textfilter"" version=""*""
dependency "":inet"" version=""*""

targetType ""library""
targetName ""vibed""

// NOTE: ""lib"" is a path with no D sources to work around an issue in DUB 1.0.0
//       and below that results in the standard ""source/"" path to be added even
//       if an explicit ""sourcePaths"" directive is given.
sourcePaths ""lib""
sourceFiles ""source/vibe/d.d"" ""source/vibe/vibe.d""

x:ddoxFilterArgs ""--unittest-examples"" ""--min-protection=Protected""\
	""--ex"" ""vibe.core.drivers."" ""--ex"" ""vibe.internal."" ""--ex"" ""vibe.web.internal.""\
	""--ex"" ""diet.internal"" ""--ex"" ""stdx."" ""--ex"" ""eventcore.internal."" ""--ex"" ""eventcore.drivers.""\
	""--ex"" ""mir."" ""--ex"" ""openssl_version""

configuration ""vibe-core"" {
	subConfiguration ""vibe-d:core"" ""vibe-core""
}
    configuration ""win32_mscoff"" {
	subConfiguration ""vibe-d:core"" ""win32_mscoff""
}
configuration ""libevent"" {
    subConfiguration ""vibe-d:core"" ""libevent""
}
configuration ""libasync"" {
    subConfiguration ""vibe-d:core"" ""libasync""
}
configuration ""win32"" {
    subConfiguration ""vibe-d:core"" ""win32""
}

subPackage ""utils""
subPackage ""data""
subPackage ""core""
subPackage ""stream""
subPackage ""tls""
subPackage ""crypto""
subPackage ""textfilter""
subPackage ""inet""
subPackage ""mail""
subPackage ""http""
subPackage ""mongodb""
subPackage ""redis""
subPackage ""web""

name ""engine""
description ""A minimal D application.""
authors ""Sealab""
copyright ""Copyright © 2020, Sealab""
license ""proprietary""
dependency ""fluent-asserts"" version=""~>0.13.3""
dependency ""silly"" version=""~>1.0.2""
dependency ""erupted"" version=""~>2.0.62""
dependency ""taggedalgebraic"" version=""~>0.11.18""
dependency ""jcli"" version=""~>0.11.0""
dependency ""gfm"" version=""~>8.0.4""
dependency ""bindbc-lua"" version=""~>0.3.0""
dependency ""stdx-allocator"" version=""~>3.0.2""
dependency ""bindbc-freetype"" version=""~>0.9.1""
dependency ""bindbc-sdl"" version=""~>0.19.0""
dependency ""libasync"" version=""~>0.8.6""
targetType ""executable""
targetPath ""bin""
libs ""$PACKAGE_DIR/deps/win_x64/freetype"" ""$PACKAGE_DIR/deps/win_x64/lua51"" ""$PACKAGE_DIR/deps/win_x64/vma_no_assert"" ""$PACKAGE_DIR/deps/win_x64/liblz4_static"" platform=""x86_64""
copyFiles ""$PACKAGE_DIR/deps/COPYING_sdl2.txt"" ""$PACKAGE_DIR/deps/LICENSE_lodepng"" ""deps/FTL.TXT""
copyFiles ""$PACKAGE_DIR/deps/win_x64/*.dll"" platform=""x86_64""
copyFiles ""$PACKAGE_DIR/deps/win_x86/*.dll"" platform=""x86""
versions ""SDL_206"" ""FT_210"" ""LUA_51"" ""BindFT_Static"" ""BindLua_Static""
configuration ""default"" {
	targetType ""executable""
}
    configuration ""library"" {
	targetType ""library""
	versions ""Engine_Library""
}
configuration ""debug-log"" {
    targetType ""executable""

    versions ""Engine_DebugLoggingThread""
}
configuration ""benchmark"" {
    targetType ""executable""

    versions ""Engine_Benchmark""
}
configuration ""debug-lua"" {
    versions ""Engine_EnableStackGuard""
}
configuration ""profile"" {
    versions ""Engine_Profile""
}

# a tag having only a name
my_tag

# three tags acting as name value pairs
first_name ""Akiko""
last_name ""Johnson""
height 68

# a tag with a value list
person ""Akiko"" ""Johnson"" 68

# a tag with attributes
person first_name = ""Akiko"" last_name=""Johnson"" height=68

# a tag with values and attributes
person ""Akiko"" ""Johnson"" height=60

# a tag with attributes using namespaces
person name:first-name=""Akiko"" name:last-name=""Johnson""

# a tag with values, attributes, namespaces, and children
my_namespace:person ""Akiko"" ""Johnson"" dimensions:height=68 {
    son ""Nouhiro"" ""Johnson""
    daughter ""Sabrina"" ""Johnson"" location=""Italy"" {
        hobbies ""swimming"" ""surfing""
        languages ""English"" ""Italian""
        smoker false
    }
}

------------------------------------------------------------------
// (notice the separator style comment above...)

# a log entry
#     note - this tag has two values (date_time and string) and an 
#            attribute (error)
entry 2005/11/23 10:14:23.253-GMT ""Something bad happened"" error = true

# a long line
mylist ""something"" ""another"" true ""shoe"" 2002/12/13 ""rock"" \
    ""morestuff"" ""sink"" ""penny"" 12:15:23.425

# a long string
text ""this is a long rambling line of text with a continuation \
   and it keeps going and going...""
   
# anonymous tag examples

files {
    ""/folder1/file.txt""
    ""/file2.txt""
}
    
# To retrieve the files as a list of strings
#
#     List files = tag.getChild(""files"").getChildrenValues(""content"");
# 
# We us the name ""content"" because the files tag has two children, each of 
# which are anonymous tags (values with no name.)  These tags are assigned
# the name ""content""
    
matrix
{
    1 2 3
    4 5 6
}

# To retrieve the values from the matrix (as a list of lists)
#
#     List rows = tag.getChild(""matrix"").getChildrenValues(""content"");
";
    }
}
