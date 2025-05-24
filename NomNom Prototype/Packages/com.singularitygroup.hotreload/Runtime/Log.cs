using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
#if (UNITY_2019_4_OR_NEWER)
using UnityEngine;
#endif

namespace SingularityGroup.HotReload {
    internal static class Log {
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
        public static LogLevel minLevel = LogLevel.Info;

        /// <summary>
        /// Tag every log so that users know which logs came from Hot Reload
        /// </summary>
        private const string TAG = "[HotReload] ";

        [StringFormatMethod("message")]
        public static void Debug(string message, params object[] args) {
            if (minLevel <= LogLevel.Debug) {
            #if (UNITY_2019_4_OR_NEWER)
                UnityEngine.Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, TAG + message, args);
            #else
                UnityEngine.Debug.LogFormat(TAG + message, args);
            #endif
            }
        }

        [StringFormatMethod("message")]
        public static void Info(string message, params object[] args) {
            if (minLevel <= LogLevel.Info) {
            #if (UNITY_2019_4_OR_NEWER)
                UnityEngine.Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, TAG + message, args);
            #else
                UnityEngine.Debug.LogFormat(TAG + message, args);
            #endif
            }
        }

        [StringFormatMethod("message")]
        public static void Information(string message, params object[] args) => Info(message, args);

        [StringFormatMethod("message")]
        public static void Warning(string message, params object[] args) {
            if (minLevel <= LogLevel.Warning) {
            #if (UNITY_2019_4_OR_NEWER)
                UnityEngine.Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, TAG + message, args);
            #else
                UnityEngine.Debug.LogWarningFormat(TAG + message, args);
            #endif
            }
        }

        [StringFormatMethod("message")]
        public static void Error(string message, params object[] args) {
            if (minLevel <= LogLevel.Error) {
            #if (UNITY_2019_4_OR_NEWER)
                UnityEngine.Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, TAG + message, args);
            #else
                UnityEngine.Debug.LogErrorFormat(TAG + message, args);
            #endif
            }
        }
    }

    public enum LogLevel {
        /// Debug logs are useful for developers of Hot Reload 
        Debug = 1,
        
        /// Info logs potentially useful for users of Hot Reload 
        Info = 2,
        
        /// Warnings are visible to users of Hot Reload 
        Warning = 3,
        
        /// Errors are visible to users of Hot Reload 
        Error = 4,
    }

    // public api for users
    public static class HotReloadLogging {
        /// <summary>
        /// Default is to log everything except Debug logs
        /// </summary>
        /// <remarks>
        /// Set to Level.Debug to see more output. 
        /// </remarks>
        public static void SetLogLevel(LogLevel level) {
            Log.minLevel = level;
        }
    }
}