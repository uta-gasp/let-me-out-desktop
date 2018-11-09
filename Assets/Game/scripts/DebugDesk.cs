using System.Linq;
using UnityEngine;

public class DebugDesk : MonoBehaviour {

    public UnityEngine.UI.Text output;
    public bool show;

    private void Start()
    {
        if (output == null)
        {
            output = FindObjectsOfType<UnityEngine.UI.Text>().Single(text => text.tag == "debug");
        }
    }

    public void print(string aMsg, string aID = "")
    {
        Print("MONO", aID, aMsg);
    }

    public void print(string aMsg, bool aIsLocalPlayer, string aID = null)
    {
        Print(Network.isServer ? "SERVER" : ("CLIENT" + (aIsLocalPlayer ? " [LOCAL]" : " [REMOTE]")), aID, aMsg);
    }

    public void print(string aMsg, bool aIsServer, bool aIsLocalPlayer, string aID = null)
    {
        Print(aIsServer ? "SERVER" : ("CLIENT" + (aIsLocalPlayer ? " [LOCAL]" : " [REMOTE]")), aID, aMsg);
    }

    private void Print(string aPlayer, string aID, string aMsg)
    {
        if (!show || !enabled)
            return;

        string msg = $"{aPlayer} - {aID} - {aMsg}";

        Debug.Log(msg);

        if (output != null)
        {
            var lines = output.text.Split('\n').Where((line, i) => i < 30);
            output.text = msg + "\n" + string.Join("\n", lines.ToArray());
        }
    }
}
