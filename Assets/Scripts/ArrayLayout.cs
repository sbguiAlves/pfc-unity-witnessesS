using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ArrayLayout {
    [System.Serializable]
    public struct rowData{
        public int[] row;
    }

    public rowData[] rows = new rowData[8]; //grid of 10x10
}
