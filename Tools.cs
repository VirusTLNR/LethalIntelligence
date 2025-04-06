using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LethalIntelligence
{
    internal static class Tools
    {
        public static TComponent CopyComponent<TComponent>(this GameObject dest, TComponent orig) where TComponent : Component
        {
            Type cmpType = orig.GetType();

            Component copy = dest.AddComponent(cmpType);

            FieldInfo[] fields = cmpType.GetFields();

            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(orig));
            }

            return copy as TComponent;
        }
    }
}
