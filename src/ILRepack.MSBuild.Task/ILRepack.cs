﻿/**
 * Copyright (c) 2004, Evain Jb (jb@evain.net)
 * Modified 2007 Marcus Griep (neoeinstein+boo@gmail.com)
 * Modified 2013 Peter Sunde <peter.sunde@gmail.com>
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *     - Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     - Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     - Neither the name of Evain Jb nor the names of its contributors may
 *       be used to endorse or promote products derived from this software
 *       without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using ILRepacking;
using Microsoft.Build.Utilities;

namespace ILRepack.MSBuild.Task
{

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ILRepack : Microsoft.Build.Utilities.Task, IDisposable
    {

        string _attributeFile;
        string _logFile;
        string _outputFile;
        string _keyFile;
        ITaskItem[] _assemblies = new ITaskItem[0];
        ILRepacking.ILRepack.Kind _targetKind;
        string _excludeFileTmpPath;

        /// <summary>
        /// Specifies a keyfile to sign the output assembly
        /// </summary>
        public virtual string KeyFile
        {
            get => _keyFile;
            set => _keyFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        /// Specifies a logfile to output log information.
        /// </summary>
        public virtual string LogFile
        {
            get => _logFile;
            set => _logFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        /// Merges types with identical names into one
        /// </summary>
        public virtual bool Union { get; set; }

        /// <summary>
        /// Enable/disable symbol file generation
        /// </summary>
        public virtual bool DebugInfo { get; set; }

        /// <summary>
        /// Take assembly attributes from the given assembly file
        /// </summary>
        public virtual string AttributeFile
        {
            get => _attributeFile;
            set => _attributeFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        /// Copy assembly attributes (by default only the
        /// primary assembly attributes are copied)
        /// </summary>
        public virtual bool CopyAttributes { get; set; }

        /// <summary>
        /// Allows multiple attributes (if type allows)
        /// </summary>
        public virtual bool AllowMultiple { get; set; }

        /// <summary>
        /// Target assembly kind (Exe|Dll|WinExe|SameAsPrimaryAssembly)
        /// </summary>
        public virtual string TargetKind
        {
            get => _targetKind.ToString();
            set
            {
                if (Enum.IsDefined(typeof(ILRepacking.ILRepack.Kind), value))
                {
                    _targetKind = (ILRepacking.ILRepack.Kind)Enum.Parse(typeof(ILRepacking.ILRepack.Kind), value);
                }
                else
                {
                    Log.LogWarning("TargetKind should be [Exe|Dll|" +
                                   "WinExe|SameAsPrimaryAssembly]; " +
                                   "set to SameAsPrimaryAssembly");
                    _targetKind = ILRepacking.ILRepack.Kind.SameAsPrimaryAssembly;
                }
            }
        }

        /// <summary>
        /// Target platform (v1, v1.1, v2, v4 supported)
        /// </summary>
        public virtual string TargetPlatformVersion { get; set; }

        /// <summary>
        /// Merge assembly xml documentation
        /// </summary>
        public bool XmlDocumentation { get; set; }

        /// <summary>
        /// List of paths to use as "include directories" when
        /// attempting to merge assemblies
        /// </summary>
        public virtual ITaskItem[] SearchDirectories { get; set; } = new ITaskItem[0];

        /// <summary>
        /// Set all types but the ones from
        /// the first assembly 'internal'
        /// </summary>
        public virtual bool Internalize { get; set; }

        /// <summary>
        /// List of assemblies that should not be interalized.
        /// </summary>
        public virtual ITaskItem[] InternalizeExclude { get; set; }

        /// <summary>
        /// Output name for merged assembly
        /// </summary>
        [Required]
        public virtual string OutputFile
        {
            get => _outputFile;
            set => _outputFile = BuildPath(ConvertEmptyToNull(value));
        }

        /// <summary>
        /// List of assemblies that will be merged
        /// </summary>
        [Required]
        public virtual ITaskItem[] InputAssemblies
        {
            get => _assemblies;
            set => _assemblies = value;
        }

        /// <summary>
        /// Set the keyfile, but don't sign the assembly
        /// </summary>
        public virtual bool DelaySign { get; set; }

        /// <summary>
        /// Allows to duplicate resources in output assembly
        /// (by default they're ignored)
        /// </summary>
        public virtual bool AllowDuplicateResources { get; set; }

        /// <summary>
        /// Allows assemblies with Zero PeKind (but obviously only IL will get merged)
        /// </summary>
        public virtual bool ZeroPeKind { get; set; }

        /// <summary>
        /// Use as many CPUs as possible to merge the assemblies
        /// </summary>
        public virtual bool Parallel { get; set; } = true;

        /// <summary>
        /// Name of primary assembly when used in conjunction
        /// with Internalize.
        /// </summary>
        public virtual string PrimaryAssemblyFile { get; set; }

        /// <summary>
        /// Pause execution once completed (good for debugging)
        /// </summary>
        public virtual bool PauseBeforeExit { get; set; }

        /// <summary>
        /// Additional debug information during merge that
        /// will be outputted to LogFile
        /// </summary>
        public virtual bool Verbose { get; set; }

        /// <summary>
        /// Allows (and resolves) file wildcards (e.g. `*`.dll)
        /// in input assemblies
        /// </summary>
        public virtual bool Wildcards { get; set; }

        public override bool Execute()
        {

            var repackOptions = new RepackOptions
            {
                KeyFile = _keyFile,
                LogFile = _logFile,
                Log = !string.IsNullOrEmpty(_logFile),
                LogVerbose = Verbose,
                UnionMerge = Union,
                DebugInfo = DebugInfo,
                CopyAttributes = CopyAttributes,
                AttributeFile = AttributeFile,
                AllowMultipleAssemblyLevelAttributes = AllowMultiple,
                TargetKind = _targetKind,
                TargetPlatformVersion = TargetPlatformVersion,
                XmlDocumentation = XmlDocumentation,
                Internalize = Internalize,
                DelaySign = DelaySign,
                AllowDuplicateResources = AllowDuplicateResources,
                AllowZeroPeKind = ZeroPeKind,
                Parallel = Parallel,
                PauseBeforeExit = PauseBeforeExit,
                OutputFile = _outputFile,
                AllowWildCards = Wildcards,
                
            };
            
            var outputPath = Path.GetDirectoryName(OutputFile);
            if (outputPath != null && !Directory.Exists(outputPath))
            {
                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception ex)
                {
                    Log.LogErrorFromException(ex);
                    return false;
                }
            }

            var assemblies = new List<string>();
            for (var i = 0; i < _assemblies.Length; i++)
            {
                var assemblyName = _assemblies[i].ItemSpec?.ToLowerInvariant();

                if (string.IsNullOrEmpty(assemblyName))
                {
                    Log.LogErrorFromException(new Exception("Empty assembly path found at index: " + i));
                    return false;
                }

                if (assemblies.Contains(assemblyName))
                {
                    Log.LogError("Duplicate assembly path found at index: ");
                    return false;
                }

                assemblies.Add(assemblyName);

                Log.LogMessage(MessageImportance.Normal, "Added assembly {0}", assemblies[i]);
            }

            var searchDirectories = new List<string>();
 
            if (SearchDirectories != null)
            {
                for (var i = 0; i < SearchDirectories.Length; i++)
                {
                    searchDirectories.Add(SearchDirectories[i].ItemSpec);

                    if (string.IsNullOrEmpty(searchDirectories[i]))
                    {
                        Log.LogErrorFromException(new Exception("Empty search directory found at index: " + i));
                        return false;
                    }

                    Log.LogMessage(MessageImportance.Normal, "Added search directory {0}", searchDirectories[i]);
                }
            }

            if (!searchDirectories.Any())
            {
                Log.LogWarning("You should specify at least one search directory. Adding current working directory as a search directory");
                searchDirectories.Add(".");
            }

            var inputAssemblies = _assemblies.Select(x => x.ItemSpec.ToString()).Distinct().ToArray();
            repackOptions.InputAssemblies = inputAssemblies;
            repackOptions.SearchDirectories = searchDirectories.Select(BuildPath);

            if (InternalizeExclude != null)
            {
                var internalizeExclude = new string[InternalizeExclude.Length];
                if (Internalize)
                {
                    for (var i = 0; i < InternalizeExclude.Length; i++)
                    {
                        internalizeExclude[i] = InternalizeExclude[i].ItemSpec;
                        if (string.IsNullOrEmpty(internalizeExclude[i]))
                        {
                             Log.LogErrorFromException(new Exception("Empty internalize exclude item at index: " + i));
                             return false;
                        }

                        if (!File.Exists(internalizeExclude[i]) && !File.Exists(BuildPath(internalizeExclude[i])))
                        {
                            Log.LogErrorFromException(new Exception($"Unable to exclude assembly '{internalizeExclude[i]}' because it does not exist."));
                            return false;
                        }

                        Log.LogMessage(MessageImportance.Normal,"Assembly {0} is excluded from being internalized.", internalizeExclude[i]);
                    }

                    // Create a temporary file with a list of assemblies that
                    // should not be internalized.
                    _excludeFileTmpPath = Path.GetTempFileName();
                    File.WriteAllLines(_excludeFileTmpPath, internalizeExclude);
                    repackOptions.ExcludeFile = _excludeFileTmpPath;
                }
            }

            try
            {
                Log.LogMessage(MessageImportance.Normal, "Merging {0} assemb{1} to '{2}'.",
                    _assemblies.Length, _assemblies.Length != 1 ? "ies" : "y", _outputFile);

                #if !NETFULLFRAMEWORK
                var ilMerger = new ILRepacking.ILRepack(repackOptions, new ILRepackLogger(this));
                #else
                var ilMerger = new ILRepacking.ILRepack(repackOptions);
                #endif

                ilMerger.Repack();
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static string ConvertEmptyToNull(string str)
        {
            return string.IsNullOrEmpty(str) ? null : str;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static string BuildPath(string path)
        {
            var workDir = Directory.GetCurrentDirectory();
            return (string.IsNullOrEmpty(path) || workDir == null) ? null :
                Path.Combine(workDir, path);
        }

        public void Dispose()
        {
            // Remove temporary exclude file
            if (File.Exists(_excludeFileTmpPath))
            {
                File.Delete(_excludeFileTmpPath);
            }
        }

    }
}