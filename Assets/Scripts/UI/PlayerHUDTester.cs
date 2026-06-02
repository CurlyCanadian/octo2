using UnityEngine;

public class PlayerHUDTester : MonoBehaviour
{
    [SerializeField] private PlayerHUD hud;

    private int health = 3;
    private int inkCharges = 5;
    private bool hidden;
    private bool armUsed;
    private bool interactable;

    private void Start()
    {
        hud.SetHealth(health, 3);
        hud.SetInkCharges(inkCharges, 5);
        hud.SetHidden(false);
        hud.SetGrabArmUsed(false);
        hud.SetCrosshairInteractable(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            health--;

            if (health < 0)
                health = 3;

            hud.SetHealth(health, 3);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            inkCharges--;

            if (inkCharges < 0)
                inkCharges = 5;

            hud.SetInkCharges(inkCharges, 5);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            hidden = !hidden;
            hud.SetHidden(hidden);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            armUsed = !armUsed;
            hud.SetGrabArmUsed(armUsed);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            interactable = !interactable;
            hud.SetCrosshairInteractable(interactable);
        }
    }
}