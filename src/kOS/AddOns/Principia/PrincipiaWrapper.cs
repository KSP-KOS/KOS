using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace kOS.AddOns.Principia
{
    public class PrincipiaWrapper
    {
        private static bool? wrapped = null;
        private static Type principiaAPIType = null;
        private static object principiaObject = null;
        private static MethodInfo prGeopotentialGetCoefficient = null;
        private static MethodInfo prGeopotentialGetReferenceRadius = null;
        private static MethodInfo prFlightPlanExists = null;
        private static MethodInfo prFlightPlanNumberOfManoeuvres = null;
        private static MethodInfo prFlightPlanGetManoeuvreInitialTime = null;
        private static MethodInfo prFlightPlanGetManoeuvreDeltaV = null;
        private static MethodInfo prFlightPlanGetManoeuvreDuration = null;
        private static MethodInfo prFlightPlanGetManoeuvreGuidance = null;

        public static class Principia
        {
            public static string AssemblyName()
            {
                foreach (var loaded_assembly in AssemblyLoader.loadedAssemblies)
                {
                    if (loaded_assembly.assembly.GetName().Name == "principia.ksp_plugin_adapter")
                    {
                        return loaded_assembly.assembly.FullName;
                    }
                }
                throw new DllNotFoundException(
                    "principia.ksp_plugin_adapter not in AssemblyLoader.loadedAssemblies");
            }

            public static Type GetType(string name)
            {
                return Type.GetType(
                  $"principia.ksp_plugin_adapter.{name}, {AssemblyName()}");
            }

            // principia.ksp_plugin_adapter.ExternalInterface.Get().
            public static object Get()
            {
                return GetType("ExternalInterface")
                    .GetMethod("Get")
                    .Invoke(null, null);
            }
        }

        private static void init()
        {
            UnityEngine.Debug.Log("[kOS-principia] Attempting to Grab Principia Assembly...");
            try
            {
                foreach (var loaded_assembly in AssemblyLoader.loadedAssemblies)
                {
                    UnityEngine.Debug.Log("[kOS-principia]   Found assembly: " + loaded_assembly.assembly.GetName().Name + " [" + loaded_assembly.assembly.GetName().FullName + "].");
                }

                principiaObject = Principia.Get();
                if (principiaObject == null)
                {
                    UnityEngine.Debug.Log("[kOS-principia] Principia Object is null. Principia is not installed or is wrong version.");
                    wrapped = false;
                    return;
                }
                principiaAPIType = principiaObject.GetType();

                prGeopotentialGetCoefficient = principiaAPIType.GetMethod("GeopotentialGetCoefficient", BindingFlags.Public | BindingFlags.Instance);
                prGeopotentialGetReferenceRadius = principiaAPIType.GetMethod("GeopotentialGetReferenceRadius", BindingFlags.Public | BindingFlags.Instance);
                prFlightPlanExists = principiaAPIType.GetMethod("FlightPlanExists", BindingFlags.Public | BindingFlags.Instance);
                prFlightPlanNumberOfManoeuvres = principiaAPIType.GetMethod("FlightPlanNumberOfManoeuvres", BindingFlags.Public | BindingFlags.Instance);
                prFlightPlanGetManoeuvreInitialTime = principiaAPIType.GetMethod("FlightPlanGetManoeuvreInitialTime", BindingFlags.Public | BindingFlags.Instance);
                prFlightPlanGetManoeuvreDeltaV = principiaAPIType.GetMethod("FlightPlanGetManoeuvreDeltaV", BindingFlags.Public | BindingFlags.Instance);
                prFlightPlanGetManoeuvreDuration = principiaAPIType.GetMethod("FlightPlanGetManoeuvreDuration", BindingFlags.Public | BindingFlags.Instance);
                prFlightPlanGetManoeuvreGuidance = principiaAPIType.GetMethod("FlightPlanGetManoeuvreGuidance", BindingFlags.Public | BindingFlags.Instance);

                UnityEngine.Debug.Log("[kOS-principia] Principia loaded.");

                wrapped = true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("[kOS-principia] Caught exception in PrincipiaWrapper: " + e.Message);
                wrapped = false;
                return;
            }
        }

        public static class Reflection
        {
            // Returns the value of the property or field of |obj| with the given name.
            public static T GetFieldOrPropertyValue<T>(object obj, string name)
            {
                if (obj == null)
                {
                    throw new NullReferenceException(
                        $"Cannot access {typeof(T).FullName} {name} on null object");
                }
                Type type = obj.GetType();
                object result = null;
                FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    result = field.GetValue(obj);
                }
                else if (property != null)
                {
                    result = property.GetValue(obj, index: null);
                }
                else
                {
                    throw new MissingMemberException(
                        $"No public instance field or property {name} in {type.FullName}");
                }
                try
                {
                    return (T)result;
                }
                catch (Exception exception)
                {
                    throw new InvalidCastException(
                        $@"Could not convert the value of {
                            (field == null ? "property" : "field")} {
                            (field?.FieldType ?? property.PropertyType).FullName} {
                            type.FullName}.{name}, {result}, to {typeof(T).FullName}",
                        exception);
                }
            }

            public static void SetFieldOrPropertyValue<T>(object obj, string name, T value)
            {
                if (obj == null)
                {
                    throw new NullReferenceException(
                        $"Cannot set {typeof(T).FullName} {name} on null object");
                }
                Type type = obj.GetType();
                FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (field == null && property == null)
                {
                    throw new MissingMemberException(
                        $"No public instance field or property {name} in {type.FullName}");
                }
                try
                {
                    field?.SetValue(obj, value);
                    property?.SetValue(obj, value, index: null);
                }
                catch (Exception exception)
                {
                    throw new ArgumentException(
                        $@"Could not set {
                            (field == null ? "property" : "field")} {
                            (field?.FieldType ?? property.PropertyType).FullName} {
                            type.FullName}.{name} to {typeof(T).FullName} {
                            value?.GetType().FullName ?? "null"} {value}",
                        exception);
                }
            }
        }

        // Standard methods
        public static Vector2d? GeopotentialGetCoefficient(CelestialBody body, int degree, int order)
        {
            if (prGeopotentialGetCoefficient == null)
                return null;

            var c20_s20 = prGeopotentialGetCoefficient.Invoke(principiaObject, new object[] { body.flightGlobalsIndex });

            try
            {
                Vector2d r = new Vector2d();
                r.x = Reflection.GetFieldOrPropertyValue<double>(c20_s20, "x");
                r.y = Reflection.GetFieldOrPropertyValue<double>(c20_s20, "y");

                return r;
            }
            catch (MissingMemberException)
            {
                return null;
            }
        }
        public static double? GeopotentialGetReferenceRadius()
        {
            if (prGeopotentialGetReferenceRadius == null)
                return null;

            CelestialBody earth = FlightGlobals.GetHomeBody();
            return (double?)prGeopotentialGetReferenceRadius.Invoke(principiaObject, new object[] { earth.flightGlobalsIndex });
        }
        public static bool FlightPlanExists(Vessel vessel)
        {
            if (prFlightPlanExists == null)
                return false;

            return (bool)prFlightPlanExists.Invoke(principiaObject, new object[] { vessel?.id.ToString() });
        }
        public static int? FlightPlanNumberOfManoeuvres(Vessel vessel)
        {
            if (prFlightPlanNumberOfManoeuvres == null)
                return null;

            return (int?)prFlightPlanNumberOfManoeuvres.Invoke(principiaObject, new object[] { vessel?.id.ToString() });
        }
        public static double? FlightPlanGetManoeuvreInitialTime(Vessel vessel, int index)
        {
            if (prFlightPlanGetManoeuvreInitialTime == null)
                return null;

            return (double?)prFlightPlanGetManoeuvreInitialTime.Invoke(principiaObject, new object[] { vessel?.id.ToString(), index });
        }
        public static double? FlightPlanGetManoeuvreDeltaV(Vessel vessel, int index)
        {
            if (prFlightPlanGetManoeuvreDeltaV == null)
                return null;

            return (double?)prFlightPlanGetManoeuvreDeltaV.Invoke(principiaObject, new object[] { vessel?.id.ToString(), index });
        }
        public static double? FlightPlanGetManoeuvreDuration(Vessel vessel, int index)
        {
            if (prFlightPlanGetManoeuvreDuration == null)
                return null;

            return (double?)prFlightPlanGetManoeuvreDuration.Invoke(principiaObject, new object[] { vessel?.id.ToString(), index });
        }
        public static Vector3d? FlightPlanGetManoeuvreGuidance(Vessel vessel, int index)
        {
            if (prFlightPlanGetManoeuvreGuidance == null)
                return null;

            var xyz = prFlightPlanGetManoeuvreGuidance.Invoke(principiaObject, new object[] { vessel?.id.ToString(), index });

            try
            {
                Vector3d r = new Vector2d();
                r.x = Reflection.GetFieldOrPropertyValue<double>(xyz, "x");
                r.y = Reflection.GetFieldOrPropertyValue<double>(xyz, "y");
                r.z = Reflection.GetFieldOrPropertyValue<double>(xyz, "z");

                return r;

            }
            catch (MissingMemberException)
            {
                return null;
            }
        }

        public static BooleanValue Wrapped()
        {
            if (wrapped != null)
            {
                return wrapped;
            }
            else //if wrapped == null
            {
                init();
                return wrapped;
            }
        }
    }
}