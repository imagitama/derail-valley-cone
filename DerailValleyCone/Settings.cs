using System;
using System.Collections.Generic;
using DerailValleyBindingHelper;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyCone;

[Serializable]
public class ConePreset
{
    public string CarName;
    public List<ConeSettings> Cones;
}

public class Settings : UnityModManager.ModSettings, IDrawable
{
    [Draw(Label = "Threshold for derailment (default 10,000)")]
    public float DerailThreshold = 10000f;
    [Draw(Label = "Amount of force to 'toss away' derailed cars (0 to disable, default 500,000)")]
    public float TossAwayForce = 500000f;
    [Draw(Label = "If to also derail the entire train set")]
    public bool DerailTrainset = true;
    public List<ConePreset> Presets = [];

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    public void OnChange()
    {
    }
}
