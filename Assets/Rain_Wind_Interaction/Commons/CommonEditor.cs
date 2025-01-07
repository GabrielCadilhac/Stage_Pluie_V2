#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ChangeCommon))]
public class CommonEditor : Editor
{
    // Global
    private SerializedProperty _deltaTimeProp;

    private SerializedProperty _globalWindProp, _localWindStrengthProp;

    private SerializedProperty _drawDebugGridProp;

    private SerializedProperty _renderSplashProp;

    // Super primitive
    private SerializedProperty _energyStrengthProp, _energySpeedProp;
    private SerializedProperty _coeffDissipProp, _coeffTransfertProp;

    // Energy Cascade
    private SerializedProperty _meanEnergyProp, _stdEnergyPrimProp;
    private SerializedProperty _minSizeTallProp, _minSizeMediumProp, _minSizeSmallProp;

    // Debug
    private SerializedProperty _renderSphereProp, _sphereSizeProp; 

    private ChangeCommon _common;

    void OnEnable()
    {
        _energyStrengthProp = serializedObject.FindProperty("_energyStrength");
        _energySpeedProp    = serializedObject.FindProperty("_energySpeed");
        _coeffDissipProp    = serializedObject.FindProperty("_coeffDissip");
        _coeffTransfertProp = serializedObject.FindProperty("_coeffTransfert");

        _meanEnergyProp    = serializedObject.FindProperty("_meanEnergy");
        _stdEnergyPrimProp = serializedObject.FindProperty("_stdEnergyPrim");
        _minSizeTallProp   = serializedObject.FindProperty("_minSizeTall");
        _minSizeMediumProp = serializedObject.FindProperty("_minSizeMedium");
        _minSizeSmallProp  = serializedObject.FindProperty("_minSizeSmall");

        _renderSphereProp = serializedObject.FindProperty("_renderSphere");
        _sphereSizeProp   = serializedObject.FindProperty("_sphereSize");

        _deltaTimeProp    = serializedObject.FindProperty("_deltaTime");

        _globalWindProp        = serializedObject.FindProperty("_globalWind");
        _localWindStrengthProp = serializedObject.FindProperty("_localWindStrength");

        _drawDebugGridProp     = serializedObject.FindProperty("_drawDebugGrid");
        _renderSplashProp      = serializedObject.FindProperty("_renderSplash");

        _common = target as ChangeCommon;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(_deltaTimeProp);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_globalWindProp);
        EditorGUILayout.Slider(_localWindStrengthProp, 1f, 100f);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Energy cascade", EditorStyles.boldLabel);
        EditorGUILayout.Slider(_energyStrengthProp, 1f, 10f);
        EditorGUILayout.Slider(_energySpeedProp, 0.1f, 0.4f);
        EditorGUILayout.Slider(_coeffDissipProp, 0.001f, 0.5f);
        EditorGUILayout.Slider(_coeffTransfertProp, 0.01f, 0.3f);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Energy cascade", EditorStyles.boldLabel);
        EditorGUILayout.Slider(_meanEnergyProp, 0.1f, 0.5f);
        EditorGUILayout.Slider(_stdEnergyPrimProp, 0f, 1f);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_minSizeTallProp);
        EditorGUILayout.PropertyField(_minSizeMediumProp);
        EditorGUILayout.PropertyField(_minSizeSmallProp);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_renderSphereProp);
        EditorGUILayout.Slider(_sphereSizeProp, 1f, 100f);
        EditorGUILayout.PropertyField(_renderSplashProp);
        EditorGUILayout.PropertyField(_drawDebugGridProp);
        EditorGUILayout.Space();

        if (EditorGUI.EndChangeCheck())
            _common.ChangeConstants(_renderSphereProp.boolValue);

        serializedObject.ApplyModifiedProperties();
    }
}

#endif
