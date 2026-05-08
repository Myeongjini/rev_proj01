using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace WizardGrower.Multiplayer
{
    public enum RemotePresenceEventType
    {
        Added,
        Changed,
        Removed
    }

    public struct RemotePresenceEvent
    {
        public RemotePresenceEventType Type;
        public string Uid;
        public string DisplayName;
        public Vector2 Position;
        public long LastUpdateUnixMs;
    }

    public class PresenceService : MonoBehaviour
    {
        private const string FirebaseDatabaseTypeName = "Firebase.Database.FirebaseDatabase, Firebase.Database";

        private object database;
        private Type databaseType;
        private Type databaseReferenceType;
        private Type dataSnapshotType;
        private Type childChangedEventArgsType;
        private PropertyInfo snapshotProperty;
        private PropertyInfo databaseErrorProperty;
        private MethodInfo getReferenceMethod;
        private MethodInfo setValueAsyncMethod;
        private MethodInfo removeValueAsyncMethod;
        private MethodInfo onDisconnectMethod;
        private MethodInfo onDisconnectRemoveValueMethod;
        private MethodInfo childMethod;
        private PropertyInfo keyProperty;
        private PropertyInfo valueProperty;

        private string uid;
        private string displayName;
        private bool initialized;

        public async Task InitializeAsync(string uid, string displayName)
        {
            if (string.IsNullOrEmpty(uid))
                throw new ArgumentException("PresenceService requires a Firebase UID.", nameof(uid));

            EnsureDatabaseTypes();
            this.uid = uid;
            this.displayName = string.IsNullOrWhiteSpace(displayName) ? $"Guest-{uid.Substring(0, Mathf.Min(6, uid.Length))}" : displayName;
            initialized = true;
            await Task.CompletedTask;
        }

        public async Task WriteOwnAsync(string stage, float x, float y)
        {
            EnsureInitialized();
            object reference = GetPresenceReference(stage, uid);
            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "x", x },
                { "y", y },
                { "displayName", displayName },
                { "lastUpdateUnixMs", NowMs() }
            };

            RegisterOnDisconnect(reference);
            InvokeQueuedTask(setValueAsyncMethod, reference, payload);
            await Task.CompletedTask;
        }

        public async Task RemoveOwnAsync(string stage)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(stage))
                return;

            InvokeQueuedTask(removeValueAsyncMethod, GetPresenceReference(stage, uid));
            await Task.CompletedTask;
        }

        public IDisposable SubscribeStage(string stage, Action<RemotePresenceEvent> onEvent)
        {
            EnsureInitialized();
            object stageReference = GetStageReference(stage);
            return new StageSubscription(this, stageReference, onEvent);
        }

        private void EnsureInitialized()
        {
            if (!initialized)
                throw new InvalidOperationException("PresenceService is not initialized. Call InitializeAsync(uid, displayName) first.");
        }

        private void EnsureDatabaseTypes()
        {
            if (database != null)
                return;

            databaseType = Type.GetType(FirebaseDatabaseTypeName);
            if (databaseType == null)
                throw new InvalidOperationException("Firebase Realtime Database SDK is missing. Import FirebaseDatabase.unitypackage before Task K.");

            databaseReferenceType = Type.GetType("Firebase.Database.DatabaseReference, Firebase.Database");
            dataSnapshotType = Type.GetType("Firebase.Database.DataSnapshot, Firebase.Database");
            childChangedEventArgsType = Type.GetType("Firebase.Database.ChildChangedEventArgs, Firebase.Database");
            Type onDisconnectType = Type.GetType("Firebase.Database.OnDisconnect, Firebase.Database");
            if (databaseReferenceType == null || dataSnapshotType == null || childChangedEventArgsType == null || onDisconnectType == null)
                throw new InvalidOperationException("Firebase Realtime Database SDK is partially imported. Reimport FirebaseDatabase.unitypackage.");

            database = CreateDatabaseInstance();
            getReferenceMethod = databaseType.GetMethod("GetReference", new[] { typeof(string) });
            setValueAsyncMethod = FindMethod(databaseReferenceType, "SetValueAsync", 1);
            removeValueAsyncMethod = FindMethod(databaseReferenceType, "RemoveValueAsync", 0);
            onDisconnectMethod = FindMethod(databaseReferenceType, "OnDisconnect", 0);
            onDisconnectRemoveValueMethod = FindMethod(onDisconnectType, "RemoveValue", 0);
            childMethod = dataSnapshotType.GetMethod("Child", new[] { typeof(string) });
            keyProperty = dataSnapshotType.GetProperty("Key");
            valueProperty = dataSnapshotType.GetProperty("Value");
            snapshotProperty = childChangedEventArgsType.GetProperty("Snapshot");
            databaseErrorProperty = childChangedEventArgsType.GetProperty("DatabaseError");

            if (database == null || getReferenceMethod == null || setValueAsyncMethod == null || removeValueAsyncMethod == null || onDisconnectMethod == null || onDisconnectRemoveValueMethod == null || childMethod == null || keyProperty == null || valueProperty == null || snapshotProperty == null || databaseErrorProperty == null)
                throw new InvalidOperationException("Firebase Realtime Database API shape is not recognized.");
        }

        private object GetStageReference(string stage)
        {
            if (string.IsNullOrEmpty(stage))
                throw new ArgumentException("Stage key is required.", nameof(stage));

            return getReferenceMethod.Invoke(database, new object[] { $"presence/{stage}" });
        }

        private object GetPresenceReference(string stage, string uid)
        {
            if (string.IsNullOrEmpty(uid))
                throw new ArgumentException("UID is required.", nameof(uid));

            return getReferenceMethod.Invoke(database, new object[] { $"presence/{stage}/{uid}" });
        }

        private void RegisterOnDisconnect(object reference)
        {
            object onDisconnect = onDisconnectMethod.Invoke(reference, null);
            object result = onDisconnectRemoveValueMethod.Invoke(onDisconnect, null);
            if (result is Task task && task.IsFaulted)
                Debug.LogWarning($"Presence OnDisconnect registration failed: {task.Exception?.GetBaseException().Message}");
        }

        private RemotePresenceEvent ToEvent(RemotePresenceEventType type, object args)
        {
            if (args == null || databaseErrorProperty.GetValue(args) != null)
                return default;

            object snapshot = snapshotProperty.GetValue(args);
            if (snapshot == null)
                return default;

            string eventUid = keyProperty.GetValue(snapshot) as string;
            if (string.IsNullOrEmpty(eventUid) || eventUid == uid)
                return default;

            return new RemotePresenceEvent
            {
                Type = type,
                Uid = eventUid,
                DisplayName = ReadString(snapshot, "displayName"),
                Position = new Vector2(ReadFloat(snapshot, "x"), ReadFloat(snapshot, "y")),
                LastUpdateUnixMs = ReadLong(snapshot, "lastUpdateUnixMs")
            };
        }

        private string ReadString(object snapshot, string childName)
        {
            object value = ReadChildValue(snapshot, childName);
            return value != null ? value.ToString() : string.Empty;
        }

        private float ReadFloat(object snapshot, string childName)
        {
            object value = ReadChildValue(snapshot, childName);
            if (value == null)
                return 0f;
            return Convert.ToSingle(value);
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

        private static MethodInfo FindMethod(Type type, string name, int parameterCount)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name == name && method.GetParameters().Length == parameterCount)
                    return method;
            }

            return null;
        }

        private static void InvokeQueuedTask(MethodInfo method, object target, params object[] args)
        {
            object result = method.Invoke(target, args);
            if (result is Task task)
                task.ContinueWith(failed =>
                {
                    if (failed.IsFaulted)
                        Debug.LogWarning(failed.Exception?.GetBaseException().Message);
                });
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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

        private sealed class StageSubscription : IDisposable
        {
            private readonly PresenceService service;
            private readonly object reference;
            private readonly Action<RemotePresenceEvent> onEvent;
            private readonly EventInfo childAddedEvent;
            private readonly EventInfo childChangedEvent;
            private readonly EventInfo childRemovedEvent;
            private readonly Delegate addedDelegate;
            private readonly Delegate changedDelegate;
            private readonly Delegate removedDelegate;
            private bool disposed;

            public StageSubscription(PresenceService service, object reference, Action<RemotePresenceEvent> onEvent)
            {
                this.service = service;
                this.reference = reference;
                this.onEvent = onEvent;

                childAddedEvent = service.databaseReferenceType.GetEvent("ChildAdded");
                childChangedEvent = service.databaseReferenceType.GetEvent("ChildChanged");
                childRemovedEvent = service.databaseReferenceType.GetEvent("ChildRemoved");
                addedDelegate = CreateEventHandler(childAddedEvent.EventHandlerType, service.childChangedEventArgsType, nameof(OnChildAdded));
                changedDelegate = CreateEventHandler(childChangedEvent.EventHandlerType, service.childChangedEventArgsType, nameof(OnChildChanged));
                removedDelegate = CreateEventHandler(childRemovedEvent.EventHandlerType, service.childChangedEventArgsType, nameof(OnChildRemoved));

                childAddedEvent.AddEventHandler(reference, addedDelegate);
                childChangedEvent.AddEventHandler(reference, changedDelegate);
                childRemovedEvent.AddEventHandler(reference, removedDelegate);
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                childAddedEvent.RemoveEventHandler(reference, addedDelegate);
                childChangedEvent.RemoveEventHandler(reference, changedDelegate);
                childRemovedEvent.RemoveEventHandler(reference, removedDelegate);
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
                Publish(RemotePresenceEventType.Added, args);
            }

            private void OnChildChanged(object args)
            {
                Publish(RemotePresenceEventType.Changed, args);
            }

            private void OnChildRemoved(object args)
            {
                Publish(RemotePresenceEventType.Removed, args);
            }

            private void Publish(RemotePresenceEventType type, object args)
            {
                RemotePresenceEvent remoteEvent = service.ToEvent(type, args);
                if (!string.IsNullOrEmpty(remoteEvent.Uid))
                    onEvent?.Invoke(remoteEvent);
            }
        }
    }
}
