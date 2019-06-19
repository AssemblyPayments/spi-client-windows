using SPIClient;
using System;
using System.Reflection;

namespace Test
{
    internal class SpiClientTestUtils
    {
        /// <summary>
        /// Uses reflection to get the field value from an object.
        /// </summary>
        ///
        /// <param name="type">The instance type.</param>
        /// <param name="instance">The instance object.</param>
        /// <param name="fieldName">The field's name which is to be fetched.</param>
        ///
        /// <returns>The field value from the object.</returns>
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        /// <summary>
        /// Uses reflection to get the field value from an object.
        /// </summary>
        ///
        /// <param name="instance">The instance object.</param>
        /// <param name="fieldName">The field's name which is to be fetched.</param>
        /// <param name="value">The field's value</param>
        ///
        /// <returns>The field value from the object.</returns>
        internal static void SetInstanceField(object instance, string fieldName, object value)
        {
            var prop = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            prop.SetValue(instance, value);
        }

        internal static object CallInstanceMethod(object instance, string methodName, object[] parameters)
        {
            MethodInfo methodInfo = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return methodInfo.Invoke(instance, parameters);
        }

        internal static Secrets SetTestSecrets(string encKey = "", string hmacKey = "")
        {
            if (encKey == "" & hmacKey == "")
            {
                encKey = "81CF9E6A14CDAF244A30B298D4CECB505C730CE352C6AF6E1DE61B3232E24D3F";
                hmacKey = "D35060723C9EECDB8AEA019581381CB08F64469FC61A5A04FE553EBDB5CD55B9";
            }

            return new Secrets(encKey, hmacKey);
        }
    }
}