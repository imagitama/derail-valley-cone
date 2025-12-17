using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DerailValleyBindingHelper;
using DV.Utils;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyCone;

[Serializable]
public class ConeSettings
{
    public float PositionX = 0;
    public float PositionY = 3;
    public float PositionZ = -4;
    public float RotationX = 0;
    public float RotationY = 0;
    public float RotationZ = 0;
    public float Scale = 1f; // TODO: on each axis
    public bool HideBody = false;
    public ConeSettings Clone()
    {
        return new ConeSettings()
        {
            PositionX = PositionX,
            PositionY = PositionY,
            PositionZ = PositionZ,
            RotationX = RotationX,
            RotationY = RotationY,
            RotationZ = RotationZ,
            Scale = Scale,
            HideBody = HideBody
        };
    }
}

public static class ConeHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private static AssetBundle? _coneAssetBundle;

    private static AssetBundle LoadBundle(string pathInsideAssetBundles)
    {
        var bundlePath = Path.Combine(Main.ModEntry.Path, "Dependencies/AssetBundles", pathInsideAssetBundles);

        Logger.Log($"Loading bundle from: {bundlePath}");

        if (!File.Exists(bundlePath))
            throw new Exception($"Asset bundle not found at {bundlePath}");

        return AssetBundle.LoadFromFile(bundlePath);
    }

    public static GameObject? RocketPrefab;

    private static GameObject GetConePrefab()
    {
        if (RocketPrefab != null)
            return RocketPrefab;

        _coneAssetBundle = LoadBundle("cone");

        var all = _coneAssetBundle.LoadAllAssets<GameObject>();

        var newPrefab = all[0];

        Logger.Log($"Found prefab: {newPrefab}");

        RocketPrefab = newPrefab;

        return RocketPrefab;
    }

    public static int GetConeCount(Transform target)
    {
        return GetCones(target).Count;
    }

    public static bool GetDoesTargetHaveCone(Transform target)
    {
        return GetConeCount(target) > 0;
    }

    public static bool GetDoesTargetHaveStandardCone(Transform target, StandardSide side)
    {
        return GetCones(target).Any(x => x.side == side);
    }

    public static void ApplyOffsetsToCone(Transform target, ConeSettings newSettings)
    {
        var pos = new Vector3(0 + newSettings.PositionX, 0 + newSettings.PositionY, 0 + newSettings.PositionZ);
        var rot = Quaternion.Euler(newSettings.RotationX, newSettings.RotationY, newSettings.RotationZ);

        target.localPosition = pos;
        target.localRotation = rot;
        target.localScale = new Vector3(newSettings.Scale, newSettings.Scale, newSettings.Scale);
    }

    public static ConeComponent AddCone(Transform target, TrainCar trainCar, ConeSettings newSettings)
    {
        var prefab = GetConePrefab();

        var newObj = GameObject.Instantiate(prefab, target);

        var settingsToApply = newSettings.Clone();

        ApplyOffsetsToCone(newObj.transform, settingsToApply);

        var component = newObj.gameObject.AddComponent<ConeComponent>();
        component.trainCar = trainCar;
        component.settings = settingsToApply;

        newObj.layer = (int)DVLayer.Train_Big_Collider;

        Logger.Log($"Added Cone as object {newObj} (with component {component}) to {target} layer={newObj.layer}");

        return component;
    }

    public static void UpdateCone(Transform target, ConeSettings newSettings, int? componentIndex = null)
    {
        var components = GetCones(target);

        if (components.Count == 0)
        {
            Logger.Log("Cannot update cone - No components");
            return;
        }

        if (componentIndex != null)
        {
            var component = components[(int)componentIndex];
            components = [component];
        }

        var settingsToApply = newSettings.Clone();

        foreach (var component in components)
        {
            component.settings = settingsToApply;
        }

        ApplyOffsetsToCone(target, settingsToApply);
    }
    public static void UpdateCone(ConeComponent component, ConeSettings newSettings)
    {
        var settingsToApply = newSettings.Clone();

        component.settings = settingsToApply;

        ApplyOffsetsToCone(component.transform, settingsToApply);
    }

    public static void RemoveCone(Transform target, int? componentIndex = null)
    {
        var components = GetCones(target);

        if (components.Count == 0)
        {
            Logger.Log("Cannot remove cone - No components");
            return;
        }

        if (componentIndex != null)
        {
            var component = components[(int)componentIndex];
            components = [component];
        }

        var count = components.Count;

        foreach (var component in components)
            GameObject.Destroy(component.gameObject);

        Logger.Log($"Removed {count} cone components from {target} (index={componentIndex})");
    }

    public static List<ConeComponent> GetAllCones()
    {
        var Cones = SingletonBehaviour<CarSpawner>.Instance.AllCars
            .SelectMany(x => x.GetComponentsInChildren<ConeComponent>())
            .ToList();

        return Cones;
    }

    public static void RemoveAllCones(Transform? target = null)
    {
        if (target != null)
        {
            RemoveCone(target);
            return;
        }

        var allCones = GetAllCones();

        Logger.Log($"Removing all cones ({allCones.Count})...");

        // fix InvalidOperationException
        var ConesToRemove = allCones.ToList();

        foreach (var ConeComponent in ConesToRemove)
        {
            allCones.Remove(ConeComponent);

            GameObject.Destroy(ConeComponent.gameObject);
        }

        Logger.Log("All removed");
    }

    public static List<ConeComponent> GetCones(Transform target)
    {
        return target.GetComponentsInChildren<ConeComponent>().ToList();
    }

    public static void Unload()
    {
        Logger.Log("Unload manager");
        RemoveAllCones();
        _coneAssetBundle?.Unload(unloadAllLoadedObjects: true);
    }
}