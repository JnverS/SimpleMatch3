using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectible : GameItem
{
    public bool clearedByBomb = false;
    public bool clearedByBottom = false;
    // Start is called before the first frame update
    void Start()
    {
        matchValue = MatchValue.None;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
