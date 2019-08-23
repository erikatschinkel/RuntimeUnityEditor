using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RuntimeUnityEditor.Core.Debugging
{
    public class DebugHelpers
    {
        public static string debugPath = Application.dataPath;

        public static void SetDebugPath()
        {
            if (Directory.Exists(Application.dataPath)) { debugPath = Application.dataPath; }
            else if (Directory.Exists(Application.persistentDataPath)) { debugPath = Application.persistentDataPath; }
            debugPath = (debugPath.Remove(debugPath.LastIndexOf('/')).Replace('/', '\\') + "\\") + "DEBUG_LOGS\\";
            if (!Directory.Exists(debugPath) || !Directory.Exists(debugPath + "Types")) { Directory.CreateDirectory(debugPath); Directory.CreateDirectory(debugPath + "Types"); }
        }

        public static void DebugWriteDetails()
        {
            string strAssemblies = "Loaded Assemblies:\r\n\r\n";
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                string localCodeBase = GetAssemblyFullPath(assembly);
                string strTypes = "Types Found in Assembly: " + assembly.FullName + "\r\n\r\n";
                strAssemblies += "    Assembly: " + assembly.FullName + "\r\n    CodeBase: " + localCodeBase + "\r\n\r\n";

                Type[] assemblyTypes = assembly.GetTypes();

                foreach (Type assemblyType in assemblyTypes)
                {
                    if (assemblyType != null)
                    {                       
                        string assemblyTypeDetails = PopulateTypeInfo(assemblyType);
                        strTypes += String.Format("    {0}\r\n{1}\r\n", assemblyType.FullName, assemblyTypeDetails);
                    }
                }

                File.WriteAllText(debugPath + "Types\\" + assembly.GetName().Name + ".txt", strTypes);
            }

            File.WriteAllText(debugPath + "LOADED_ASSEMBLIES.txt", strAssemblies);
        }

        public static string GetAssemblyFullPath(Assembly assembly)
        {
            try
            {
                string codeBasePseudoUrl = assembly.CodeBase; // "pseudo" because it is not properly escaped
                if (codeBasePseudoUrl != null)
                {
                    const string filePrefix3 = @"file:///";
                    if (codeBasePseudoUrl.StartsWith(filePrefix3))
                    {
                        string startPath = codeBasePseudoUrl.Substring(filePrefix3.Length);
                        string basePath = startPath.Replace('/', '\\');
                        string fullPath = Path.GetFullPath(basePath);
                        return fullPath;
                    }
                }

                //System.Diagnostics.Debug.Assert(false, "CodeBase evaluation failed! - Using Location as fallback.");
                return Path.GetFullPath(assembly.Location);
            }
            catch
            {
                return String.Empty; // If both methods fail, then Assembly is likely Shared Memory
            }

        }

        private static string PopulateTypeInfo(Type type)
        {
            string details = "";

            IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public).Where(m => { var method = m as MethodInfo; return method == null || !m.IsSpecialName; });
            IEnumerable<MemberInfo> propertyAccessors = type.GetProperties(BindingFlags.Static | BindingFlags.Public).SelectMany(p => p.GetAccessors()).Cast<MemberInfo>();
            IEnumerable<MemberInfo> eventAccessors = type.GetEvents(BindingFlags.Static | BindingFlags.Public).SelectMany(e => new[] { e.GetAddMethod(true), e.GetRemoveMethod(true)}).Cast<MemberInfo>();
            //IEnumerable<MemberInfo> accessors = propertyAccessors.Concat(eventAccessors);

            //IEnumerable<MemberInfo> memberList = type.GetMembers(BindingFlags.Static | BindingFlags.Public).Except(accessors); //Alternate Method to below
            //IEnumerable<MemberInfo> memberList = type.GetMembers(BindingFlags.Static | BindingFlags.Public).Where(m => { var method = m as MethodBase; return method == null || !method.IsSpecialName; });
            
            /*
            details += "        Public Members:\r\n";
            foreach(var member in memberList)
            {
                details += "            " + member.Name + "\r\n";
            }

            details += "        Public Accessors:\r\n";
            foreach(var accessor in accessors)
            {
                details += "            " + accessor.Name + "\r\n";
            }
            */

            details += "        Public Methods:\r\n";
            foreach(var method in methods)
            {
                IOrderedEnumerable<ParameterInfo> methodParams = method.GetParameters().OrderBy(m => m.Position);
                string tmp = "";
                foreach (var mp in methodParams)
                {
                    tmp += mp.ParameterType.Name + " " + mp.Name;
                    if( methodParams.Count() > 1) { tmp += ", "; }
                }

                if(tmp.TrimEnd().EndsWith(",")) { tmp = tmp.Remove(tmp.Length - 2); }

                details += "            " + method.Name + "(" + tmp + ")\r\n";
            }

            details += "        Public Properties:\r\n";
            foreach(var property in propertyAccessors)
            {
                details += "            " + property.Name + "\r\n";
            }

            details += "        Public Events:\r\n";
            foreach(var xevent in eventAccessors)
            {
                details += "            " + xevent.Name + "\r\n";
            }

            return details;
        }
    }
}
