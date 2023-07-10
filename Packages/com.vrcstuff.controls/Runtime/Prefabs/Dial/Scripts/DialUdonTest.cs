
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DialUdonTest : UdonSharpBehaviour
{
    public int currentPosition = 0;

    void Start()
    {
        
    }

    public void PositionMoved()
    {
        Debug.Log($"{gameObject.name} - Dial moved to position {currentPosition}");
    }

    public void Selected()
    {
        Debug.Log($"{gameObject.name} - I've been selected! Pos is now {currentPosition}");
    }
    public void Deselected()
    {
        Debug.Log($"{gameObject.name} - I've been de-selected! Pos is now {currentPosition}");
    }
}
