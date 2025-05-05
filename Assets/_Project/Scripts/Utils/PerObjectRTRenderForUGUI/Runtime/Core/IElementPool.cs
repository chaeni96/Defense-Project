using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace CatDarkGame.PerObjectRTRenderForUGUI
{
    public interface IElementPool<T> where T : IElementPool<T>
    {
        int poolIndex { get; }
        
        internal static readonly List<T> ElementPool = new List<T>();
        
        internal static void Register(T element)
        {
            ((IElementPoolInternal)element).SetPoolIndex(ElementPool.Count);
            ElementPool.Add(element);
        }

        internal static void UnRegister(T element)
        {
            if (ElementPool.Count < 1) return;
            var poolIndex = ElementPool.IndexOf(element);
            ((IElementPoolInternal)element).SetPoolIndex(-1);
            ElementPool.RemoveAtSwapBack(poolIndex);
            int poolCount = ElementPool.Count;
            if (poolCount > 0 && poolCount > poolIndex)
            {
                ((IElementPoolInternal)ElementPool[poolIndex]).SetPoolIndex(poolIndex);
            }
        }
    }
    
    internal interface IElementPoolInternal
    {
        void SetPoolIndex(int index);
    }
}