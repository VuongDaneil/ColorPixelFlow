using System;
using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableObjects/ColorPallete", menuName = "ScriptableObjects/ColorPallete")]
public class ColorPalleteData : ScriptableObject
{
    [SerializedDictionary("Color Pallete")]
    public SerializedDictionary<string, Color> colorPallete = new SerializedDictionary<string, Color>();
    
    public Color GetColorByCode(string code)
    {
        return colorPallete[code];
    }
}