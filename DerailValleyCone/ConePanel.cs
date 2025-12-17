using System;
using UnityEngine;
using UnityModManagerNet;
using DerailValleyModToolbar;
using DV.ThingTypes;
using System.Linq;
using System.Collections.Generic;
using DerailValleyBindingHelper;

namespace DerailValleyCone;

public class ConeSettingsText
{
    public string PositionX = "";
    public string PositionY = "";
    public string PositionZ = "";
    public string RotationX = "";
    public string RotationY = "";
    public string RotationZ = "";
    public string SoundVolume = "";
    public string Scale = "";
    public ConeSettingsText(ConeSettings inputSettings)
    {
        PositionX = inputSettings.PositionX.ToString();
        PositionY = inputSettings.PositionY.ToString();
        PositionZ = inputSettings.PositionZ.ToString();
        RotationX = inputSettings.RotationX.ToString();
        RotationY = inputSettings.RotationY.ToString();
        RotationZ = inputSettings.RotationZ.ToString();
        Scale = inputSettings.Scale.ToString();
    }
}

public class ConePanel : MonoBehaviour, IModToolbarPanel
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private ConeSettings? _settingsEditingDraft = null;
    private ConeSettingsText _settingsEditingText = new ConeSettingsText(new ConeSettings());
    private int? _selectedComponentIndex = null;
    private bool _showBasics = true;
    private bool _showSettings = true;
    private bool _snapping = true;
    // standard values
    private float _standardThrust = 100000f;
    private float _lastKnownSliderValue = 100000;
    private string _lastknownTextValue = "";
    private bool _useStandardThrustSlider = true;
    private float _frontPositionX = 0f;
    private float _frontPositionY = 0f;
    private float _frontPositionZ = 0f;
    private float _rearPositionX = 0f;
    private float _rearPositionY = 0f;
    private float _rearPositionZ = 0f;
    private bool _isVisible = true;

    void Start()
    {
        Logger.Log("[Panel] Start");
    }

    void OnDestroy()
    {
        Logger.Log("[Panel] Destroy");
    }

    (Transform transform, Rigidbody rigidbody, TrainCar trainCar)? GetConeTargetInfo()
    {
        if (PlayerManager.Car == null)
            return null;

        return (PlayerManager.Car.transform, PlayerManager.Car.rb, PlayerManager.Car);
    }

    Transform? GetConeTargetTransform()
    {
        if (PlayerManager.Car == null)
            return null;

        return PlayerManager.Car.transform;
    }

    void StopTrainMoving()
    {
        Logger.Log("[Panel] Stopping train moving");

        var target = GetConeTargetInfo();
        if (target == null)
            return;

        target.Value.trainCar.StopMovement();
    }

    void DerailTrain()
    {
        Logger.Log("[Panel] Derail train");

        var target = GetConeTargetInfo();
        if (target == null)
            return;

        target.Value.trainCar.Derail();
    }

    public Vector3? GetStandardRearConePosition(TrainCar trainCar)
    {
        switch (trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                return new Vector3(0, 0.5f, -4.8f);
            case TrainCarType.LocoDH4:
                return new Vector3(0, 0.5f, -8f);
        }

        return null;
    }

    public Vector3? GetStandardFrontConePosition(TrainCar trainCar)
    {
        switch (trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                return new Vector3(0, 0.5f, 4.8f);
            case TrainCarType.LocoDH4:
                return new Vector3(0, 0.5f, 8f);
        }

        return null;
    }

    public void Window(Rect rect)
    {
        var target = GetConeTargetInfo();

        GUILayout.Label($"Train Car: {(target != null ?
                $"{target.Value.trainCar.carType}" : "(none)")}"); ;

        if (GUILayout.Button($"<b>Basics {(_showBasics ? "▼" : "▶")}</b>", GUI.skin.label)) _showBasics = !_showBasics;
        if (_showBasics) DrawBasics(target != null ? target.Value.transform : null);

        if (GUILayout.Button($"<b>Settings {(_showSettings ? "▼" : "▶")}</b>", GUI.skin.label)) _showSettings = !_showSettings;
        if (_showSettings) DrawSettings();
    }

    // TODO: re-use with AddCone()
    void AddConeViaPreset(ConeSettings cone)
    {
        Logger.Log($"[Panel] Add cone via preset cone={cone}");

        var target = GetConeTargetInfo();
        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        ConeHelper.AddCone(transform, trainCar, cone);

        _selectedComponentIndex = null;
    }

    void DrawBasics(Transform? target)
    {
        GUI.enabled = target != null;

        void OnChange() => HydrateStandardCones(target);

        GUI.enabled = target != null && ConeHelper.GetDoesTargetHaveStandardCone(target, StandardSide.Front) == false;

        // if (ConeHelper.GetDoesTargetHaveStandardCone(StandardSide.Front))
        // {
        // if (GUILayout.Button($"Remove Front Cone"))
        // {
        //     RemoveStandardFrontCone();
        // }
        // }
        // else
        // {
        if (GUILayout.Button($"Add Front Cone"))
        {
            AddStandardFrontCone();
        }
        // }
        GUI.enabled = true;

        DrawStandardConeOffsetSlider("X", ref _frontPositionX, -5f, 5f, OnChange);
        DrawStandardConeOffsetSlider("Y", ref _frontPositionY, -5f, 5f, OnChange);
        DrawStandardConeOffsetSlider("Z", ref _frontPositionZ, -10f, 10f, OnChange);

        GUI.enabled = target != null && ConeHelper.GetDoesTargetHaveStandardCone(target, StandardSide.Rear) == false;

        // if (ConeHelper.GetDoesTargetHaveStandardCone(StandardSide.Rear))
        // {
        // if (GUILayout.Button($"Remove Rear Cone"))
        // {
        //     RemoveStandardRearCone();
        // }
        // }
        // else
        // {
        if (GUILayout.Button($"Add Rear Cone"))
        {
            AddStandardRearCone();
        }
        // }
        GUI.enabled = true;

        DrawStandardConeOffsetSlider("X", ref _rearPositionX, -5f, 5f, OnChange);
        DrawStandardConeOffsetSlider("Y", ref _rearPositionY, -5f, 5f, OnChange);
        DrawStandardConeOffsetSlider("Z", ref _rearPositionZ, -10f, 10f, OnChange);

        if (GUILayout.Button("Remove All Cones From Train"))
        {
            RemoveAllConesFromCurrentTrain();
        }

        GUI.enabled = true;
    }

    void DrawStandardConeOffsetSlider(string label, ref float value, float min, float max, Action onChange)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: ", GUILayout.Width(30));
        GUILayout.Label($"{min}m", GUILayout.Width(40));
        var newValueRaw = GUILayout.HorizontalSlider(value, min, max);
        var newValue = _snapping ? Mathf.Round(newValueRaw / 0.05f) * 0.05f : newValueRaw;
        GUILayout.Label($"{max}m", GUILayout.Width(40));
        GUILayout.Label($" {value:F2}", GUILayout.Width(30));
        GUILayout.EndHorizontal();

        if (newValue != value)
        {
            value = newValue;
            onChange();
        }
    }

    void DrawConePositionInput(string label, ref string textValue, ref float value, float min, float max, Action onChange)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: ", GUILayout.Width(50));
        GUILayout.Label($"{min}m", GUILayout.Width(40));
        var newValueRaw = GUILayout.HorizontalSlider(value, min, max);
        var newValue = _snapping ? Mathf.Round(newValueRaw / 0.05f) * 0.05f : newValueRaw;
        GUILayout.Label($"{max}m", GUILayout.Width(40));
        var newTextValue = GUILayout.TextField(textValue, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        if (newValue != value)
        {
            value = newValue;
            textValue = newValue.ToString();
            onChange();
            return;
        }
        if (newTextValue != textValue)
        {
            textValue = newTextValue;
            if (float.TryParse(newTextValue, out float floatResult))
            {
                if (floatResult != value)
                {
                    value = floatResult;
                    onChange();
                }
            }
        }
    }

    void DrawConeRotationInput(string label, ref string textValue, ref float value, Action onChange)
    {
        var min = 0;
        var max = 360;
        var snapAmount = 5f;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: ", GUILayout.Width(50));
        GUILayout.Label($"0", GUILayout.Width(40));
        var newValueRaw = GUILayout.HorizontalSlider(value, min, max);
        var newValue = _snapping ? Mathf.Round(newValueRaw / snapAmount) * snapAmount : newValueRaw;
        GUILayout.Label($"360", GUILayout.Width(40));
        var newTextValue = GUILayout.TextField(textValue, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        if (newValue != value)
        {
            value = newValue;
            textValue = newValue.ToString();
            onChange();
            return;
        }
        if (newTextValue != textValue)
        {
            textValue = newTextValue;
            if (float.TryParse(newTextValue, out float floatResult))
            {
                if (floatResult != value)
                {
                    value = floatResult;
                    onChange();
                }
            }
        }
    }

    void DrawConeScaleInput(string label, ref string textValue, ref float value, Action onChange)
    {
        var min = 0.1f;
        var max = 5;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: ", GUILayout.Width(50));
        GUILayout.Label($"{min}m", GUILayout.Width(40));
        var newValueRaw = GUILayout.HorizontalSlider(value, min, max);
        var newValue = _snapping ? Mathf.Round(newValueRaw / 0.05f) * 0.05f : newValueRaw;
        GUILayout.Label($"{max}m", GUILayout.Width(40));
        var newTextValue = GUILayout.TextField(textValue, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        if (newValue != value)
        {
            value = newValue;
            textValue = newValue.ToString();
            onChange();
            return;
        }
        if (newTextValue != textValue)
        {
            textValue = newTextValue;
            if (float.TryParse(newTextValue, out float floatResult))
            {
                if (floatResult != value)
                {
                    value = floatResult;
                    onChange();
                }
            }
        }
    }

    void DrawSettings()
    {
        _snapping = GUILayout.Toggle(_snapping, "Snapping");

        if (GUILayout.Button("Remove All Cones From All Trains"))
        {
            RemoveAllConesFromAllTrains();
        }

        var newIsVisible = GUILayout.Toggle(_isVisible, "Cones visible");

        if (newIsVisible != _isVisible)
        {
            ToggleVisibility(newIsVisible);

            _isVisible = newIsVisible;
        }
    }

    void ToggleVisibility(bool isNowVisible)
    {
        Logger.Log($"[Panel] Toggle visibility => {isNowVisible}");

        var target = GetConeTargetTransform();
        if (target == null)
            return;

        var cones = ConeHelper.GetCones(target);

        foreach (var cone in cones)
            cone.IsVisible = isNowVisible;
    }

    public void HydrateStandardCones(Transform? target)
    {
        if (target == null)
            return;

        // TODO: move to manager

        var cones = ConeHelper.GetCones(target);

        var standardCones = cones.Where(x => x.side != null);

        foreach (var cone in standardCones)
        {
            switch (cone.side)
            {
                case StandardSide.Front:
                    cone.settings.PositionX = _frontPositionX;
                    cone.settings.PositionY = _frontPositionY;
                    cone.settings.PositionZ = _frontPositionZ;
                    break;
                case StandardSide.Rear:
                    cone.settings.PositionX = _rearPositionX;
                    cone.settings.PositionY = _rearPositionY;
                    cone.settings.PositionZ = _rearPositionZ;
                    break;
            }

            ConeHelper.ApplyOffsetsToCone(cone.transform, cone.settings);
        }
    }

    public void AddStandardRearCone()
    {
        Logger.Log("[Panel] Adding standard rear cone...");

        var target = GetConeTargetInfo();
        if (target == null)
            return;

        var position = GetStandardRearConePosition(target.Value.trainCar);

        if (position == null)
            position = TrainCarHelper.GetApproxStandardRearConePosition(target.Value.trainCar);

        if (position == null)
            position = new Vector3(2f, 0, 3f);

        var settingsRight = new ConeSettings()
        {
            PositionX = position.Value.x,
            PositionY = position.Value.y,
            PositionZ = position.Value.z
        };

        _rearPositionX = settingsRight.PositionX;
        _rearPositionY = settingsRight.PositionY;
        _rearPositionZ = settingsRight.PositionZ;

        var addedComponent = ConeHelper.AddCone(target.Value.transform, target.Value.trainCar, settingsRight);
        addedComponent.side = StandardSide.Rear;
    }

    public void AddStandardFrontCone()
    {
        Logger.Log("[Panel] Adding standard front cone...");

        var target = GetConeTargetInfo();
        if (target == null)
            return;

        var position = GetStandardFrontConePosition(target.Value.trainCar);

        if (position == null)
            position = TrainCarHelper.GetApproxStandardFrontConePosition(target.Value.trainCar);

        if (position == null)
            position = new Vector3(2f, 0, 3f);

        var settingsRight = new ConeSettings()
        {
            PositionX = position.Value.x,
            PositionY = position.Value.y,
            PositionZ = position.Value.z,
            RotationY = 180
        };

        _frontPositionX = settingsRight.PositionX;
        _frontPositionY = settingsRight.PositionY;
        _frontPositionZ = settingsRight.PositionZ;

        var addedComponent = ConeHelper.AddCone(target.Value.transform, target.Value.trainCar, settingsRight);
        addedComponent.side = StandardSide.Front;
    }

    public void RemoveAllConesFromCurrentTrain()
    {
        Logger.Log("[Panel] Removing all cones from current train...");

        var target = GetConeTargetInfo();
        if (target == null)
            return;

        ConeHelper.RemoveAllCones(target.Value.transform);
    }

    public void RemoveAllConesFromAllTrains()
    {
        Logger.Log("[Panel] Removing all cones from ALL...");

        ConeHelper.RemoveAllCones();
    }
}