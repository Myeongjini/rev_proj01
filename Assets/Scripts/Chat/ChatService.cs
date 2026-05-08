using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace WizardGrower.Chat
{
    [Serializable]
    public struct ChatMessage
    {
        public string Uid;
        public string DisplayName;
        public string Text;
        public long Ts;
    }

    public class ChatService : MonoBehaviour
    {
        private const string FirebaseDatabaseTypeName = "Firebase.Database.FirebaseDatabase, Firebase.Database";

        private object database;
        private Type databaseType;
        private Type databaseReferenceType;
        private Type queryType;
        private Type dataSnapshotType;
        private Type childChangedEventArgsType;
        private PropertyInfo snapshotProperty;
        private PropertyInfo databaseErrorProperty;
        private MethodInfo getReferenceMethod;
        private MethodInfo pushMethod;
        private MethodInfo setValueAsyncMethod;
        private MethodInfo orderByKeyMethod;
        private MethodInfo limitToLastMethod;
        private MethodInfo childMethod;
        private PropertyInfo valueProperty;

        private string uid;
        private string displayName;
        private bool initialized;

        public bool IsInitialized => initialized;
        public string DisplayName => displayName;

        public async Task InitializeAsync(string uid, string displayName)
        {
            if (string.IsNullOrEmpty(uid))
                throw new ArgumentException("ChatService requires a Firebase UID.", nameof(uid));

            EnsureDatabaseTypes();
            this.uid = uid;
            this.displayName = string.IsNullOrWhiteSpace(displayName) ? $"Guest-{uid.Substring(0, Mathf.Min(6, uid.Length))}" : displayName;
            initialized = true;
            await Task.CompletedTask;
        }

        public async Task SendAsync(ChatChannel channel, string stage, string text)
        {
            EnsureInitialized();
            string normalized = NormalizeText(text);
            if (string.IsNullOrEmpty(normalized))
                throw new ArgumentException("Chat text is empty.", nameof(text));
            if (normalized.Length > 200)
                throw new ArgumentException("Chat text must be 200 characters or less.", nameof(text));

            object reference = GetChannelReference(channel, stage);
            object pushedReference = pushMethod.Invoke(reference, null);
            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "uid", uid },
                { "displayName", displayName },
                { "text", normalized },
                { "ts", NowMs() }
            };

            object result = setValueAsyncMethod.Invoke(pushedReference, new object[] { payload });
            if (result is Task task)
                await task;
        }

        public IDisposable SubscribeChannel(ChatChannel channel, string stage, int limit, Action<ChatMessage> onMessage)
        {
            EnsureInitialized();
            object reference = GetChannelReference(channel, stage);
            object query = orderByKeyMethod.Invoke(reference, null);
            query = limitToLastMethod.Invoke(query, new object[] { Mathf.Max(1, limit) });
            return new ChatSubscription(this, query, onMessage);
        }

        public ChatMessage CreateLocalMessage(string text)
        {
            return new ChatMessage
            {
                Uid = uid,
                DisplayName = displayName,
                Text = NormalizeText(text),
                Ts = NowMs()
            };
        }

        public static string BuildStageKey(int chapterNumber, int stageNumber)
        {
            return string.Format("{0}_{1}", chapterNumber, stageNumber);
        }

        private void EnsureInitialized()
        {
            if (!initialized)
                throw new InvalidOperationException("ChatService is not initialized. Call InitializeAsync(uid, displayName) first.");
        }

        private void EnsureDatabaseTypes()
        {
            if (database != null)
                return;

            databaseType = Type.GetType(FirebaseDatabaseTypeName);
            if (databaseType == null)
                throw new InvalidOperationException("Firebase Realtime Database SDK is missing. Import FirebaseDatabase.unitypackage before Task M.");

            databaseReferenceType = Type.GetType("Firebase.Database.DatabaseReference, Firebase.Database");
            queryType = Type.GetType("Firebase.Database.Query, Firebase.Database");
            dataSnapshotType = Type.GetType("Firebase.Database.DataSnapshot, Firebase.Database");
            childChangedEventArgsType = Type.GetType("Firebase.Database.ChildChangedEventArgs, Firebase.Database");
            if (databaseReferenceType == null || queryType == null || dataSnapshotType == null || childChangedEventArgsType == null)
                throw new InvalidOperationException("Firebase Realtime Database SDK is partially imported. Reimport FirebaseDatabase.unitypackage.");

            database = CreateDatabaseInstance();
            getReferenceMethod = databaseType.GetMethod("GetReference", new[] { typeof(string) });
            pushMethod = FindMethod(databaseReferenceType, "Push", 0);
            setValueAsyncMethod = FindMethod(databaseReferenceType, "SetValueAsync", 1);
            orderByKeyMethod = FindMethod(queryType, "OrderByKey", 0);
            limitToLastMethod = FindMethod(queryType, "LimitToLast", 1);
            childMethod = dataSnapshotType.GetMethod("Child", new[] { typeof(string) });
            valueProperty = dataSnapshotType.GetProperty("Value");
            snapshotProperty = childChangedEventArgsType.GetProperty("Snapshot");
            databaseErrorProperty = childChangedEventArgsType.GetProperty("DatabaseError");

            if (database == null || getReferenceMethod == null || pushMethod == null || setValueAsyncMethod == null || orderByKeyMethod == null || limitToLastMethod == null || childMethod == null || valueProperty == null || snapshotProperty == null || databaseErrorProperty == null)
                throw new InvalidOperationException("Firebase Realtime Database API shape is not recognized for chat.");
        }

        private object GetChannelReference(ChatChannel channel, string stage)
        {
            string path = channel == ChatChannel.World ? "chat/world" : string.Format("chat/stage/{0}", stage);
            if (channel == ChatChannel.Stage && string.IsNullOrEmpty(stage))
                throw new ArgumentException("Stage key is required for stage chat.", nameof(stage));

            return getReferenceMethod.Invoke(database, new object[] { path });
        }

        private ChatMessage ToMessage(object args)
        {
            if (args == null || databaseErrorProperty.GetValue(args) != null)
                return default;

            object snapshot = snapshotProperty.GetValue(args);
            if (snapshot == null)
                return default;

            return new ChatMessage
            {
                Uid = ReadString(snapshot, "uid"),
                DisplayName = ReadString(snapshot, "displayName"),
                Text = ReadString(snapshot, "text"),
                Ts = ReadLong(snapshot, "ts")
            };
        }

        private string ReadString(object snapshot, string childName)
        {
            object value = ReadChildValue(snapshot, childName);
            return value != null ? value.ToString() : string.Empty;
        }

        private long ReadLong(object snapshot, string childName)
        {
            object value = ReadChildValue(snapshot, childName);
            if (value == null)
                return 0L;
            return Convert.ToInt64(value);
        }

        private object ReadChildValue(object snapshot, string childName)
        {
            object child = childMethod.Invoke(snapshot, new object[] { childName });
            return valueProperty.GetValue(child);
        }

        private object CreateDatabaseInstance()
        {
            PropertyInfo defaultInstance = databaseType.GetProperty("DefaultInstance", BindingFlags.Public | BindingFlags.Static);
            try
            {
                object instance = defaultInstance?.GetValue(null);
                if (instance != null)
                    return instance;
            }
            catch (TargetInvocationException)
            {
                // Editor configs sometimes omit DatabaseURL from DefaultInstance; fall back to google-services files below.
            }

            string databaseUrl = ResolveDatabaseUrl();
            if (string.IsNullOrEmpty(databaseUrl))
                throw new InvalidOperationException("Firebase Realtime Database URL is missing. Add firebase_url/database_url to google-services-desktop.json or google-services.xml.");

            MethodInfo getInstance = databaseType.GetMethod("GetInstance", new[] { typeof(string) });
            if (getInstance == null)
                throw new InvalidOperationException("FirebaseDatabase.GetInstance(string) was not found.");

            return getInstance.Invoke(null, new object[] { databaseUrl });
        }

        private static string ResolveDatabaseUrl()
        {
            string desktopConfigPath = Path.Combine(Application.streamingAssetsPath, "google-services-desktop.json");
            if (File.Exists(desktopConfigPath))
            {
                string json = File.ReadAllText(desktopConfigPath);
                GoogleServicesDesktopConfig config = JsonUtility.FromJson<GoogleServicesDesktopConfig>(json);
                if (config != null && config.project_info != null && !string.IsNullOrEmpty(config.project_info.firebase_url))
                    return config.project_info.firebase_url;
            }

            string androidConfigPath = Path.Combine(Application.dataPath, "Plugins/Android/FirebaseApp.androidlib/res/values/google-services.xml");
            if (File.Exists(androidConfigPath))
            {
                string xml = File.ReadAllText(androidConfigPath);
                const string marker = "<string name=\"firebase_database_url\" translatable=\"false\">";
                int start = xml.IndexOf(marker, StringComparison.Ordinal);
                if (start >= 0)
                {
                    start += marker.Length;
                    int end = xml.IndexOf("</string>", start, StringComparison.Ordinal);
                    if (end > start)
                        return xml.Substring(start, end - start);
                }
            }

            return string.Empty;
        }

        private static string NormalizeText(string text)
        {
            return (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static MethodInfo FindMethod(Type type, string name, int parameterCount)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name == name && method.GetParameters().Length == parameterCount)
                    return method;
            }

            return null;
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        [Serializable]
        private class GoogleServicesDesktopConfig
        {
            public GoogleServicesProjectInfo project_info;
        }

        [Serializable]
        private class GoogleServicesProjectInfo
        {
            public string firebase_url;
        }

        private sealed class ChatSubscription : IDisposable
        {
            private readonly ChatService service;
            private readonly object query;
            private readonly Action<ChatMessage> onMessage;
            private readonly EventInfo childAddedEvent;
            private readonly Delegate addedDelegate;
            private bool disposed;

            public ChatSubscription(ChatService service, object query, Action<ChatMessage> onMessage)
            {
                this.service = service;
                this.query = query;
                this.onMessage = onMessage;

                childAddedEvent = service.queryType.GetEvent("ChildAdded");
                addedDelegate = CreateEventHandler(childAddedEvent.EventHandlerType, service.childChangedEventArgsType, nameof(OnChildAdded));
                childAddedEvent.AddEventHandler(query, addedDelegate);
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                childAddedEvent.RemoveEventHandler(query, addedDelegate);
                disposed = true;
            }

            private Delegate CreateEventHandler(Type handlerType, Type argsType, string methodName)
            {
                ParameterExpression sender = Expression.Parameter(typeof(object), "sender");
                ParameterExpression args = Expression.Parameter(argsType, "args");
                MethodInfo method = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                MethodCallExpression call = Expression.Call(Expression.Constant(this), method, Expression.Convert(args, typeof(object)));
                return Expression.Lambda(handlerType, call, sender, args).Compile();
            }

            private void OnChildAdded(object args)
            {
                ChatMessage message = service.ToMessage(args);
                if (!string.IsNullOrEmpty(message.Text))
                    onMessage?.Invoke(message);
            }
        }
    }
}
