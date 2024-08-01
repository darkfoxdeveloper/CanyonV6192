using Canyon.Game.Scripting;
using Canyon.Game.Services.Processors.Scripting;
using Canyon.Game.States;
using Canyon.Game.States.Items;
using Canyon.Game.States.User;

namespace Canyon.Game.Services.Managers
{
    public class LuaScriptManager
    {
        private static readonly ILogger logger = LogFactory.CreateLogger<LuaScriptManager>();

        private static LuaProcessor processor;

        private static bool initialized = false;

        public static Task InitializeAsync()
        {
            if (processor != null)
            {
                logger.LogWarning("Attempt to reload lua processors!!!");
                return Task.CompletedTask;
            }

            logger.LogInformation("Loading LUA Scripts!!");

            // removed multithreading due to event issues (events loading table multiple times)
            // lua must be single thread
            processor = new LuaProcessor(0);
            initialized = true;
            return Task.CompletedTask;
        }

        public static void Reload(int idScript)
        {
            if (initialized)
            {
                LuaScriptsSettings.Reload();
                processor.ReloadScript(idScript);
            }
        }

        public static void Reload()
        {
            if (initialized)
            {
                LuaScriptsSettings.Reload();
                processor.ReloadScripts();
            }
        }

        public static void Run(Character user, Role role, Item item, string input, string script)
        {
            if (initialized)
            {
                processor.Execute(user, role, item, input, script);
            }
        }

        public static void Run(string script)
        {
            if (initialized)
            {
                processor.Execute(null, null, null, null, script);
            }
        }

        public static string ParseTaskDialogAnswerToScript(string task)
        {
            string result = string.Empty;
            bool typeFetched = false;
            string tempType = string.Empty;
            string tempMessage = string.Empty;
            for (int i = 0; i < task.Length; i++)
            {
                if (task[i].Equals('<'))
                {
                    if (typeFetched)
                    {
                        if (tempType.Contains('F'))
                        {
                            result = tempMessage + "(";
                        }
                        else if (tempType.Contains('S'))
                        {
                            result += $"'{tempMessage}',";
                        }
                        else if (tempType.Contains('N'))
                        {
                            result += $"{tempMessage},";
                        }

                        typeFetched = false;
                    }

                    tempType = string.Empty;
                    tempMessage = string.Empty;
                    tempType += task[i];
                }
                else if (task[i].Equals('>'))
                {
                    typeFetched = true;
                    tempType += task[i];
                }
                else if (typeFetched)
                {
                    tempMessage += task[i];
                }
                else
                {
                    tempType += task[i];
                }
            }

            if (typeFetched && !string.IsNullOrEmpty(tempMessage))
            {
                if (tempType.Contains('F'))
                {
                    result += tempMessage + "(";
                }
                else if (tempType.Contains('S'))
                {
                    result += $"'{tempMessage}',";
                }
                else if (tempType.Contains('N'))
                {
                    result += $"{tempMessage},";
                }
            }

            result = result.Trim(',');
            result += ")";
            return result;
        }
    }
}