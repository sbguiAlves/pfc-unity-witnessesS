using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tileMapScript : MonoBehaviour
{
    //Reference holders for the other two scripts that are currently running
    //alongside this script
    [Header("Manager Scripts")]
    public battleManagerScript BMS;
    public gameManagerScript GMS;

    //public GameObject unitTarget;

    //List of tiles that are used to generate the map
    //Try chaging tilesTypes to enum later   
    [Header("Tiles")]
    public Tile[] tileTypes;
    public int[,] tiles;
    public ArrayLayout arrayData;

    //This is used when the game starts and there are pre-existing units
    //It uses this variable to check if there are any units and then maps them to the proper tiles
    [Header("Units on the board")]
    public GameObject unitsOnBoard;

    //This 2d array is the list of tile gameObjects on the board
    public GameObject[,] tilesOnMap;

    //This 2d array is the list of quadUI gameObjects on the board
    public GameObject[,] quadOnMap;
    public GameObject[,] quadOnMapForUnitMovementDisplay;
    public GameObject[,] quadOnMapCursor;

    //public is only to set them in the inspector, if you change these to private then you will
    //need to re-enable them in the inspector
    //Game object that is used to overlay onto the tiles to show possible movement
    public GameObject mapUI;
    //Game object that is used to highlight the mouse location
    public GameObject mapCursorUI;
    //Game object that is used to highlight the path the unit is taking
    public GameObject mapUnitMovementUI;

    //Nodes along the path of shortest path from the pathfinding
    public List<Node> currentPath = null;
    public List<Node> tempPath = null;

    //Node graph for pathfinding purposes
    public Node[,] graph;

    //containers (parent gameObjects) for the UI tiles
    [Header("Containers")]
    public GameObject tileContainer;
    public GameObject UIQuadPotentialMovesContainer;
    public GameObject UIQuadCursorContainer;
    public GameObject UIUnitMovementPathContainer;

    //Set in the inspector, might change this otherwise.
    //This is the map size (please put positive numbers it probably wont work well with negative numbers)
    [Header("Board Size")]
    public int mapSizeX;
    public int mapSizeY;

    //In the update() function mouse down raycast sets this unit
    [Header("Selected Unit Info")]
    public GameObject selectedUnit;
    //These two are set in the highlightUnitRange() function
    //They are used for other things as well, mainly to check for movement, or finalize function
    public HashSet<Node> selectedUnitTotalRange;
    public HashSet<Node> selectedUnitMoveRange;

    public bool isSelected = false;
    public bool toMove = false;

    public int unitSelectedPreviousX;
    public int unitSelectedPreviousY;

    public GameObject previousOccupiedTile;

    //public AudioSource selectedSound;
    //public AudioSource unselectedSound;
    //public area to set the material for the quad material for UI purposes
    [Header("Materials")]
    public Material colorPreviousAttack;
    public Material redUIMat;
    public Material blueUIMat;

    private void Start()
    {
        //selectedUnit = new GameObject();
        //Get the battlemanager running
        //BMS = GetComponent<battleManagerScript>();
        //GMS = GetComponent<gameManagerScript>();
        //Generate the map info that will be used
        generateMapInfo();
        //Generate pathfinding graph
        generatePathFindingGraph();
        //With the generated info this function will read the info and produce the map
        generateMapVisuals();
        //Check if there are any pre-existing units on the board
        setIfTileIsOccupied();
    }

    private void Update()
    {
        if (selectedUnit == null)
        {
            if (GMS.isPlayerTurn)
                currentPlayerUnit(); //seleciona automaticamente uma unidade do time
            if (!GMS.isPlayerTurn)
                currentEnemyUnit();
        }
        if (selectedUnit != null)
        {
            //After a unit has been selected then, we need to check if the unit has entered the selection state 'Selected' (1)
            //Move unit
            if (selectedUnit.GetComponent<UnitScript>().unitMoveState == selectedUnit.GetComponent<UnitScript>().getMovementStateEnum(1)
                    && selectedUnit.GetComponent<UnitScript>().movementQueue.Count == 0)
            {
                if (GMS.isPlayerTurn)
                {
                    if (selectTileToMoveTo())
                    {
                        //selectedSound.Play();
                        selectedUnit.GetComponent<UnitScript>().setWalkingAnimation();
                        unitSelectedPreviousX = selectedUnit.GetComponent<UnitScript>().x;
                        unitSelectedPreviousY = selectedUnit.GetComponent<UnitScript>().y;
                        previousOccupiedTile = selectedUnit.GetComponent<UnitScript>().tileBeingOccupied;
                        moveUnit();

                        StartCoroutine(moveUnitAndFinalize());
                        toMove = true;                     
                    }
                }
                if (!GMS.isPlayerTurn) //Enemy Turn
                {
                    if (generatePath())
                    {
                        selectedUnit.GetComponent<UnitScript>().setWalkingAnimation();
                        moveUnit();
                        StartCoroutine(moveUnitAndFinalize());
                    }
                    else
                    {
                        selectedUnit.GetComponent<UnitScript>().setMovementState(2);
                    }
                }

            }

            //After finalizes the movement, then is time to attack ou skip turn
            if (selectedUnit.GetComponent<UnitScript>().unitMoveState == selectedUnit.GetComponent<UnitScript>().getMovementStateEnum(2))
            {
                if (GMS.isPlayerTurn)
                {
                    highlightUnitAttackOptionsFromPosition();
                    highlightTileUnitIsOccupying();
                    finalizeOption();
                }
                if (!GMS.isPlayerTurn)               
                    AIOption();

            }
        }
    }

    //The map layouts a bit different
    //all this does is set the tiles[x,y] to the corresponding tile
    public void generateMapInfo()
    {
        tiles = new int[mapSizeX, mapSizeY];
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                tiles[x, y] = arrayData.rows[x].row[y];
            }
        }
    }

    //Creates the graph for the pathfinding, it sets up the neighbours
    public void generatePathFindingGraph()
    {
        graph = new Node[mapSizeX, mapSizeY];

        //initialize graph 
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                graph[x, y] = new Node();
                graph[x, y].x = x;
                graph[x, y].y = y;
            }
        }
        //calculate neighbours
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                //X is not 0, then we can add left (x - 1)
                if (x > 0)
                {
                    graph[x, y].neighbours.Add(graph[x - 1, y]);
                }
                //X is not mapSizeX - 1, then we can add right (x + 1)
                if (x < mapSizeX - 1)
                {
                    graph[x, y].neighbours.Add(graph[x + 1, y]);
                }
                //Y is not 0, then we can add downwards (y - 1 ) 
                if (y > 0)
                {
                    graph[x, y].neighbours.Add(graph[x, y - 1]);
                }
                //Y is not mapSizeY -1, then we can add upwards (y + 1)
                if (y < mapSizeY - 1)
                {
                    graph[x, y].neighbours.Add(graph[x, y + 1]);
                }
            }
        }
    }

    //In: 
    //Out: void
    //Desc: This instantiates all the information for the map, the UI Quads and the map tiles
    public void generateMapVisuals()
    {
        //generate list of actual tileGameObjects
        tilesOnMap = new GameObject[mapSizeX, mapSizeY];
        quadOnMap = new GameObject[mapSizeX, mapSizeY];
        quadOnMapForUnitMovementDisplay = new GameObject[mapSizeX, mapSizeY];
        quadOnMapCursor = new GameObject[mapSizeX, mapSizeY];
        int index;
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                index = tiles[x, y];
                GameObject newTile = Instantiate(tileTypes[index].tileVisualPrefab, new Vector3(x, 0, y), Quaternion.identity);
                newTile.GetComponent<ClickableTileScript>().tileX = x;
                newTile.GetComponent<ClickableTileScript>().tileY = y;
                newTile.GetComponent<ClickableTileScript>().map = this;
                newTile.transform.SetParent(tileContainer.transform);
                tilesOnMap[x, y] = newTile;


                GameObject gridUI = Instantiate(mapUI, new Vector3(x, 0.501f, y), Quaternion.Euler(90f, 0, 0));
                gridUI.transform.SetParent(UIQuadPotentialMovesContainer.transform);
                quadOnMap[x, y] = gridUI;

                GameObject gridUIForPathfindingDisplay = Instantiate(mapUnitMovementUI, new Vector3(x, 0.502f, y), Quaternion.Euler(90f, 0, 0));
                gridUIForPathfindingDisplay.transform.SetParent(UIUnitMovementPathContainer.transform);
                quadOnMapForUnitMovementDisplay[x, y] = gridUIForPathfindingDisplay;

                GameObject gridUICursor = Instantiate(mapCursorUI, new Vector3(x, 0.503f, y), Quaternion.Euler(90f, 0, 0));
                gridUICursor.transform.SetParent(UIQuadCursorContainer.transform);
                quadOnMapCursor[x, y] = gridUICursor;

            }
        }
    }

    //Moves the unit
    public void moveUnit()
    {
        if (selectedUnit != null)
        {
            selectedUnit.GetComponent<UnitScript>().MoveNextTile();
        }
    }

    //In: the x and y of a tile
    //Out: vector 3 of the tile in world space, theyre .75f off of zero
    //Desc: returns a vector 3 of the tile in world space, theyre .75f off of zero
    public Vector3 tileCoordToWorldCoord(int x, int y)
    {
        return new Vector3(x, 0.75f, y);
    }

    //In: 
    //Out: void
    //Desc: sets the tile as occupied, if a unit is on the tile
    public void setIfTileIsOccupied()
    {
        foreach (Transform team in unitsOnBoard.transform)
        {
            //Debug.Log("Set if Tile is Occupied is Called");
            foreach (Transform unitOnTeam in team)
            {
                int unitX = unitOnTeam.GetComponent<UnitScript>().x;
                int unitY = unitOnTeam.GetComponent<UnitScript>().y;
                unitOnTeam.GetComponent<UnitScript>().tileBeingOccupied = tilesOnMap[unitX, unitY];
                tilesOnMap[unitX, unitY].GetComponent<ClickableTileScript>().unitOnTile = unitOnTeam.gameObject;
            }

        }
    }

    public bool generatePath()
    {
        GameObject unitTarget = null;

        if (selectedUnit.GetComponent<UnitScript>().unitID == 0)
            unitTarget = FindByLife(GMS.teamPlayer);

        if (selectedUnit.GetComponent<UnitScript>().unitID == 1)
            unitTarget = FindByDamage(GMS.teamPlayer);

        int targetX = unitTarget.GetComponent<UnitScript>().x;
        int targetY = unitTarget.GetComponent<UnitScript>().y + selectedUnit.GetComponent<UnitScript>().attackRange;

        if (unitCanEnterTile(targetX, targetY) == false)
        {
            //Debug.Log("Não pode mover. Coord:" + targetX + ", " + targetY);
            //cant move into something so we can probably just return
            //cant set this endpoint as our goal
            return false;
        }

        selectedUnit.GetComponent<UnitScript>().path = null;
        currentPath = null;

        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();

        Node source = graph[selectedUnit.GetComponent<UnitScript>().x, selectedUnit.GetComponent<UnitScript>().y];
        Node target = graph[targetX, targetY];

        dist[source] = 0;
        prev[source] = null;

        List<Node> unvisited = new List<Node>();

        foreach (Node n in graph)
        {
            if (n != source)
            {
                dist[n] = Mathf.Infinity;
                prev[n] = null;
            }
            unvisited.Add(n);
        }
        while (unvisited.Count > 0)
        {
            Node u = null;
            foreach (Node possibleU in unvisited)
            {
                if (u == null || dist[possibleU] < dist[u])
                {
                    u = possibleU;
                }
            }
            if (u == target) //se o nó que ele está agr for igual ao destino, sai do loop
            {
                break;
            }
            unvisited.Remove(u);

            foreach (Node n in u.neighbours)
            {
                float alt = dist[u] + costToEnterTile(n.x, n.y);
                if (alt < dist[n])
                {
                    dist[n] = alt;
                    prev[n] = u;
                }
            }
        }
        if (prev[target] == null)
        {
            //No route;
            return false;
        }
        tempPath = new List<Node>();
        currentPath = new List<Node>();
        Node curr = target;
        int index = 0;

        while (curr != null)//dependendo então do quanto ele pode se mover (moveRange), ele subtrai dos nós possíveis
        {
            tempPath.Add(curr);
            curr = prev[curr];
        }
        //Now currPath is from target to our source, we need to reverse it from source to target.
        tempPath.Reverse();
        //Debug.Log("Casas andadas: " + currentPath.Count);

        foreach (var elem in tempPath)
        {
            Debug.Log("Lista:" + elem.x + "," + elem.y);
        }

        if (tempPath.Count == 2)
        {
            Debug.Log("Anda só um");
            selectedUnit.GetComponent<UnitScript>().path = tempPath;
            return true;
        }
        else
        {
            while (currentPath.Count < selectedUnit.GetComponent<UnitScript>().moveRange)
            {
                currentPath.Add(tempPath[index]);
                index++;
            }
            selectedUnit.GetComponent<UnitScript>().path = currentPath;
        }

        return true;
    }

    //In: x and y position of the tile to move to
    //Out: void
    //Desc: generates the path for the selected unit ------------------ IMPORTANTE ESSE DAQUI
    public void generatePathTo(int x, int y)
    {
        //Clicked the same tile that the unit is standing on
        if (selectedUnit.GetComponent<UnitScript>().x == x && selectedUnit.GetComponent<UnitScript>().y == y)
        {
            currentPath = new List<Node>();
            selectedUnit.GetComponent<UnitScript>().path = currentPath;

            return;
        }

        if (unitCanEnterTile(x, y) == false)
        {
            //cant move into something so we can probably just return
            //cant set this endpoint as our goal

            return;
        }

        selectedUnit.GetComponent<UnitScript>().path = null;
        currentPath = null;
        //from wiki dijkstra's
        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();
        Node source = graph[selectedUnit.GetComponent<UnitScript>().x, selectedUnit.GetComponent<UnitScript>().y];
        Node target = graph[x, y]; //aqui vai ter uma lista de opções para os movimentos ---------- IMPORTANTÍSSIMO PRA IA DO NPC
        dist[source] = 0;
        prev[source] = null;
        //Unchecked nodes
        List<Node> unvisited = new List<Node>();

        //Initialize
        foreach (Node n in graph)
        {

            //Initialize to infite distance as we don't know the answer
            //Also some places are infinity
            if (n != source)
            {
                dist[n] = Mathf.Infinity;
                prev[n] = null;
            }
            unvisited.Add(n);
        }
        //if there is a node in the unvisited list lets check it
        while (unvisited.Count > 0)
        {
            //u will be the unvisited node with the shortest distance
            Node u = null;
            foreach (Node possibleU in unvisited)
            {
                if (u == null || dist[possibleU] < dist[u])
                {
                    u = possibleU;
                }
            }


            if (u == target) //se o nó que ele está agr for igual ao destino, sai do loop
            {
                break;
            }

            unvisited.Remove(u);

            foreach (Node n in u.neighbours)
            {

                //float alt = dist[u] + u.DistanceTo(n);
                float alt = dist[u] + costToEnterTile(n.x, n.y);
                if (alt < dist[n])
                {
                    dist[n] = alt;
                    prev[n] = u;
                }
            }
        }
        //if were here we found shortest path, or no path exists
        if (prev[target] == null)
        {
            //No route;
            return;
        }
        currentPath = new List<Node>();
        Node curr = target;
        //Step through the current path and add it to the chain
        while (curr != null)
        {
            currentPath.Add(curr);
            curr = prev[curr];
        }
        //Now currPath is from target to our source, we need to reverse it from source to target.
        currentPath.Reverse();

        selectedUnit.GetComponent<UnitScript>().path = currentPath;
    }

    //In: tile's x and y position
    //Out: cost that is requiredd to enter the tile
    //Desc: checks the cost of the tile for a unit to enter
    public float costToEnterTile(int x, int y)
    {

        if (unitCanEnterTile(x, y) == false)
        {
            return Mathf.Infinity;
        }

        //Gotta do the math here
        Tile t = tileTypes[tiles[x, y]];
        float dist = t.movementCost;

        return dist;
    }

    //change this when we add movement types
    //In:  tile's x and y position
    //Out: true or false if the unit can enter the tile that was entered
    //Desc: if the tile is not occupied by another team's unit, then you can walk through and if the tile is walkable 
    public bool unitCanEnterTile(int x, int y)
    {
        if (tilesOnMap[x, y].GetComponent<ClickableTileScript>().unitOnTile != null)
        {
            return false;
        }
        return tileTypes[tiles[x, y]].isWalkable;
    }

    //Finalizes the movement, sets the tile the unit moved to as occupied, etc
    public void finalizeMovementPosition()
    {
        tilesOnMap[selectedUnit.GetComponent<UnitScript>().x, selectedUnit.GetComponent<UnitScript>().y].GetComponent<ClickableTileScript>().unitOnTile = selectedUnit;
        //After a unit has been moved we will set the unitMoveState to (2) the 'Moved' state
        selectedUnit.GetComponent<UnitScript>().setMovementState(2);

        // highlightUnitAttackOptionsFromPosition();
        //highlightTileUnitIsOccupying();
    }

    public void currentPlayerUnit()
    {
        if (isSelected == false && GMS.isPlayerTurn)
        {
            GameObject tempSelectedUnit = GMS.returnUnit();

            if (tempSelectedUnit == null)
            {
                GMS.checkIfUnitsRemain();
            }
            else
            {
                //Debug.Log("Unidade Amiga: " + tempSelectedUnit.GetComponent<UnitScript>().unitName.ToString());
                //Debug.Log("Status Amigo: " + tempSelectedUnit.GetComponent<UnitScript>().unitMoveState.ToString());

                if (tempSelectedUnit.GetComponent<UnitScript>().unitMoveState == tempSelectedUnit.GetComponent<UnitScript>().getMovementStateEnum(0))
                {
                    // disableHighlightUnitRange();
                    //selectedSound.Play();

                    selectedUnit = tempSelectedUnit;
                    selectedUnit.GetComponent<UnitScript>().map = this;
                    selectedUnit.GetComponent<UnitScript>().setMovementState(1);
                    selectedUnit.GetComponent<UnitScript>().setSelectedAnimation();
                    isSelected = true;
                    highlightUnitRange();
                }
            }


        }
    }

    public void currentEnemyUnit()
    {
        if (isSelected == false && !GMS.isPlayerTurn)
        {
            GameObject tempSelectedUnit = GMS.returnUnit();
            // Debug.Log("Unidade Inimiga: " + tempSelectedUnit.GetComponent<UnitScript>().unitName.ToString());
            // Debug.Log("Status Inimigo: " + tempSelectedUnit.GetComponent<UnitScript>().unitMoveState.ToString());

            if (tempSelectedUnit == null)
            {
                // Debug.Log("Para NPC");
                GMS.checkIfUnitsRemain();
            }
            else
            {
                if (tempSelectedUnit.GetComponent<UnitScript>().unitMoveState == tempSelectedUnit.GetComponent<UnitScript>().getMovementStateEnum(0))
                {
                    //selectedSound.Play();

                    selectedUnit = tempSelectedUnit;
                    selectedUnit.GetComponent<UnitScript>().map = this;
                    selectedUnit.GetComponent<UnitScript>().setMovementState(1);
                    selectedUnit.GetComponent<UnitScript>().setSelectedAnimation();
                    isSelected = true;
                }
                //Debug.Log("Status Inimigo: " + tempSelectedUnit.GetComponent<UnitScript>().unitMoveState.ToString());
            }


        }
    }

    public GameObject FindByLife(GameObject playerTeam)
    {
        GameObject tempUnit = playerTeam.transform.GetChild(0).gameObject;
        if (playerTeam.transform.childCount > 1)
        {
            for (int x = 0; x < playerTeam.transform.childCount; x++)
            {
                if (playerTeam.transform.GetChild(x).GetComponent<UnitScript>().currentHealthPoints < tempUnit.GetComponent<UnitScript>().currentHealthPoints)
                    tempUnit = playerTeam.transform.GetChild(x).gameObject;
            }

        }

        return tempUnit;
    }

    public GameObject FindByDamage(GameObject playerTeam)
    {
        GameObject tempUnit = playerTeam.transform.GetChild(0).gameObject;
        if (playerTeam.transform.childCount > 1)
        {
            for (int x = 0; x < playerTeam.transform.childCount; x++)
            {
                if (playerTeam.transform.GetChild(x).GetComponent<UnitScript>().maxDamage >= tempUnit.GetComponent<UnitScript>().maxDamage)
                    tempUnit = playerTeam.transform.GetChild(x).gameObject;
            }

        }

        return tempUnit;
    }

    //Finalizes the AI's option, wait or attack
    public void AIOption()
    {
        // Debug.Log("Ação Inimiga");
        HashSet<Node> attackableTiles = getUnitAttackOptionsFromPosition();
        GameObject unitTarget = null;

        if (selectedUnit.GetComponent<UnitScript>().unitID == 0)
            unitTarget = FindByLife(GMS.teamPlayer);

        if (selectedUnit.GetComponent<UnitScript>().unitID == 1)
            unitTarget = FindByDamage(GMS.teamPlayer);

        int unitX = unitTarget.GetComponent<UnitScript>().x;
        int unitY = unitTarget.GetComponent<UnitScript>().y;


            if (selectedUnit.GetComponent<UnitScript>().x == unitX && (selectedUnit.GetComponent<UnitScript>().y == unitY + selectedUnit.GetComponent<UnitScript>().attackRange))
            {
                if (unitTarget.GetComponent<UnitScript>().currentHealthPoints > 0)
                {

                    //We clicked an enemy that should be attacked
                    StartCoroutine(BMS.attack(selectedUnit, unitTarget));
                    //selectedUnit.GetComponent<UnitScript>().wait();

                    //Check if soemone has won
                    //GMS.checkIfUnitsRemain();
                    StartCoroutine(deselectAfterMovements(selectedUnit, unitTarget));
                }
            }
            else
            {
                StartCoroutine(deselectAfterMovements(selectedUnit, unitTarget));
            }

        unitTarget = null;
    }

    //Finalizes the player's option, wait or attack
    public void finalizeOption()
    {
        //Debug.Log("Ação Player");
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        HashSet<Node> attackableTiles = getUnitAttackOptionsFromPosition();

        if (Physics.Raycast(ray, out hit))
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (hit.transform.gameObject.CompareTag("Tile"))
                {
                    //Exists a unit here
                    if (hit.transform.GetComponent<ClickableTileScript>().unitOnTile != null)
                    {

                        GameObject unitOnTile = hit.transform.GetComponent<ClickableTileScript>().unitOnTile;
                        int unitX = unitOnTile.GetComponent<UnitScript>().x;
                        int unitY = unitOnTile.GetComponent<UnitScript>().y;

                        if (unitOnTile.GetComponent<UnitScript>().isPlayerTeam != selectedUnit.GetComponent<UnitScript>().isPlayerTeam && attackableTiles.Contains(graph[unitX, unitY]))
                        {
                            if (unitOnTile.GetComponent<UnitScript>().currentHealthPoints > 0)
                            {
                                //We clicked an enemy that should be attacked

                                StartCoroutine(BMS.attack(selectedUnit, unitOnTile));
                                //selectedUnit.GetComponent<UnitScript>().wait();

                                //Check if soemone has won
                                //GMS.checkIfUnitsRemain();
                                StartCoroutine(deselectAfterMovements(selectedUnit, unitOnTile));
                                toMove = false;
                            }
                        }
                    }
                }
            }
        }
    }

    public void passTurn()
    {
        selectedUnit.GetComponent<UnitScript>().setIdleAnimation();
        deselectUnit();
    }

    //Highlights the units range options - O NPC não faz o highlight disso
    public void highlightUnitRange()
    {
        HashSet<Node> finalMovementHighlight = new HashSet<Node>();
        HashSet<Node> totalAttackableTiles = new HashSet<Node>();
        HashSet<Node> finalEnemyUnitsInMovementRange = new HashSet<Node>();

        int attRange = selectedUnit.GetComponent<UnitScript>().attackRange;
        int moveRange = selectedUnit.GetComponent<UnitScript>().moveRange;

        Node unitInitialNode = graph[selectedUnit.GetComponent<UnitScript>().x, selectedUnit.GetComponent<UnitScript>().y];
        finalMovementHighlight = getUnitMovementOptions();
        totalAttackableTiles = getUnitTotalAttackableTiles(finalMovementHighlight, attRange, unitInitialNode);

        foreach (Node n in totalAttackableTiles)
        {

            if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
            {
                GameObject unitOnCurrentlySelectedTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                if (unitOnCurrentlySelectedTile.GetComponent<UnitScript>().isPlayerTeam != selectedUnit.GetComponent<UnitScript>().isPlayerTeam)
                {
                    finalEnemyUnitsInMovementRange.Add(n);
                }
            }
        }


        highlightEnemiesInRange(totalAttackableTiles);
        //highlightEnemiesInRange(finalEnemyUnitsInMovementRange);
        highlightMovementRange(finalMovementHighlight);
        //Debug.Log(finalMovementHighlight.Count);
        selectedUnitMoveRange = finalMovementHighlight;

        //This final bit sets the selected Units tiles, which can be accessible in other functions
        //Probably bad practice, but I'll need to get things to work for now (2019-09-30)
        selectedUnitTotalRange = getUnitTotalRange(finalMovementHighlight, totalAttackableTiles);
        //Debug.Log(unionTiles.Count);

        //This will for each loop will highlight the movement range of the units
    }


    //Desc: disables the quads that are being used to highlight position
    public void disableUnitUIRoute()
    {
        foreach (GameObject quad in quadOnMapForUnitMovementDisplay)
        {
            if (quad.GetComponent<Renderer>().enabled == true)
            {

                quad.GetComponent<Renderer>().enabled = false;
            }
        }
    }

    //In:  
    //Out: HashSet<Node> of the tiles that can be reached by unit
    //Desc: returns the hashSet of nodes that the unit can reach from its position
    public HashSet<Node> getUnitMovementOptions() // Os tiles que são alcançaveis pela unidade, aqui entra as opções da IA ---- IMPORTANTE
    {
        float[,] cost = new float[mapSizeX, mapSizeY];
        HashSet<Node> UIHighlight = new HashSet<Node>();
        HashSet<Node> tempUIHighlight = new HashSet<Node>();
        HashSet<Node> finalMovementHighlight = new HashSet<Node>();
        int moveRange = selectedUnit.GetComponent<UnitScript>().moveRange; //IMPORTANTE ESSA PARTE AQUI
        Node unitInitialNode = graph[selectedUnit.GetComponent<UnitScript>().x, selectedUnit.GetComponent<UnitScript>().y]; //POSIÇÃO INICIAL DO NPC

        ///Set-up the initial costs for the neighbouring nodes
        finalMovementHighlight.Add(unitInitialNode);
        foreach (Node n in unitInitialNode.neighbours)
        {
            cost[n.x, n.y] = costToEnterTile(n.x, n.y);
            // Debug.Log(cost[n.x, n.y]);
            if (moveRange - cost[n.x, n.y] >= 0)
            {
                UIHighlight.Add(n);
            }
        }

        finalMovementHighlight.UnionWith(UIHighlight);

        while (UIHighlight.Count != 0)
        {
            foreach (Node n in UIHighlight)
            {
                foreach (Node neighbour in n.neighbours)
                {
                    if (!finalMovementHighlight.Contains(neighbour))
                    {
                        cost[neighbour.x, neighbour.y] = costToEnterTile(neighbour.x, neighbour.y) + cost[n.x, n.y];
                        //Debug.Log(cost[neighbour.x, neighbour.y]);
                        if (moveRange - cost[neighbour.x, neighbour.y] >= 0)
                        {
                            //Debug.Log(cost[neighbour.x, neighbour.y]);
                            tempUIHighlight.Add(neighbour);
                        }
                    }
                }

            }

            UIHighlight = tempUIHighlight;
            finalMovementHighlight.UnionWith(UIHighlight);
            tempUIHighlight = new HashSet<Node>();

        }
        //Debug.Log("The total amount of movable spaces for this unit is: " + finalMovementHighlight.Count);
        return finalMovementHighlight;
    }

    //In:  finalMovement highlight and totalAttackabletiles
    //Out: a hashSet of nodes that are the combination of the two inputs
    //Desc: returns the unioned hashSet
    public HashSet<Node> getUnitTotalRange(HashSet<Node> finalMovementHighlight, HashSet<Node> totalAttackableTiles)
    {
        HashSet<Node> unionTiles = new HashSet<Node>();
        unionTiles.UnionWith(finalMovementHighlight);
        //unionTiles.UnionWith(finalEnemyUnitsInMovementRange);
        unionTiles.UnionWith(totalAttackableTiles);
        return unionTiles;
    }
    //In:  finalMovement highlight, the attack range of the unit, and the initial node that the unit was standing on
    //Out: hashSet Node of the total attackable tiles for the unit
    //Desc: returns a set of nodes that represent the unit's total attackable tiles
    public HashSet<Node> getUnitTotalAttackableTiles(HashSet<Node> finalMovementHighlight, int attRange, Node unitInitialNode)
    {
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        HashSet<Node> seenNodes = new HashSet<Node>();
        HashSet<Node> totalAttackableTiles = new HashSet<Node>();
        foreach (Node n in finalMovementHighlight)
        {
            neighbourHash = new HashSet<Node>();
            neighbourHash.Add(n);
            for (int i = 0; i < attRange; i++)
            {
                foreach (Node t in neighbourHash)
                {
                    foreach (Node tn in t.neighbours)
                    {
                        tempNeighbourHash.Add(tn);
                    }
                }

                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();
                if (i < attRange - 1)
                {
                    seenNodes.UnionWith(neighbourHash);
                }

            }
            neighbourHash.ExceptWith(seenNodes);
            seenNodes = new HashSet<Node>();
            totalAttackableTiles.UnionWith(neighbourHash);
        }
        totalAttackableTiles.Remove(unitInitialNode);

        //Debug.Log("The unit node has this many attack options" + totalAttackableTiles.Count);
        return (totalAttackableTiles);
    }


    //In:  
    //Out: hashSet of nodes get all the attackable tiles from the current position
    //Desc: returns a set of nodes that are all the attackable tiles from the units current position
    public HashSet<Node> getUnitAttackOptionsFromPosition()
    {
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        HashSet<Node> seenNodes = new HashSet<Node>();
        Node initialNode = graph[selectedUnit.GetComponent<UnitScript>().x, selectedUnit.GetComponent<UnitScript>().y];
        int attRange = selectedUnit.GetComponent<UnitScript>().attackRange;


        neighbourHash = new HashSet<Node>();
        neighbourHash.Add(initialNode);
        for (int i = 0; i < attRange; i++)
        {
            foreach (Node t in neighbourHash)
            {
                foreach (Node tn in t.neighbours)
                {
                    tempNeighbourHash.Add(tn);
                }
            }
            neighbourHash = tempNeighbourHash;
            tempNeighbourHash = new HashSet<Node>();
            if (i < attRange - 1)
            {
                seenNodes.UnionWith(neighbourHash);
            }
        }
        neighbourHash.ExceptWith(seenNodes);
        neighbourHash.Remove(initialNode);
        return neighbourHash;
    }

    //In:  
    //Out: hashSet node that the unit is currently occupying
    //Desc: returns a set of nodes of the tile that the unit is occupying
    public HashSet<Node> getTileUnitIsOccupying()
    {

        int x = selectedUnit.GetComponent<UnitScript>().x;
        int y = selectedUnit.GetComponent<UnitScript>().y;
        HashSet<Node> singleTile = new HashSet<Node>();
        singleTile.Add(graph[x, y]);
        return singleTile;

    }

    //In:  
    //Out: void
    //Desc: highlights the selected unit's options
    public void highlightTileUnitIsOccupying()
    {
        if (selectedUnit != null)
        {
            highlightMovementRange(getTileUnitIsOccupying());
        }
    }

    //In:  
    //Out: void
    //Desc: highlights the selected unit's attackOptions from its position
    public void highlightUnitAttackOptionsFromPosition()
    {
        if (selectedUnit != null)
        {
            highlightEnemiesInRange(getUnitAttackOptionsFromPosition());
        }
    }

    //In:  Hash set of the available nodes that the unit can range
    //Out: void - it changes the quadUI property in the gameworld to visualize the selected unit's movement
    //Desc: This function highlights the selected unit's movement range
    public void highlightMovementRange(HashSet<Node> movementToHighlight)
    {
        foreach (Node n in movementToHighlight)
        {
            quadOnMap[n.x, n.y].GetComponent<Renderer>().material = blueUIMat;
            quadOnMap[n.x, n.y].GetComponent<MeshRenderer>().enabled = true;
        }
    }

    //In:  Hash set of the enemies in range of the selected Unit
    //Out: void - it changes the quadUI property in the gameworld to visualize an enemy
    //Desc: This function highlights the enemies in range once they have been added to a hashSet
    public void highlightEnemiesInRange(HashSet<Node> enemiesToHighlight)
    {
        if(toMove)
        {
            foreach (Node n in enemiesToHighlight)
            {
                quadOnMap[n.x, n.y].GetComponent<Renderer>().material = redUIMat;
                quadOnMap[n.x, n.y].GetComponent<MeshRenderer>().enabled = true;
            }
        }
        if(!toMove)
        {
            foreach (Node n in enemiesToHighlight)
            {
                quadOnMap[n.x, n.y].GetComponent<Renderer>().material = colorPreviousAttack;
                quadOnMap[n.x, n.y].GetComponent<MeshRenderer>().enabled = true;
            }
        }
        
    }

    //Desc: disables the highlight
    public void disableHighlightUnitRange()
    {
        foreach (GameObject quad in quadOnMap)
        {
            if (quad.GetComponent<Renderer>().enabled == true)
            {
                quad.GetComponent<Renderer>().enabled = false;
            }
        }
    }

    //Desc: moves the unit then finalizes the movement
    public IEnumerator moveUnitAndFinalize()
    {
        if (GMS.isPlayerTurn)
        {
            disableHighlightUnitRange();
            disableUnitUIRoute();
        }
        while (selectedUnit.GetComponent<UnitScript>().movementQueue.Count != 0)
        {
            yield return new WaitForEndOfFrame();
        }
        finalizeMovementPosition();
        selectedUnit.GetComponent<UnitScript>().setSelectedAnimation();
    }


    //In:  both units engaged in a battle
    //Out:  
    //Desc: deselects the selected unit after the action has been taken
    public IEnumerator deselectAfterMovements(GameObject unit, GameObject enemy)
    {
        //selectedSound.Play();
        selectedUnit.GetComponent<UnitScript>().setMovementState(3);

        if (GMS.isPlayerTurn)
        {
            disableHighlightUnitRange();
            disableUnitUIRoute();
        }
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

    //Desc: de-selects the unit
    public void deselectUnit()
    {
        // if (selectedUnit.GetComponent<UnitScript>().unitMoveState == selectedUnit.GetComponent<UnitScript>().getMovementStateEnum(1) ||
        //     selectedUnit.GetComponent<UnitScript>().unitMoveState == selectedUnit.GetComponent<UnitScript>().getMovementStateEnum(3))
        //  {
        if (GMS.isPlayerTurn)
        {
            disableHighlightUnitRange();
            disableUnitUIRoute();
        }
        selectedUnit.GetComponent<UnitScript>().setMovementState(0);

        selectedUnit = null;
        isSelected = false;

        GMS.endTurn();
        //  }
    }


    //In:  
    //Out: true if there is a tile that was clicked that the unit can move to, false otherwise 
    //Desc: checks if the tile that was clicked is move-able for the selected unit
    public bool selectTileToMoveTo() //----------------------- IMPORTANTÍSSIMO
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (hit.transform.gameObject.CompareTag("Tile"))
                {

                    int clickedTileX = hit.transform.GetComponent<ClickableTileScript>().tileX;
                    int clickedTileY = hit.transform.GetComponent<ClickableTileScript>().tileY;
                    Node nodeToCheck = graph[clickedTileX, clickedTileY];
                    //var unitScript = selectedUnit.GetComponent<UnitScript>();

                    if (selectedUnitMoveRange.Contains(nodeToCheck))
                    {
                        if ((hit.transform.gameObject.GetComponent<ClickableTileScript>().unitOnTile == null
                             || hit.transform.gameObject.GetComponent<ClickableTileScript>().unitOnTile == selectedUnit) && (selectedUnitMoveRange.Contains(nodeToCheck)))
                        {
                            // Debug.Log("We have finally selected the tile to move to");
                            generatePathTo(clickedTileX, clickedTileY); //IMPORTANTÍSSIMO
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }


}
