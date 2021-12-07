using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMainManager : MonoBehaviour
{ 
    public void loadMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void loadLevelOne()
    {
        SceneManager.LoadScene(1);
    }
}
