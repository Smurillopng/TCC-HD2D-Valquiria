//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.4.4
//     from Assets/TCCHD2D/Code/Scripts/GameControls.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @GameControls : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @GameControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""GameControls"",
    ""maps"": [
        {
            ""name"": ""Default"",
            ""id"": ""f55a841e-6cc9-4d2f-837f-dcc7182c7e1d"",
            ""actions"": [
                {
                    ""name"": ""Mouse"",
                    ""type"": ""Button"",
                    ""id"": ""eeef05cc-7363-4c34-8bb3-14f2a7fde63c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Run"",
                    ""type"": ""Button"",
                    ""id"": ""727bb1fd-5703-4224-8a60-d1ece41aaf1b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Walk"",
                    ""type"": ""Value"",
                    ""id"": ""5a9d1b69-05e9-4afe-905e-8b392d17916f"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Interact"",
                    ""type"": ""Button"",
                    ""id"": ""df4995fa-3458-417c-8c14-841b84b7540e"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""093b5232-4e03-439a-a865-8291bf7cf3e8"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""Mouse"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""222d48df-2647-437b-86f4-f619c4752a67"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""Run"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Walking"",
                    ""id"": ""f5a8ceaa-4a0b-4d85-b30d-d4d16de0eb21"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Walk"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""fc89e04a-3bf3-41c2-8f18-67d28ea25ebd"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""Walk"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""a60dd529-5144-4b71-a845-284a48dfbc22"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""Walk"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""959ff0c4-9350-4d9d-8d9e-c7a8aa0a69cb"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""Walk"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""09b7c17c-8a46-4674-966a-8dbdeba7763b"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""Walk"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""a0cf27dc-2c4d-4df4-91d8-7b56a3808918"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""Interact"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""Console"",
            ""id"": ""2d6d5855-a0e4-4fc4-ba66-ab9df4b0fa65"",
            ""actions"": [
                {
                    ""name"": ""ShowConsole"",
                    ""type"": ""Button"",
                    ""id"": ""667c014a-60ff-4d5c-a299-797fa4be75d4"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""CommandHistory"",
                    ""type"": ""Button"",
                    ""id"": ""1bf0e21f-ca97-45e3-be8d-1199e1566d07"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""0515aa79-74d1-4de4-9fe9-0eb4f5c53d99"",
                    ""path"": ""<Keyboard>/backquote"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""ShowConsole"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""49e40913-51ec-4c30-882b-36867f5ee602"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""PC"",
                    ""action"": ""CommandHistory"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""PC"",
            ""bindingGroup"": ""PC"",
            ""devices"": [
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // Default
        m_Default = asset.FindActionMap("Default", throwIfNotFound: true);
        m_Default_Mouse = m_Default.FindAction("Mouse", throwIfNotFound: true);
        m_Default_Run = m_Default.FindAction("Run", throwIfNotFound: true);
        m_Default_Walk = m_Default.FindAction("Walk", throwIfNotFound: true);
        m_Default_Interact = m_Default.FindAction("Interact", throwIfNotFound: true);
        // Console
        m_Console = asset.FindActionMap("Console", throwIfNotFound: true);
        m_Console_ShowConsole = m_Console.FindAction("ShowConsole", throwIfNotFound: true);
        m_Console_CommandHistory = m_Console.FindAction("CommandHistory", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }
    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Default
    private readonly InputActionMap m_Default;
    private IDefaultActions m_DefaultActionsCallbackInterface;
    private readonly InputAction m_Default_Mouse;
    private readonly InputAction m_Default_Run;
    private readonly InputAction m_Default_Walk;
    private readonly InputAction m_Default_Interact;
    public struct DefaultActions
    {
        private @GameControls m_Wrapper;
        public DefaultActions(@GameControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Mouse => m_Wrapper.m_Default_Mouse;
        public InputAction @Run => m_Wrapper.m_Default_Run;
        public InputAction @Walk => m_Wrapper.m_Default_Walk;
        public InputAction @Interact => m_Wrapper.m_Default_Interact;
        public InputActionMap Get() { return m_Wrapper.m_Default; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(DefaultActions set) { return set.Get(); }
        public void SetCallbacks(IDefaultActions instance)
        {
            if (m_Wrapper.m_DefaultActionsCallbackInterface != null)
            {
                @Mouse.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMouse;
                @Mouse.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMouse;
                @Mouse.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnMouse;
                @Run.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRun;
                @Run.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRun;
                @Run.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnRun;
                @Walk.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnWalk;
                @Walk.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnWalk;
                @Walk.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnWalk;
                @Interact.started -= m_Wrapper.m_DefaultActionsCallbackInterface.OnInteract;
                @Interact.performed -= m_Wrapper.m_DefaultActionsCallbackInterface.OnInteract;
                @Interact.canceled -= m_Wrapper.m_DefaultActionsCallbackInterface.OnInteract;
            }
            m_Wrapper.m_DefaultActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Mouse.started += instance.OnMouse;
                @Mouse.performed += instance.OnMouse;
                @Mouse.canceled += instance.OnMouse;
                @Run.started += instance.OnRun;
                @Run.performed += instance.OnRun;
                @Run.canceled += instance.OnRun;
                @Walk.started += instance.OnWalk;
                @Walk.performed += instance.OnWalk;
                @Walk.canceled += instance.OnWalk;
                @Interact.started += instance.OnInteract;
                @Interact.performed += instance.OnInteract;
                @Interact.canceled += instance.OnInteract;
            }
        }
    }
    public DefaultActions @Default => new DefaultActions(this);

    // Console
    private readonly InputActionMap m_Console;
    private IConsoleActions m_ConsoleActionsCallbackInterface;
    private readonly InputAction m_Console_ShowConsole;
    private readonly InputAction m_Console_CommandHistory;
    public struct ConsoleActions
    {
        private @GameControls m_Wrapper;
        public ConsoleActions(@GameControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @ShowConsole => m_Wrapper.m_Console_ShowConsole;
        public InputAction @CommandHistory => m_Wrapper.m_Console_CommandHistory;
        public InputActionMap Get() { return m_Wrapper.m_Console; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(ConsoleActions set) { return set.Get(); }
        public void SetCallbacks(IConsoleActions instance)
        {
            if (m_Wrapper.m_ConsoleActionsCallbackInterface != null)
            {
                @ShowConsole.started -= m_Wrapper.m_ConsoleActionsCallbackInterface.OnShowConsole;
                @ShowConsole.performed -= m_Wrapper.m_ConsoleActionsCallbackInterface.OnShowConsole;
                @ShowConsole.canceled -= m_Wrapper.m_ConsoleActionsCallbackInterface.OnShowConsole;
                @CommandHistory.started -= m_Wrapper.m_ConsoleActionsCallbackInterface.OnCommandHistory;
                @CommandHistory.performed -= m_Wrapper.m_ConsoleActionsCallbackInterface.OnCommandHistory;
                @CommandHistory.canceled -= m_Wrapper.m_ConsoleActionsCallbackInterface.OnCommandHistory;
            }
            m_Wrapper.m_ConsoleActionsCallbackInterface = instance;
            if (instance != null)
            {
                @ShowConsole.started += instance.OnShowConsole;
                @ShowConsole.performed += instance.OnShowConsole;
                @ShowConsole.canceled += instance.OnShowConsole;
                @CommandHistory.started += instance.OnCommandHistory;
                @CommandHistory.performed += instance.OnCommandHistory;
                @CommandHistory.canceled += instance.OnCommandHistory;
            }
        }
    }
    public ConsoleActions @Console => new ConsoleActions(this);
    private int m_PCSchemeIndex = -1;
    public InputControlScheme PCScheme
    {
        get
        {
            if (m_PCSchemeIndex == -1) m_PCSchemeIndex = asset.FindControlSchemeIndex("PC");
            return asset.controlSchemes[m_PCSchemeIndex];
        }
    }
    public interface IDefaultActions
    {
        void OnMouse(InputAction.CallbackContext context);
        void OnRun(InputAction.CallbackContext context);
        void OnWalk(InputAction.CallbackContext context);
        void OnInteract(InputAction.CallbackContext context);
    }
    public interface IConsoleActions
    {
        void OnShowConsole(InputAction.CallbackContext context);
        void OnCommandHistory(InputAction.CallbackContext context);
    }
}
