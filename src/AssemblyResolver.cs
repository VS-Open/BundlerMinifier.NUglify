﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Source : https://github.com/dotnet/buildtools/blob/48e815d61529eeda8d1a419ae904edea9d4092aa/src/common/AssemblyResolver.cs
// Documentation : https://natemcmaster.com/blog/2017/11/11/msbuild-task-with-dependencies 

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BundlerMinifier.NUglify
{
    internal static class AssemblyResolver
    {
        static AssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        /// <summary>
        /// Call to enable the assembly resolver for the current AppDomain.
        /// </summary>
        public static void Enable()
        {
            // intentionally empty.  This is just meant to ensure the static constructor
            // has run.
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // apply any existing policy
            AssemblyName referenceName = new AssemblyName(AppDomain.CurrentDomain.ApplyPolicy(args.Name));

            string fileName = referenceName.Name + ".dll";

            // look next to requesting assembly
            string assemblyPath = args.RequestingAssembly?.Location;
            string probingPath;
            Assembly assm;
            if (!String.IsNullOrEmpty(assemblyPath))
            {
                probingPath = Path.Combine(Path.GetDirectoryName(assemblyPath), fileName);
                Debug.WriteLine($"Considering {probingPath} based on RequestingAssembly");
                if (Probe(probingPath, referenceName.Version, out assm))
                {
                    return assm;
                }
            }

            // look in AppDomain base directory
            probingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            Debug.WriteLine($"Considering {probingPath} based on BaseDirectory");
            if (Probe(probingPath, referenceName.Version, out assm))
            {
                return assm;
            }

            // look next to the executing assembly
            assemblyPath = Assembly.GetExecutingAssembly().Location;
            if (!String.IsNullOrEmpty(assemblyPath))
            {
                probingPath = Path.Combine(Path.GetDirectoryName(assemblyPath), fileName);

                Debug.WriteLine($"Considering {probingPath} based on ExecutingAssembly");
                if (Probe(probingPath, referenceName.Version, out assm))
                {
                    return assm;
                }
            }

            // look in current directory
            Debug.WriteLine($"Considering {fileName}");
            if (Probe(fileName, referenceName.Version, out assm))
            {
                return assm;
            }

            return null;
        }

        /// <summary>
        /// Considers a path to load for satisfying an assembly ref and loads it
        /// if the file exists and version is sufficient.
        /// </summary>
        /// <param name="filePath">Path to consider for load</param>
        /// <param name="minimumVersion">Minimum version to consider</param>
        /// <param name="assembly">loaded assembly</param>
        /// <returns>true if assembly was loaded</returns>
        private static bool Probe(string filePath, Version minimumVersion, out Assembly assembly)
        {
            if (File.Exists(filePath))
            {
                AssemblyName name = AssemblyName.GetAssemblyName(filePath);

                if (name.Version >= minimumVersion)
                {
                    assembly = Assembly.Load(name);
                    return true;
                }
            }

            assembly = null;
            return false;
        }
    }
}