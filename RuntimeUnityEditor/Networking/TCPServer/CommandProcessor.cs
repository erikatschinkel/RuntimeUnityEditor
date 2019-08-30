using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using RuntimeUnityEditor.Core.Debugging;
using Mono.CSharp;
using Object = UnityEngine.Object;

namespace RuntimeUnityEditor.Core.Networking.TCPServer
{
    public class CommandProcessor : IDisposable
    {
        #region[Declarations]

        public CommandProcessor Instance { get; }

        #endregion

        #region[Contructor/Dispose]

        public CommandProcessor()
        {
            if (Instance == null)
                Instance = this;

            DebugHelpers.SetDebugPath();
        }

        public void Dispose()
        {
            ((IDisposable)Instance).Dispose();
        }

        #endregion

        public string ProcessCommand(string command)
        {
            string result = "";
            string dataText = "";

            command = command.Remove(0, 1);

            int indexOfOpenAngleBracket = 0;
            int indexOfOpenParentheses = 0;

            if (!command.EndsWith("();"))
            {
                if (command.Contains("<"))
                {
                    command = command.Replace(">();", "");
                    command = command.Replace("> ();", "");
                    indexOfOpenAngleBracket = command.IndexOf('<');
                    dataText = command.Substring(indexOfOpenAngleBracket + 1);
                }

                if (command.Contains("("))
                {
                    command = command.Replace(");", "");
                    indexOfOpenParentheses = command.IndexOf('(');
                    dataText = command.Substring(indexOfOpenParentheses + 1);
                }                
            } else { command = command.Replace("();", ""); }

            if(RuntimeUnityEditorCore._enableDebug) { File.WriteAllText(DebugHelpers.debugPath + "TELNET_CommandProcessDebug.txt", "command: " + command + "\r\ndata: " + dataText);  }

            switch (command)
            {
                case "help":
                    result = REPL.REPL.help;
                    break;
                case "Describe":
                    result = InteractiveBase.Describe((object)dataText);
                    File.WriteAllText(DebugHelpers.debugPath + "TELNET_CommandProcessDebug.txt", "command: " + command + "\r\ndata: " + dataText);
                    if (result != "") { File.WriteAllText(DebugHelpers.debugPath + "TMP.txt", result); }
                    break;
                case "Print":
                    if (!dataText.Contains(","))
                    {
                        InteractiveBase.print((object)dataText);
                    }
                    else
                    {
                        object[] dataParams = InitializeArray<object>(20);
                        InteractiveBase.print(dataText, dataParams);
                    }
                    break;
                case "LoadAssembly":
                    InteractiveBase.LoadAssembly(dataText);
                    break;
                case "LoadPackage":
                    InteractiveBase.LoadPackage(dataText);
                    break;
                case "ShowUsing":
                    InteractiveBase.ShowUsing();
                    break;
                case "ShowVars":
                    InteractiveBase.ShowVars();
                    break;
                case "Time":
                    //TimeSpan resTime = InteractiveBase.Time(dataText);
                    //result = resTime.ToString();
                    break;
                // TESTING ---------------------------------------------------------------------------------------------------------------------------------------
                case "find<":
                    try
                    {
                        Object obj = UnityEngine.Object.FindObjectOfType(Type.GetType("\"" + dataText + "\""));
                        File.WriteAllText(DebugHelpers.debugPath + "TELNET_CommandProcessDebug.txt", "command: " + command + "\r\ndata: " + dataText);
                        if (obj != null) { File.WriteAllText(DebugHelpers.debugPath + "TMP.txt", "object is not null"); }
                        
                        result = obj.QuickDump();
                    }
                    catch(Exception e)
                    {
                        result = e.Message;
                    }

                    break;
                // END TESTING ------------------------------------------------------------------------------------------------------------------------------------
                default:
                    break;
            }

            return result;
        }

        private T[] InitializeArray<T>(int length) where T : new()
        {
            T[] array = new T[length];
            for (int i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }

        /* TESTING
        Type type = dataText.GetType().GetGenericArguments()[0];
        //Test 1
        IEnumerable<Type> types =
            from a in AppDomain.CurrentDomain.GetAssemblies()
            from t in a.GetTypes()
            select t;

        //Test 2
        List<Type> list = new List<Type>();
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in ass.GetExportedTypes())
                {
                    if (t.IsEnum)
                    {
                        list.Add(t);
                    }
                }
            }
        */
    }

    public class Order
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
    }
}
