using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickableTileScript : MonoBehaviour    
{
    //Determinate when a tile has been clicked - Não vai mais determinar qual tile foi apertado inicialmente
    //The x and y co-ordinate of the tile
    public int tileX;
    public int tileY;
    //The unit on the tile
    public GameObject unitOnTile;
    public tileMapScript map;
}
