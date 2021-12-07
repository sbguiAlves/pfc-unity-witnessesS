using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class gameSceneManager : MonoBehaviour
{
    public Canvas mainMenu;
    public Canvas aboutMenu;

    public Canvas tutorialCanvas;   
    public Canvas pageOneBookCanvas;
    public Canvas pageTwoBookCanvas;
    public Canvas pageThreeBookCanvas;

    void Start()
    {
        mainMenu.enabled = true;
        aboutMenu.enabled = false;
    }


    public void BeginPlay()
    {
        mainMenu.enabled = false;
        aboutMenu.enabled = false;
        tutorialCanvas.enabled = true;
        PageOneBook();
    }

    public void PageOneBook()
    {
        pageOneBookCanvas.enabled = true;
        pageTwoBookCanvas.enabled = false;
        pageThreeBookCanvas.enabled = false;
    }

    public void PageTwoBook()
    {
        pageOneBookCanvas.enabled = false;
        pageTwoBookCanvas.enabled = true;
        pageThreeBookCanvas.enabled = false;
    }

    public void PageThreeBook()
    {
        pageOneBookCanvas.enabled = false;
        pageTwoBookCanvas.enabled = false;
        pageThreeBookCanvas.enabled = true;
    }        

    public void closeHelpCanvas()
    {
        tutorialCanvas.enabled = false;
        
        pageOneBookCanvas.enabled = false;
        pageTwoBookCanvas.enabled = false;
        pageThreeBookCanvas.enabled = false;

        SceneManager.LoadScene(1);
    }


    public void Leave()
    {
        Application.Quit();
    }

    public void loadMainMenu()
    {
        SceneManager.LoadScene(0);
    }


    public void loadHelpCanvas()
    {
        mainMenu.enabled = false;
        aboutMenu.enabled = true;
    }

    public void loadMainCanvas()
    {
        mainMenu.enabled = true;
        aboutMenu.enabled = false;
    }
}
