using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RainManager))]
public class RainManagerEditor : Editor
{
    private SerializedProperty _boxProp, _bezierCurveProp, _showGizmosProp;

    // Wind parameters
    private SerializedProperty _globalWindProp;
    private SerializedProperty _localWindForceProp, _deltaTimeProp;

    // Rain parameters
    private SerializedProperty _forceRotationProp;

    private SerializedProperty _updateShaderProp, _collisionShaderProp;

    private RainManager _rainManager;
    void OnEnable()
    {
        _boxProp             = serializedObject.FindProperty("_box");
        _bezierCurveProp     = serializedObject.FindProperty("_bezierCurve");
        _globalWindProp      = serializedObject.FindProperty("_globalWind");
        _localWindForceProp  = serializedObject.FindProperty("_localWindForce");
        _deltaTimeProp       = serializedObject.FindProperty("_deltaTime");
        _forceRotationProp   = serializedObject.FindProperty("_forceRotation");
        _updateShaderProp    = serializedObject.FindProperty("_updateShader");
        _collisionShaderProp = serializedObject.FindProperty("_collisionShader");
        _showGizmosProp = serializedObject.FindProperty("_showGizmos");

        _rainManager = target as RainManager;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("General properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_showGizmosProp);
        EditorGUILayout.PropertyField(_boxProp);
        EditorGUILayout.PropertyField(_bezierCurveProp);

        EditorGUILayout.LabelField("Wind properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_localWindForceProp);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_globalWindProp);
        if (EditorGUI.EndChangeCheck())
            _rainManager.GlobalWindChanged();

        EditorGUILayout.PropertyField(_deltaTimeProp);

        EditorGUILayout.LabelField("Rain properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_forceRotationProp);
        EditorGUILayout.PropertyField(_updateShaderProp);
        EditorGUILayout.PropertyField(_collisionShaderProp);


        serializedObject.ApplyModifiedProperties();
    }
}
