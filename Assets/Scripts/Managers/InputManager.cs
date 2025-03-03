using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    public InputSystem_Actions PlayerInput;

    public override void Awake()
    {
        base.Awake();
    }

    public void OnEnable()
    {
        PlayerInput = new InputSystem_Actions();
        PlayerInput.Enable();
    }

    void Reset() { }
}
