using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;
using System.Reflection;

public class Commandline : MonoBehaviour
{
    [SerializeField]
    private Text output;

    void Start()
    {
        
    }

    public void EnterCommand(InputField inputField)
    {
        string line = inputField.text;

        output.text += $"\n{inputField.text}";
        inputField.text = "";

        string[] args = line.Split(' ');
        string cmd = args[0];
        switch(cmd)
        {
            case "host":
                (new GameObject()).AddComponent<ServerBehaviour>();
                break;
            case "connect":
                (new GameObject()).AddComponent<ClientBehaviour>();
                break;
            default:
                output.text += $"\nUnrecognised command: {cmd}";
                break;
        }

        EventSystem.current.SetSelectedGameObject(inputField.gameObject, null);
        inputField.OnPointerClick(new PointerEventData(EventSystem.current));
    }

    void Update()
    {

    }
}
