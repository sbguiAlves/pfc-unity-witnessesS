using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UnitScript : MonoBehaviour
{
    public bool isPlayerTeam;
    public int x;
    public int y;

    //This is a low tier idea, don't use it 
    public bool coroutineRunning;

    //Meta defining play here
    public Queue<int> movementQueue;
    public Queue<int> combatQueue;
    //This global variable is used to increase the units movementSpeed when travelling on the board
    public float visualMovementSpeed = .15f;

    //Animator
    public Animator animator;

    public GameObject tileBeingOccupied;

    public GameObject damagedParticle;
    [Header("Unit Stats")]
    public string unitName;
    public int unitID;
    public int moveRange = 2;    
    public int attackRange = 1;
    public int maxDamage = 1; //fazer uma função com um random MAXmaxDamage. Ele pode dar 0 de dano pq ele errou o ataque
    public int maxHealthPoints = 5;
    public int currentHealthPoints;
    public Sprite unitSprite;

    private int damage;

    [Header("UI Elements")]
    //Unity UI References

    public Canvas damagePopupCanvas;
    public TMP_Text damagePopupText;
    public Image damageBackdrop;

    //This may change in the future if 2d sprites are used instead
    //public Material unitMaterial;
    //public Material unitWaitMaterial;

    public tileMapScript map;

    //Location for positional update
    public Transform startPoint;
    public Transform endPoint;
    public float moveSpeedTime = 1f;
    
    //2D model was used
    public GameObject holder2D;
    // Total distance between the markers.
    private float journeyLength;

    //Boolean to startTravelling
    public bool unitInMovement;

    //Enum for unit states
    public enum movementStates
    {
        Unselected,
        Selected,
        Moved,
        Wait
    }
    public movementStates unitMoveState;
   
    //Pathfinding A*
    public List<Node> path = null;

    //Path for moving unit's transform
    public List<Node> pathForMovement = null;
    public bool completedMovement = false;

    private void Awake()
    {
        animator = holder2D.GetComponent<Animator>();
        movementQueue = new Queue<int>();
        combatQueue = new Queue<int>();
        
        x = (int)transform.position.x;
        y = (int)transform.position.z;
        unitMoveState = movementStates.Unselected;
        currentHealthPoints = maxHealthPoints;
    }

    public void LateUpdate()
    {
        //damagePopupCanvas.transform.forward = Camera.main.transform.forward;
        holder2D.transform.forward = Camera.main.transform.forward;
    }

    public void MoveNextTile()
    {
        if (path.Count == 0)
        {
            return;
        }
        else
        {
            StartCoroutine(moveOverSeconds(transform.gameObject, path[path.Count - 1]));
        }
        
     }
   
    public void moveAgain()
    {   
        path = null;
        setMovementState(0);
        completedMovement = false;
        gameObject.GetComponentInChildren<SpriteRenderer>().color = Color.white;
        setIdleAnimation();
        //gameObject.GetComponentInChildren<Renderer>().material = unitMaterial;
    }

    public movementStates getMovementStateEnum(int i)
    {
        if (i == 0)
        {
            return movementStates.Unselected;
        }
        else if (i == 1)
        {
            return movementStates.Selected;
        }
        else if (i == 2)
        {
            return movementStates.Moved;
        }
        else if (i == 3)
        {
            return movementStates.Wait;
        }
        return movementStates.Unselected;
        
    }

    public void setMovementState(int i)
    {
        if (i == 0)
        {
            unitMoveState =  movementStates.Unselected;
        }
        else if (i == 1)
        {
            unitMoveState = movementStates.Selected;
        }
        else if (i == 2)
        {
            unitMoveState = movementStates.Moved;
        }
        else if (i == 3)
        {
            unitMoveState = movementStates.Wait;
        }
    
    }

    public void dealDamage(int x)
    {
        damage = x; //fazer o random dps, se der tempo
        currentHealthPoints = currentHealthPoints - damage;
    }

    public void wait()
    {

        gameObject.GetComponentInChildren<SpriteRenderer>().color = Color.gray;
        //gameObject.GetComponentInChildren<Renderer>().material = unitWaitMaterial;
    }

    public void unitDie()
    {
        if (holder2D.activeSelf)
        {
            StartCoroutine(fadeOut());
            StartCoroutine(checkIfRoutinesRunning());
           
        }    
    }

    public IEnumerator checkIfRoutinesRunning()
    {
        while (combatQueue.Count>0)
        {
          
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);

    }   

    public IEnumerator fadeOut()
    {

        combatQueue.Enqueue(1);
        //setDieAnimation();
        //yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        Renderer rend = GetComponentInChildren<SpriteRenderer>();
        
        for (float f = 1f; f >= .02; f -= 0.01f)
        {
            Color c = rend.material.color;
            c.a = f;
            rend.material.color = c;
            yield return new WaitForEndOfFrame();
        }
        combatQueue.Dequeue();//tira o inimigo da fila de ação
       

    }
    
    public IEnumerator moveOverSeconds(GameObject objectToMove,Node endNode)
    {
        movementQueue.Enqueue(1); //adiciona quem vai fazer o movimento e dps tira o cara dessa fila

        //remove first thing on path because, its the tile we are standing on
        path.RemoveAt(0);
        while (path.Count != 0)
        {
            
            Vector3 endPos = map.tileCoordToWorldCoord(path[0].x, path[0].y); //posição destino
            objectToMove.transform.position = Vector3.Lerp(transform.position, endPos, visualMovementSpeed); //movimento da origem até destino
            if ((transform.position - endPos).sqrMagnitude < 0.001) {

                path.RemoveAt(0); //remove tile da fila, movimento por segundo
              
            }
            yield return new WaitForEndOfFrame();
        }
        visualMovementSpeed = 0.15f; //reseta os valores
        transform.position = map.tileCoordToWorldCoord(endNode.x, endNode.y);

        x = endNode.x;
        y = endNode.y;
        tileBeingOccupied.GetComponent<ClickableTileScript>().unitOnTile = null;
        tileBeingOccupied = map.tilesOnMap[x, y];
        movementQueue.Dequeue();

    }

    public IEnumerator displayDamageEnum(int damageTaken)
    {

        combatQueue.Enqueue(1);
       
        damagePopupText.SetText(damageTaken.ToString());
        damagePopupCanvas.enabled = true;
        for (float f = 1f; f >=-0.01f; f -= 0.01f)
        {
            
            Color backDrop = damageBackdrop.GetComponent<Image>().color;
            Color damageValue = damagePopupText.color;

            backDrop.a = f;
            damageValue.a = f;
            damageBackdrop.GetComponent<Image>().color = backDrop;
            damagePopupText.color = damageValue;
           yield return new WaitForEndOfFrame();
        }

        //damagePopup.enabled = false;
        combatQueue.Dequeue();
       
    }

    public void resetPath()
    {
        path = null;
        completedMovement = false;
    }
    
    public void displayDamage(int damageTaken)
    {
        damagePopupCanvas.enabled = true;
        damagePopupText.SetText(damageTaken.ToString());
    }

    public void disableDisplayDamage()
    {
        damagePopupCanvas.enabled = false;
    }

    public void setSelectedAnimation()
    {       
        animator.SetTrigger("toSelected");
    }

    public void setIdleAnimation()
    {        
        animator.SetTrigger("toIdle");
    }

    public void setWalkingAnimation()
    {
        animator.SetTrigger("toWalking");
    }

    public void setAttackAnimation()
    {
       animator.SetTrigger("toAttacking");
    }

    public void setWaitIdleAnimation()
    {
        animator.SetTrigger("toIdleWait");
    }
       
    public void setDieAnimation()
    {
        animator.SetTrigger("dieTrigger");
    }
}
