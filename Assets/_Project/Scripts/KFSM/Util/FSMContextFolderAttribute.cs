using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kylin.FSM
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class FSMContextFolderAttribute : Attribute
    {
        public string MenuPath { get; private set; }

        public FSMContextFolderAttribute(string menuPath)
        {
            MenuPath = menuPath;
        }
    }
}