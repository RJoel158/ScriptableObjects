#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class MutantBossAnimatorSetup
{
    private const string ControllerPath = "Assets/MonsterMutant 7/MonsterMutant7 Animator Controller.controller";
    private const string AnimFolder = "Assets/MonsterMutant 7/Animations/";

    static MutantBossAnimatorSetup()
    {
        EditorApplication.delayCall += () =>
        {
            SetupBossAnimator(false);
        };
    }

    [MenuItem("RPG Survival/👹 Configurar Animator del Jefe Mutante", false, 40)]
    public static void ManualSetup()
    {
        SetupBossAnimator(true);
        AutoBakeMutantBossPrefab(true);
    }

    [MenuItem("RPG Survival/⚙️ Pre-Vincular Puntos de Espinas y Animator en Prefab", false, 41)]
    public static void ManualBakePrefab()
    {
        AutoBakeMutantBossPrefab(true);
    }

    public static void AutoBakeMutantBossPrefab(bool showDialog)
    {
        string prefabPath = "Assets/03_Prefabs/Base mesh MonsterMutant7 skin1.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Aviso", "No se encontró el prefab en " + prefabPath, "OK");
            return;
        }

        MutantBossController bossCtrl = prefabRoot.GetComponent<MutantBossController>();
        if (bossCtrl == null) bossCtrl = prefabRoot.AddComponent<MutantBossController>();

        // Asignar ScriptableObject
        EnemyData bossData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/04_ScriptableObjects/Enemies/Enemy_MutantMonster.asset");

        // Vincular Animator y Puntos de Espinas automáticamente
        bossCtrl.AutoBindComponentsAndSpikePoints();

        // Guardar prefab pre-horneado
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Prefab Pre-Configurado", 
                "¡Prefab del Jefe Mutante pre-configurado exitosamente!\n\n" +
                "• Animator vinculado.\n" +
                "• Big Spikes (Púas Gigantes Izquierda y Derecha) y Brazos auto-asignados.\n" +
                "• Cero búsqueda en tiempo de ejecución (0 Roaming).", 
                "¡Excelente!");
        }
    }

    public static void SetupBossAnimator(bool showDialog)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Aviso", "No se encontró el Controller en " + ControllerPath, "OK");
            return;
        }

        // Cargar clips
        AnimationClip idleClip = LoadClip(AnimFolder + "MutantMonster2@idle1.fbx");
        AnimationClip walkClip = LoadClip(AnimFolder + "MutantMonster2@walk2.fbx");
        AnimationClip runClip = LoadClip(AnimFolder + "MutantMonster2@run1.fbx");
        AnimationClip runFastClip = LoadClip(AnimFolder + "MutantMonster2@run3.fbx");
        AnimationClip strafeLClip = LoadClip(AnimFolder + "MutantMonster2@strafeleft.fbx");
        AnimationClip strafeRClip = LoadClip(AnimFolder + "MutantMonster2@straferight.fbx");
        AnimationClip attackMeleeClip = LoadClip(AnimFolder + "MutantMonster2@attack2.fbx");
        AnimationClip attackSpikeClip = LoadClip(AnimFolder + "MutantMonster2@attack2RLSpike.fbx");
        if (attackSpikeClip == null) attackSpikeClip = LoadClip(AnimFolder + "MutantMonster2@attack1RSpike.fbx");
        AnimationClip jumpClip = LoadClip(AnimFolder + "MutantMonster2@jump.fbx");
        AnimationClip rageClip = LoadClip(AnimFolder + "MutantMonster2@rage.fbx");
        AnimationClip hitClip = LoadClip(AnimFolder + "MutantMonster2@gethit1.fbx");
        AnimationClip deathClip = LoadClip(AnimFolder + "MutantMonster2@death1.fbx");

        // Asegurar Parámetros
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "isMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Strafe", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "SpikeAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Rage", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Phase", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "DeathIndex", AnimatorControllerParameterType.Int);

        var rootSm = controller.layers[0].stateMachine;

        // Buscar o crear estados
        AnimatorState idleState = FindOrCreateState(rootSm, "Boss_Idle", idleClip, new Vector3(300, 100, 0));
        AnimatorState moveState = FindOrCreateState(rootSm, "Boss_Run", runClip, new Vector3(300, 180, 0));
        AnimatorState attackState = FindOrCreateState(rootSm, "Boss_Attack", attackMeleeClip, new Vector3(560, 40, 0));
        AnimatorState spikeState = FindOrCreateState(rootSm, "Boss_SpikeAttack", attackSpikeClip, new Vector3(560, 110, 0));
        AnimatorState jumpState = FindOrCreateState(rootSm, "Boss_Jump", jumpClip, new Vector3(560, 180, 0));
        AnimatorState rageState = FindOrCreateState(rootSm, "Boss_Rage", rageClip, new Vector3(560, 250, 0));
        AnimatorState hitState = FindOrCreateState(rootSm, "Boss_Hit", hitClip, new Vector3(560, 320, 0));
        AnimatorState dieState = FindOrCreateState(rootSm, "Boss_Die", deathClip, new Vector3(560, 390, 0));

        rootSm.defaultState = idleState;

        // Transición Idle <-> Run
        EnsureTransition(idleState, moveState, "Speed", AnimatorConditionMode.Greater, 0.1f, 0.15f);
        EnsureTransition(moveState, idleState, "Speed", AnimatorConditionMode.Less, 0.1f, 0.15f);

        // Transiciones AnyState
        EnsureAnyTransition(rootSm, attackState, "Attack", 0.1f);
        EnsureAnyTransition(rootSm, spikeState, "SpikeAttack", 0.1f);
        EnsureAnyTransition(rootSm, jumpState, "Jump", 0.1f);
        EnsureAnyTransition(rootSm, rageState, "Rage", 0.1f);
        EnsureAnyTransition(rootSm, hitState, "Hit", 0.05f);
        EnsureAnyTransition(rootSm, dieState, "Die", 0.08f, canSelf: false);

        // Retorno a Idle tras ataques
        EnsureExitTransition(attackState, idleState, 0.85f, 0.15f);
        EnsureExitTransition(spikeState, idleState, 0.85f, 0.15f);
        EnsureExitTransition(jumpState, idleState, 0.90f, 0.15f);
        EnsureExitTransition(rageState, idleState, 0.90f, 0.15f);
        EnsureExitTransition(hitState, idleState, 0.80f, 0.12f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Animator Configurado", "¡El Animator Controller del Jefe Mutante ha sido actualizado exitosamente con todas sus animaciones de combate por fases!", "¡Excelente!");
        }
    }

    private static void EnsureParameter(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in ctrl.parameters)
        {
            if (p.name.Equals(name)) return;
        }
        ctrl.AddParameter(name, type);
    }

    private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string name, Motion motion, Vector3 pos)
    {
        foreach (var cs in sm.states)
        {
            if (cs.state.name.Equals(name))
            {
                if (motion != null) cs.state.motion = motion;
                return cs.state;
            }
        }
        AnimatorState st = sm.AddState(name, pos);
        st.motion = motion;
        st.writeDefaultValues = true;
        return st;
    }

    private static void EnsureTransition(AnimatorState from, AnimatorState to, string param, AnimatorConditionMode mode, float threshold, float duration)
    {
        foreach (var tr in from.transitions)
        {
            if (tr.destinationState == to) return;
        }
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.hasFixedDuration = true;
        transition.AddCondition(mode, threshold, param);
    }

    private static void EnsureAnyTransition(AnimatorStateMachine sm, AnimatorState to, string trigger, float duration, bool canSelf = true)
    {
        foreach (var tr in sm.anyStateTransitions)
        {
            if (tr.destinationState == to) return;
        }
        var transition = sm.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = canSelf;
        transition.AddCondition(AnimatorConditionMode.If, 0, trigger);
    }

    private static void EnsureExitTransition(AnimatorState from, AnimatorState to, float exitTime, float duration)
    {
        foreach (var tr in from.transitions)
        {
            if (tr.destinationState == to) return;
        }
        var transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null) return null;
        foreach (var a in assets)
        {
            if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }
}
#endif
