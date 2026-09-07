#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerAnimatorLayerSetup
{
    private const string MaskPath = "Assets/08_Animations/UpperBodyMask.mask";
    private const string ControllerPath = "Assets/08_Animations/Player_AnimatorController.controller";

    static PlayerAnimatorLayerSetup()
    {
        EditorApplication.delayCall += () =>
        {
            SetupUpperBodySystem(false);
        };
    }

    [MenuItem("RPG Survival/🎭 Configurar Capas de Animación (UpperBody Mask)", false, 30)]
    public static void ManualSetup()
    {
        SetupUpperBodySystem(true);
    }

    public static void SetupUpperBodySystem(bool showDialog)
    {
        // 1. Crear / Asegurar AvatarMask para UpperBody
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            mask.name = "UpperBodyMask";
            
            // Desactivar tren inferior (raíz, caderas, piernas, pies)
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);

            // Activar tren superior (tronco, cabeza, brazos, manos)
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);

            AssetDatabase.CreateAsset(mask, MaskPath);
            AssetDatabase.SaveAssets();
        }

        // 2. Cargar AnimatorController
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Aviso", "No se encontró Player_AnimatorController en " + ControllerPath, "OK");
            return;
        }

        // Cargar clips de animación necesarios
        AnimationClip reloadClip = LoadAnimationClip("Assets/08_Animations/Basic Shooter Pack/reloading.fbx");
        AnimationClip hitClip = LoadAnimationClip("Assets/08_Animations/Basic Shooter Pack/hit reaction.fbx");
        AnimationClip shootClip = LoadAnimationClip("Assets/08_Animations/Basic Shooter Pack/firing rifle.fbx");

        // 3. Limpiar transiciones de Reload y Hit del Base Layer (para que nunca congelen las piernas)
        AnimatorControllerLayer[] layers = controller.layers;
        if (layers.Length > 0)
        {
            var baseSm = layers[0].stateMachine;
            var anyTransitions = baseSm.anyStateTransitions;
            for (int i = anyTransitions.Length - 1; i >= 0; i--)
            {
                var tr = anyTransitions[i];
                if (tr.destinationState != null && 
                   (tr.destinationState.name.Equals("Player_Reload") || tr.destinationState.name.Equals("Player_Hit")))
                {
                    baseSm.RemoveAnyStateTransition(tr);
                }
            }
        }

        // 4. Buscar o crear la capa "UpperBody"
        int upperLayerIdx = -1;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name.Equals("UpperBody") || layers[i].name.Equals("UpperBody Layer"))
            {
                upperLayerIdx = i;
                break;
            }
        }

        AnimatorStateMachine upperSm;
        if (upperLayerIdx < 0)
        {
            controller.AddLayer("UpperBody");
            layers = controller.layers;
            upperLayerIdx = layers.Length - 1;
        }

        layers[upperLayerIdx].avatarMask = mask;
        layers[upperLayerIdx].defaultWeight = 1.0f;
        layers[upperLayerIdx].blendingMode = AnimatorLayerBlendingMode.Override;
        controller.layers = layers;

        upperSm = controller.layers[upperLayerIdx].stateMachine;

        // Limpiar estados previos de UpperBody para asegurar configuración limpia
        var existingStates = upperSm.states;
        AnimatorState upperEmptyState = null;
        AnimatorState upperReloadState = null;
        AnimatorState upperHitState = null;
        AnimatorState upperShootState = null;

        foreach (var childState in existingStates)
        {
            if (childState.state.name == "Upper_Empty" || childState.state.name == "Upper_Locomotion") upperEmptyState = childState.state;
            else if (childState.state.name == "Upper_Reload") upperReloadState = childState.state;
            else if (childState.state.name == "Upper_Hit") upperHitState = childState.state;
            else if (childState.state.name == "Upper_Shoot") upperShootState = childState.state;
        }

        if (upperEmptyState == null)
        {
            upperEmptyState = upperSm.AddState("Upper_Empty", new Vector3(300, 100, 0));
            upperEmptyState.motion = null; // Pase libre para el Base Layer
            upperSm.defaultState = upperEmptyState;
        }

        if (upperReloadState == null)
        {
            upperReloadState = upperSm.AddState("Upper_Reload", new Vector3(560, 40, 0));
        }
        if (reloadClip != null) upperReloadState.motion = reloadClip;
        upperReloadState.writeDefaultValues = true;

        if (upperHitState == null)
        {
            upperHitState = upperSm.AddState("Upper_Hit", new Vector3(560, 120, 0));
        }
        if (hitClip != null) upperHitState.motion = hitClip;
        upperHitState.writeDefaultValues = true;

        if (upperShootState == null)
        {
            upperShootState = upperSm.AddState("Upper_Shoot", new Vector3(560, 200, 0));
        }
        if (shootClip != null) upperShootState.motion = shootClip;
        upperShootState.writeDefaultValues = true;

        // Configurar Transiciones AnyState en UpperBody
        EnsureAnyTransition(upperSm, upperReloadState, "Reload", AnimatorConditionMode.If, 0f, 0.08f);
        EnsureAnyTransition(upperSm, upperHitState, "Hit", AnimatorConditionMode.If, 0f, 0.08f);
        EnsureAnyTransition(upperSm, upperShootState, "Shoot", AnimatorConditionMode.If, 0f, 0.05f);

        // Configurar Transiciones de Regreso a Upper_Empty
        EnsureExitTransition(upperReloadState, upperEmptyState, 0.90f, 0.15f);
        EnsureExitTransition(upperHitState, upperEmptyState, 0.85f, 0.15f);
        EnsureExitTransition(upperShootState, upperEmptyState, 0.70f, 0.08f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Animaciones Separadas", 
                "¡Capa 'UpperBody' configurada exitosamente!\n\n" +
                "• El tren inferior (piernas/locomoción) seguirá caminando y corriendo fluidamente con WASD.\n" +
                "• El tren superior reproducirá la recarga (Reload), disparo y reacciones de impacto sin congelar el movimiento.",
                "¡Excelente!");
        }
    }

    private static void EnsureAnyTransition(AnimatorStateMachine sm, AnimatorState targetState, string triggerName, AnimatorConditionMode mode, float threshold, float duration)
    {
        foreach (var tr in sm.anyStateTransitions)
        {
            if (tr.destinationState == targetState) return;
        }

        var transition = sm.AddAnyStateTransition(targetState);
        transition.canTransitionToSelf = true;
        transition.duration = duration;
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.AddCondition(mode, threshold, triggerName);
    }

    private static void EnsureExitTransition(AnimatorState sourceState, AnimatorState targetState, float exitTime, float duration)
    {
        foreach (var tr in sourceState.transitions)
        {
            if (tr.destinationState == targetState) return;
        }

        var transition = sourceState.AddTransition(targetState);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
    }

    private static AnimationClip LoadAnimationClip(string fbxPath)
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
