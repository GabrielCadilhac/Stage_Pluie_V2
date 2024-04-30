using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChangeCommon))]
public class CommonEditor : Editor
{
    // Super primitive
    private SerializedProperty _energyStrengthProp, _energySpeedProp;
    private SerializedProperty _coeffDissipProp, _coeffTransfertProp;

    // Energy Cascade
    private SerializedProperty _meanEnergyProp, _stdEnergyPrimProp;
    private SerializedProperty _minSizeTallProp, _minSizeMediumProp, _minSizeSmallProp;

    private ChangeCommon _common;

    void OnEnable()
    {
        _energyStrengthProp = serializedObject.FindProperty("_energyStrength");
        _energySpeedProp = serializedObject.FindProperty("_energySpeed");
        _coeffDissipProp = serializedObject.FindProperty("_coeffDissip");
        _coeffTransfertProp = serializedObject.FindProperty("_coeffTransfert");

        _meanEnergyProp = serializedObject.FindProperty("_meanEnergy");
        _stdEnergyPrimProp = serializedObject.FindProperty("_stdEnergyPrim");
        _minSizeTallProp = serializedObject.FindProperty("_minSizeTall");
        _minSizeMediumProp = serializedObject.FindProperty("_minSizeMedium");
        _minSizeSmallProp = serializedObject.FindProperty("_minSizeSmall");

        _common = target as ChangeCommon;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("Energy cascade", EditorStyles.boldLabel);
        EditorGUILayout.Slider(_energyStrengthProp, 1f, 5f);
        EditorGUILayout.Slider(_energySpeedProp, 0.1f, 0.4f);
        EditorGUILayout.Slider(_coeffDissipProp, 0.001f, 0.1f);
        EditorGUILayout.Slider(_coeffTransfertProp, 0.01f, 0.3f);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Energy cascade", EditorStyles.boldLabel);
        EditorGUILayout.Slider(_meanEnergyProp, 0.1f, 0.5f);
        EditorGUILayout.Slider(_stdEnergyPrimProp, 0f, 1f);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_minSizeTallProp);
        EditorGUILayout.PropertyField(_minSizeMediumProp);
        EditorGUILayout.PropertyField(_minSizeSmallProp);

        if (EditorGUI.EndChangeCheck())
            _common.ChangeConstants();

        serializedObject.ApplyModifiedProperties();
    }
}
