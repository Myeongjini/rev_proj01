using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace WizardGrower.Cloud
{
    public class CloudFunctionsClient : MonoBehaviour
    {
        private const string FunctionsRegion = "asia-northeast3";
        private object functionsInstance;
        private bool initialized;
        private bool functionsSdkAvailable;

        public bool IsReady => initialized && functionsSdkAvailable && functionsInstance != null;

        public void Initialize(bool useEmulatorInEditor = true)
        {
            if (initialized)
                return;

            initialized = true;
            Type functionsType = FindType("Firebase.Functions.FirebaseFunctions");
            if (functionsType == null)
            {
                Debug.LogWarning("Firebase Functions SDK is not installed. CloudFunctionsClient will use local fallbacks.");
                return;
            }

            functionsSdkAvailable = true;
            functionsInstance = ResolveFunctionsInstance(functionsType);
            if (functionsInstance == null)
            {
                Debug.LogWarning("Firebase Functions instance could not be resolved.");
                return;
            }

            if (Application.isEditor && useEmulatorInEditor)
                TryUseEmulator(functionsInstance);
        }

        public async Task<IDictionary<string, object>> CallAsync(string functionName, object payload)
        {
            if (!IsReady)
                throw new InvalidOperationException("Firebase Functions SDK is unavailable.");

            object callable = GetHttpsCallable(functionsInstance, functionName);
            if (callable == null)
                throw new InvalidOperationException($"Callable function not found: {functionName}");

            MethodInfo callAsync = callable.GetType().GetMethod("CallAsync", new[] { typeof(object) })
                ?? callable.GetType().GetMethod("CallAsync", Type.EmptyTypes);
            if (callAsync == null)
                throw new InvalidOperationException("Firebase Functions CallAsync API shape is not recognized.");

            object taskObj = callAsync.GetParameters().Length == 0
                ? callAsync.Invoke(callable, null)
                : callAsync.Invoke(callable, new object[] { payload });
            Task task = taskObj as Task;
            if (task == null)
                throw new InvalidOperationException("Firebase Functions CallAsync did not return a Task.");

            await task;
            object result = task.GetType().GetProperty("Result")?.GetValue(task);
            object data = result?.GetType().GetProperty("Data")?.GetValue(result);
            return NormalizeDictionary(data);
        }

        private static object ResolveFunctionsInstance(Type functionsType)
        {
            MethodInfo getInstanceRegion = functionsType.GetMethod("GetInstance", new[] { typeof(string) });
            if (getInstanceRegion != null)
                return getInstanceRegion.Invoke(null, new object[] { FunctionsRegion });

            PropertyInfo defaultInstance = functionsType.GetProperty("DefaultInstance", BindingFlags.Public | BindingFlags.Static);
            return defaultInstance?.GetValue(null);
        }

        private static object GetHttpsCallable(object functions, string functionName)
        {
            MethodInfo method = functions.GetType().GetMethod("GetHttpsCallable", new[] { typeof(string) });
            return method?.Invoke(functions, new object[] { functionName });
        }

        private static void TryUseEmulator(object functions)
        {
            MethodInfo useEmulator = functions.GetType().GetMethod("UseEmulator", new[] { typeof(string), typeof(int) });
            if (useEmulator != null)
            {
                useEmulator.Invoke(functions, new object[] { "localhost", 5001 });
                return;
            }

            MethodInfo useFunctionsEmulator = functions.GetType().GetMethod("UseFunctionsEmulator", new[] { typeof(string) });
            useFunctionsEmulator?.Invoke(functions, new object[] { "http://localhost:5001" });
        }

        private static IDictionary<string, object> NormalizeDictionary(object data)
        {
            Dictionary<string, object> normalized = new Dictionary<string, object>();
            if (data is IDictionary<string, object> typed)
                return typed;

            if (data is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                    if (entry.Key != null)
                        normalized[entry.Key.ToString()] = entry.Value;
            }
            return normalized;
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
