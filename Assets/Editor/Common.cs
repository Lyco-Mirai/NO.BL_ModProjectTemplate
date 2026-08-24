using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using BL.Blueprinter;
using BL.Common;

namespace BL.Common
{
    public static class T
    {
        public static T[] AddRaw<T>(T[] array, T newElement)
        {
            T[] newArray = new T[array.Length + 1];
            for (int _x = 0; _x < array.Length; _x++)
            {
                newArray[_x] = array[_x];
            }
            newArray[newArray.Length - 1] = newElement;
            return newArray;
        }
        public static T[] RemoveNulls<T>(T[] sourceArray)
        {
            T[] newArray = new T[0];
            for (int _x = 0; _x < sourceArray.Length; _x++)
            {
                if (sourceArray[_x] != null)
                {
                    newArray = AddRaw(newArray, sourceArray[_x]);
                }
            }
            return newArray;
        }
        public static T[] Add<T>(T[] array, T newElement)
        {
            T[] newArray = AddRaw(array, newElement);
            return RemoveNulls(newArray);
        }
    }
}
