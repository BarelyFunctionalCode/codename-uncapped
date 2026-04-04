using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static System.Reflection.BindingFlags;

interface IUpdatableVolumeComponent
{
    void Update();
    bool ExecuteInEditMode => true;
}

[ExecuteAlways]
public sealed class VolumeComponentUpdater : MonoBehaviour
{
    VolumeStack previousStack;
    Dictionary<Type, VolumeComponent> cachedVolumeStackComponents;

    void LateUpdate()
    {
        VolumeStack stack = VolumeManager.instance.stack;
        if (stack == null) return;

        // invalidate cache if stack changed
        if (stack != previousStack) cachedVolumeStackComponents = null;
        previousStack = stack;

        // get components from the VolumeStack using reflection
        cachedVolumeStackComponents ??= typeof(VolumeStack)
            .GetField("components", NonPublic | Instance)
            .GetValue(stack) as Dictionary<Type, VolumeComponent>;

        // update components that implement IUpdatableVolumeComponent
        foreach (var component in cachedVolumeStackComponents.Values)
            if (component is IUpdatableVolumeComponent updatable)
                if (updatable.ExecuteInEditMode || Application.isPlaying)
                    updatable.Update();
    }
}