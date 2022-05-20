using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DiverScriptableObject", menuName = "Scriptable Objects/DiverSettings")]
public class DiveSettings : ScriptableObject {

    [field: Header("Environment Values")]
    [Range(-5, 40)] public float waterTemp = 15f;                           // Celcius
    [Range(1000f, 1040f)] public float waterDensity = 1025f;                // Kg/m3
    [Range(850, 1100)] public float atmosphericPressure = 1013f;            // Air pressure in hPa (same as millibar)

    [field: Header("Player Values")]
    [Range(100, 250)] public float diverHeight = 180f;        // cm
    [Range(20, 200)] public float diverWeight = 80f;          // kg
    [Range(10, 20)] public float RMV = 15f;                   // l, Respiratory minute volume
    [Range(1, 10)] public float suitThickness = 5f;           // mm

    public enum tankMaterials { Aluminium, Steel };
    public tankMaterials tankMaterial = tankMaterials.Steel;  // Aluminium or Steel
    public int matValue = 0;

    [Range(3, 30)] public float tankEmptyWeight = 12f;         // kg
    [Range(100, 500)] public float tankStartPress = 300;       // bar
    [Range(3, 20)] public float tankCapacity = 10f;            // liter
    [Range(0f, 50f)] public float leadWeights = 0f;            // kg
    [Range(1f, 6f)] public float BCD_weight = 3.5f;            // kg
    [Range(5, 40)] public float BCD_Capacity = 15f;            // liter
}
