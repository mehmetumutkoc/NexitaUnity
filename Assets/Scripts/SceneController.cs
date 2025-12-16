using UnityEngine;
using UnityEngine.InputSystem;

public class SceneController : MonoBehaviour
{
    

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AttractionSequence attractionSequence = FindObjectOfType<AttractionSequence>();
            if (attractionSequence != null)
            {
                attractionSequence.ToggleFocus();
            }
        }
    }
}
