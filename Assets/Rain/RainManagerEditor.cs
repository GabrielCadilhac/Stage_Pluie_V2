using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RainManager))]
public class RainManagerEditor : Editor
{
    private SerializedProperty _bezierCurveProp, _showGizmosProp, _nbParticlesProp;

    // Wind parameters
    private SerializedProperty _globalWindProp;
    private SerializedProperty _localWindForceProp, _deltaTimeProp, _primitiveSpeedProp;

    // Rain parameters
    private SerializedProperty _forceRotationProp;
    private SerializedProperty _updateShaderProp, _collisionShaderProp;

    // Splash parameters
    private SerializedProperty _splashPlaneProp;

    private RainManager _rainManager;
    
    void OnEnable()
    {
        _bezierCurveProp     = serializedObject.FindProperty("_bezierCurve");
        _globalWindProp      = serializedObject.FindProperty("_globalWind");
        _nbParticlesProp     = serializedObject.FindProperty("_nbParticles");
        _localWindForceProp  = serializedObject.FindProperty("_localWindForce");
        _deltaTimeProp       = serializedObject.FindProperty("_deltaTime");
        _primitiveSpeedProp  = serializedObject.FindProperty("_primitiveSpeed");
        _forceRotationProp   = serializedObject.FindProperty("_forceRotation");
        _updateShaderProp    = serializedObject.FindProperty("_updateShader");
        _collisionShaderProp = serializedObject.FindProperty("_collisionShader");
        _showGizmosProp      = serializedObject.FindProperty("_showGizmos");
        _splashPlaneProp     = serializedObject.FindProperty("_splashPlane");

        _rainManager = target as RainManager;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("General properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_nbParticlesProp);
        EditorGUILayout.PropertyField(_showGizmosProp);
        EditorGUILayout.PropertyField(_bezierCurveProp);

        EditorGUILayout.LabelField("Wind properties", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_localWindForceProp);
        if (EditorGUI.EndChangeCheck())
            _rainManager.LocalWindForceChanged();
        
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_globalWindProp);
        if (EditorGUI.EndChangeCheck())
            _rainManager.GlobalWindChanged();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_primitiveSpeedProp);
        if (EditorGUI.EndChangeCheck())
            _rainManager.PrimitiveSpeedChanged();

        EditorGUILayout.PropertyField(_deltaTimeProp);

        EditorGUILayout.LabelField("Rain properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_forceRotationProp);
        EditorGUILayout.PropertyField(_updateShaderProp);
        EditorGUILayout.PropertyField(_collisionShaderProp);

        EditorGUILayout.LabelField("Splash properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_splashPlaneProp);

        serializedObject.ApplyModifiedProperties();
    }
}
