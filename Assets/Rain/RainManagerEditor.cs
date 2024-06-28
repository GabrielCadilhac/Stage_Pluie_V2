using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RainManager))]
public class RainManagerEditor : Editor
{
    private SerializedProperty _bezierCurveProp, _showGizmosProp, _nbParticlesProp;
    private SerializedProperty _hodographProp;

    // Wind parameters
    private SerializedProperty _localWindForceProp, _windShearStrengthProp, _deltaTimeProp, _globalWindProp;
    private SerializedProperty _windShearShaderProp, _localWindShaderProp;

    // Rain parameters
    private SerializedProperty _forceRotationProp;
    private SerializedProperty _updateShaderProp, _collisionShaderProp;

    // Splash parameters
    private SerializedProperty _splashPlaneProp;

    private RainManager _rainManager;

    // Test
    private SerializedProperty _test, _globalMinProp;
    
    void OnEnable()
    {
        _bezierCurveProp     = serializedObject.FindProperty("_bezierCurve");
        _hodographProp       = serializedObject.FindProperty("_hodograph");
        _nbParticlesProp     = serializedObject.FindProperty("_nbParticles");
        _localWindForceProp  = serializedObject.FindProperty("_localWindForce");
        _windShearStrengthProp = serializedObject.FindProperty("_windShearStrength");
        _globalWindProp      = serializedObject.FindProperty("_globalWind");
        _deltaTimeProp       = serializedObject.FindProperty("_deltaTime");
        _forceRotationProp   = serializedObject.FindProperty("_forceRotation");
        _updateShaderProp    = serializedObject.FindProperty("_updateShader");
        _collisionShaderProp = serializedObject.FindProperty("_collisionShader");
        _showGizmosProp      = serializedObject.FindProperty("_showGizmos");
        _splashPlaneProp     = serializedObject.FindProperty("_splashPlane");
        _windShearShaderProp = serializedObject.FindProperty("_windShearShader");
        _localWindShaderProp = serializedObject.FindProperty("_localWindShader");
        _globalMinProp = serializedObject.FindProperty("_globalMin");

        _test = serializedObject.FindProperty("_test");

        _rainManager = target as RainManager;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("General properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_nbParticlesProp);
        EditorGUILayout.PropertyField(_showGizmosProp);
        EditorGUILayout.PropertyField(_bezierCurveProp);
        EditorGUILayout.PropertyField(_hodographProp);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Wind properties", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_localWindForceProp);
        if (EditorGUI.EndChangeCheck())
            _rainManager.LocalWindForceChanged();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_windShearStrengthProp);
        EditorGUILayout.PropertyField(_globalWindProp);
        if (EditorGUI.EndChangeCheck())
            _rainManager.GlobalWindForceChanged();

        EditorGUILayout.PropertyField(_windShearShaderProp);
        EditorGUILayout.PropertyField(_localWindShaderProp);

        EditorGUILayout.PropertyField(_deltaTimeProp);
        EditorGUILayout.PropertyField(_globalMinProp);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Rain properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_forceRotationProp);
        EditorGUILayout.PropertyField(_updateShaderProp);
        EditorGUILayout.PropertyField(_collisionShaderProp);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Splash properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_splashPlaneProp);

        EditorGUILayout.PropertyField(_test);

        serializedObject.ApplyModifiedProperties();
    }
}
