using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace JotunnBuildTask
{
    public static class AssemblyPublicizer
    {
        /// <summary>
        ///     Publicize a dll
        /// </summary>
        /// <param name="input">Input assembly</param>
        /// <param name="hash"></param>
        /// <param name="publicizedFolder"></param>
        /// <param name="ValheimPath"></param>
        /// <returns></returns>
        public static bool PublicizeDll(string input, string hash, string publicizedFolder, string ValheimPath)
        {
            if (!File.Exists(input))
            {
                System.Console.WriteLine($"File {input} not found.");
                return false;
            }

            AssemblyDefinition assemblyDefinition;

            try
            {
                System.Console.WriteLine($"Publicizing {input}.");

                assemblyDefinition = AssemblyDefinition.ReadAssembly(input);
                BaseAssemblyResolver assemblyResolver = (BaseAssemblyResolver)assemblyDefinition.MainModule.AssemblyResolver;

                foreach (var managedFolder in JotunnBuildTask.GetManagedFolders(ValheimPath))
                {
                    assemblyResolver.AddSearchDirectory(managedFolder);
                }
            }
            catch (Exception exception)
            {
                System.Console.WriteLine($"{exception.Message}");
                return false;
            }

            // Get all type definitions
            var types = GetTypeDefinitions(assemblyDefinition.MainModule);

            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                if (type.IsNested)
                {
                    type.IsNestedPublic = true;
                }
                else
                {
                    type.IsPublic = true;
                }
            }

            var eventNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                var methods = type.Methods;
                for (int j = 0; j < methods.Count; j++)
                {
                    var method = methods[j];
                    if (!method.IsPublic)
                    {
                        method.IsPublic = true;
                    }
                }

                var events = type.Events;
                for (int j = 0; j < events.Count; j++)
                {
                    eventNames.Add(events[j].Name);
                }
            }

            for (int i = 0; i < types.Count; i++)
            {
                var fields = types[i].Fields;
                for (int j = 0; j < fields.Count; j++)
                {
                    var field = fields[j];
                    if (!field.IsPublic && !eventNames.Contains(field.Name))
                    {
                        field.IsPublic = true;
                    }
                }
            }

            assemblyDefinition.MainModule.Assembly.CustomAttributes.Add(new CustomAttribute(assemblyDefinition.MainModule.ImportReference(typeof(JVLOriginalAssemblyHashAttribute).GetConstructor(new[] { typeof(string) })))
            {
                ConstructorArguments = { new CustomAttributeArgument(assemblyDefinition.MainModule.ImportReference(typeof(string)), hash) }
            });

            var outputFilename = Path.Combine(publicizedFolder, $"{Path.GetFileNameWithoutExtension(input)}_{JotunnBuildTask.Publicized}{Path.GetExtension(input)}");

            try
            {
                assemblyDefinition.Write(outputFilename);
            }
            catch (Exception exception)
            {
                System.Console.WriteLine($"Could not write file {outputFilename}.");
                System.Console.WriteLine(exception.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Returns all type definitions for the module
        /// </summary>
        /// <param name="moduleDefinition"></param>
        /// <returns></returns>
        private static List<TypeDefinition> GetTypeDefinitions(ModuleDefinition moduleDefinition)
        {
            var result = new List<TypeDefinition>();
            CollectTypeDefinitions(moduleDefinition.Types, result);
            return result;
        }

        /// <summary>
        ///     Get all type definitions recursive
        /// </summary>
        /// <param name="typeDefinitions"></param>
        /// <param name="result"></param>
        private static void CollectTypeDefinitions(Collection<TypeDefinition> typeDefinitions, List<TypeDefinition> result)
        {
            if (typeDefinitions == null || typeDefinitions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < typeDefinitions.Count; i++)
            {
                var type = typeDefinitions[i];
                result.Add(type);
                if (type.HasNestedTypes)
                {
                    CollectTypeDefinitions(type.NestedTypes, result);
                }
            }
        }
    }
}
