using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingAI : MonoBehaviour
{
    [Header("Manager Scripts")]
    public tileMapScript map;
    public gameManagerScript GMS;
    public battleManagerScript BMS;

    public GameObject selectedUnit;
    private GameObject playerTarget;

    private bool allowedToAttack = false;
    // int casas=0;

    private void Start()
    {
        //TMS = GetComponent<tileMapScript>();
        //GMS = GetComponent<gameManagerScript>();
    }

    private void Update()
    {
        if (!GMS.isPlayerTurn)
        {
            if (selectedUnit == null)
            {
                currentSelectedNPC();

            }

            else if (map.selectedUnit.GetComponent<UnitScript>().unitMoveState == map.selectedUnit.GetComponent<UnitScript>().getMovementStateEnum(1)
               && map.selectedUnit.GetComponent<UnitScript>().movementQueue.Count == 0)
            {

                if (FindTileToMove())
                {

                    map.unitSelectedPreviousX = map.selectedUnit.GetComponent<UnitScript>().x;
                    map.unitSelectedPreviousY = map.selectedUnit.GetComponent<UnitScript>().y;
                    map.previousOccupiedTile = map.selectedUnit.GetComponent<UnitScript>().tileBeingOccupied;
                    map.selectedUnit.GetComponent<UnitScript>().setWalkingAnimation();
                    map.moveUnit();//aqui ver o move lerp
                    StartCoroutine(moveUnitAndFinalize());
                }
            }
        }

        if ((map.selectedUnit != null) && (allowedToAttack == true))
        {
            attackOption();
        }
    }

    public void currentSelectedNPC()
    {
       // GameObject tempSelectedNPC = GMS.switchUnit();

        //selectedUnit = tempSelectedNPC;
       //// selectedUnit.GetComponent<UnitScript>().map = map;
      //  selectedUnit.GetComponent<UnitScript>().setMovementState(1);
      //  selectedUnit.GetComponent<UnitScript>().setSelectedAnimation();
        Debug.Log("Selecionado");
    }

    //Desc: moves the unit then finalizes the movement
    public IEnumerator moveUnitAndFinalize()
    {
        while (map.selectedUnit.GetComponent<UnitScript>().movementQueue.Count != 0)
        {
            yield return new WaitForEndOfFrame();
        }
        map.finalizeMovementPosition();

        map.selectedUnit.GetComponent<UnitScript>().setSelectedAnimation();
        allowedToAttack = true;

    }


    public bool FindTileToMove()
    {
        //Posição Tile do Jogador. Primeiro é necessário checar se o nó objetivo está vazio
        playerTarget = FindPlayer(GMS.teamPlayer);
        //moveRange é o tanto que a unidade pode se mover

        int possiblemoveX = playerTarget.GetComponent<UnitScript>().x; //posição do NPC + o tanto q ele pode mover
        int possiblemoveY = map.selectedUnit.GetComponent<UnitScript>().y - map.selectedUnit.GetComponent<UnitScript>().moveRange;
        //com esses dois valores ele tem que montar um vetor pra saber em que direção ele vai fazer esse movimento

        if (map.tilesOnMap[possiblemoveX, possiblemoveY].GetComponent<ClickableTileScript>().unitOnTile == null)
        {
            map.generatePathTo(possiblemoveX, possiblemoveY);

            return true;
        }
        else if (map.tilesOnMap[possiblemoveX, possiblemoveY].GetComponent<ClickableTileScript>().unitOnTile == map.selectedUnit)
        {
            Debug.Log("Vai mover pra q?");
        }
        return false;
    }

    public GameObject FindPlayer(GameObject playerTeam)
    {
        // temp = currentTeam.transform.GetChild(playerNum).gameObject;
        GameObject tempUnit = playerTeam.transform.GetChild(0).gameObject;

        for (int x = 0; x < playerTeam.transform.childCount; x++)
        {
            if (playerTeam.transform.GetChild(x).GetComponent<UnitScript>().currentHealthPoints < tempUnit.GetComponent<UnitScript>().currentHealthPoints)
            {
                tempUnit = playerTeam.transform.GetChild(x).gameObject;
            }

        }
        Debug.Log("Unidade a atacar: " + tempUnit.GetComponent<UnitScript>().unitName.ToString());

        return tempUnit;
    }

    private void attackOption()
    {

        //if(map.selectedUnit.GetComponent<UnitScript>().x == seekerTileX && map.selectedUnit.GetComponent<UnitScript>().y == seekerTileY){
        if (playerTarget.GetComponent<UnitScript>().currentHealthPoints > 0)
        {

           // map.selectedUnit.GetComponent<UnitScript>().setAttackAnimation();
            StartCoroutine(BMS.attack(map.selectedUnit, playerTarget));
            //map.selectedUnit.GetComponent<UnitScript>().wait();

            StartCoroutine(deselectAfterMovements(map.selectedUnit, playerTarget));
            GMS.endTurn();
            Debug.Log("Terminou!");
            allowedToAttack = false;
        }
        //}
        else
        {
            StartCoroutine(deselectAfterMovements(map.selectedUnit, playerTarget));
            GMS.endTurn();
            Debug.Log("Terminou sem atacar!");
            allowedToAttack = false;
        }
    }

    public void deselectUnit()
    {

        if (map.selectedUnit != null)
        {
            map.selectedUnit.GetComponent<UnitScript>().setMovementState(0);

            map.selectedUnit = null;
            playerTarget = null;
        }
    }

    public IEnumerator deselectAfterMovements(GameObject unit, GameObject enemy)
    {
        //selectedSound.Play();
        //If i dont have this wait for seconds the while loops get passed as the coroutine has not started from the other script
        //Adding a delay here to ensure that it all works smoothly. (probably not the best idea)
        yield return new WaitForSeconds(.25f);
        while (unit.GetComponent<UnitScript>().combatQueue.Count > 0)
        {
            yield return new WaitForEndOfFrame();
        }
        while (enemy.GetComponent<UnitScript>().combatQueue.Count > 0)
        {
            yield return new WaitForEndOfFrame();

        }
        //Debug.Log("All animations done playing");

        deselectUnit();

    }
}
