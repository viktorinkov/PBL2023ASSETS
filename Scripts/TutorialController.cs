using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialController : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        // if the player pref bool "Tutorial" doesn't exist, create it and set it to true. If it does exist and is true, go to scene CS_GameImageText.
        if (!PlayerPrefs.HasKey("Tutorial"))
        {
            PlayerPrefs.SetInt("Tutorial", 1);
            Debug.Log("Tutorial key created");
        }
        else if (PlayerPrefs.GetInt("Tutorial") == 1)
        {
            Debug.Log("Tutorial key exists and is true");
            UnityEngine.SceneManagement.SceneManager.LoadScene("CS_GameImageText");
        }

    }
}
