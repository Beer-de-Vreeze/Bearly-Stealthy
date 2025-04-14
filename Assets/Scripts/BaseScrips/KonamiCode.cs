using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KonamiCode : MonoBehaviour
{
    KeyCode[] konamiCode =
    {
        KeyCode.UpArrow,
        KeyCode.UpArrow,
        KeyCode.DownArrow,
        KeyCode.DownArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.B,
        KeyCode.A
    };
    int currentInputIndex = 0;

    void Update()
    {
        // Check if any key is pressed
        if (Input.anyKeyDown)
        {
            // Check if the pressed key matches the expected key in the Konami code sequence
            if (Input.GetKeyDown(konamiCode[currentInputIndex]))
            {
                currentInputIndex++;

                // Check if the entire Konami code sequence has been entered
                if (currentInputIndex == konamiCode.Length)
                {
                    // Trigger your action here
                    SceneManager.LoadScene("KonamiMenu");
                    // Reset the sequence
                    currentInputIndex = 0;
                }
            }
            else
            {
                // Reset the sequence if a wrong key is pressed
                currentInputIndex = 0;
            }
        }
    }
}
