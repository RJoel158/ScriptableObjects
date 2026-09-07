using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerHealth))]
public class Player : MonoBehaviour
{
    public Transform hand;
    public GameObject equipedItem;

    public PlayerController Controller { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public PlayerHealth Health { get; private set; }

    void Awake()
    {
        Controller = GetComponent<PlayerController>();
        if (Controller == null) Controller = gameObject.AddComponent<PlayerController>();

        Combat = GetComponent<PlayerCombat>();
        if (Combat == null) Combat = gameObject.AddComponent<PlayerCombat>();

        Health = GetComponent<PlayerHealth>();
        if (Health == null) Health = gameObject.AddComponent<PlayerHealth>();

        // Alinear el modelo hijo del soldado exactamente a los pies del Player
        Transform soldier = transform.Find("Soldier");
        if (soldier != null)
        {
            soldier.localPosition = Vector3.zero;
            soldier.localRotation = Quaternion.identity;
            soldier.localScale = Vector3.one;
        }

        // Configurar CharacterController para que calce 100% con la altura y anchura del soldado
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.center = new Vector3(0f, 0.92f, 0f);
            cc.height = 1.85f;
            cc.radius = 0.35f;
            cc.skinWidth = 0.08f;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0.001f;
        }

        // Si hay un CapsuleCollider duplicado en la raíz, desactivarlo para evitar conflicto de físicas
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Ocultar o eliminar la cápsula blanca residual
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.enabled = false;
        }
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && soldier != null)
        {
            // Ocultar mesh de la cápsula
            Destroy(mr);
            Destroy(mf);
        }

        // Verificar o configurar Animator en el modelo hijo (Soldier)
        Animator anim = GetComponentInChildren<Animator>();
        if (anim == null)
        {
            if (soldier != null)
            {
                anim = soldier.gameObject.AddComponent<Animator>();
                Debug.LogWarning("[Player] Animator añadido automáticamente al objeto 'Soldier'. Asigna el Controller 'Player_AnimatorController' y su Avatar en el Inspector.");
            }
            else
            {
                anim = gameObject.AddComponent<Animator>();
            }
        }

        if (anim != null)
        {
            anim.applyRootMotion = false;
        }

        if (Combat != null)
        {
            Combat.AutoDetectRightHand();
        }
    }
}
