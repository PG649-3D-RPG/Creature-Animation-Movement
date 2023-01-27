using UnityEngine;
using Config;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

public class TrainingSettingsOverrider : MonoBehaviour{

    #if UNITY_EDITOR
    [SerializeField]
    private TrainingSettings settings;

    [SerializeField]
    private TrainingSettings defaultSettings;

    private MlAgentConfig mlAgentConfig;
    private CreatureConfig creatureConfig;
    private AdvancedEnvironmentGenerator generator;

    public void Reset(){
        mlAgentConfig = this.gameObject.GetComponent<MlAgentConfig>();
        creatureConfig = this.gameObject.GetComponent<CreatureConfig>();
        generator = this.gameObject.GetComponent<AdvancedEnvironmentGenerator>();
        if(mlAgentConfig == null || creatureConfig == null || generator == null) Debug.LogError("Was not able to retrieve config behaviours");
    }

    public void OverrideSettings() => OverrideSettings(this.settings);
    public void OverrideSettings(TrainingSettings settings){
        if(settings == null || generator == null || creatureConfig == null || mlAgentConfig == null) return;

        Undo.RecordObject(generator, "Override Generator");

        generator.AgentScriptName = settings.AgentName;
        generator.CreaturePrefab = settings.CreaturePrefab;
        generator.amountCreatures = settings.AmountCreatures;

        Undo.RecordObject(creatureConfig, "Override Creature Config");
        creatureConfig.seed = settings.Seed;
        creatureConfig.creatureType = settings.CreatureType;
        creatureConfig.ResetOnGroundContactParts = new List<BoneCategory>(settings.ResetOnGroundContactParts);

        Undo.RecordObject(mlAgentConfig, "Override MlAgentConfig");
        mlAgentConfig.RandomizeWalkSpeedEachEpisode = settings.RandomizeWalkSpeedEachEpisode;
        mlAgentConfig.MaxStep = settings.MaxStep;

        Debug.Log("Override of settings successful");
    }

    public void StoreDefault(){
        if(defaultSettings == null || generator == null || creatureConfig == null || mlAgentConfig == null) return;

        Undo.RecordObject(defaultSettings, "Store default settings");
        defaultSettings.AgentName = generator.AgentScriptName;
        defaultSettings.CreaturePrefab = generator.CreaturePrefab;
        defaultSettings.AmountCreatures = generator.amountCreatures;

        defaultSettings.Seed = creatureConfig.seed;
        defaultSettings.CreatureType = creatureConfig.creatureType;
        defaultSettings.ResetOnGroundContactParts = new List<BoneCategory>(creatureConfig.ResetOnGroundContactParts);

        defaultSettings.RandomizeWalkSpeedEachEpisode = mlAgentConfig.RandomizeWalkSpeedEachEpisode;
        defaultSettings.MaxStep = mlAgentConfig.MaxStep;
     
        Debug.Log("Wrote current settings to default");
    }

    public void ResetToDefault() => OverrideSettings(this.defaultSettings);
    #endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TrainingSettingsOverrider))]
public class TrainingSettingsOverriderEditor : Editor{

    private TrainingSettingsOverrider overrider;

    public void OnEnable(){
        overrider = target as TrainingSettingsOverrider;
        overrider.Reset();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if(GUILayout.Button("Override Settings")){
            overrider.OverrideSettings();
        }
        if(GUILayout.Button("Store Default Settings")){
            overrider.StoreDefault();
        }
        if(GUILayout.Button("Reset to Default Settings")){
            overrider.ResetToDefault();
        }

    }
}
#endif