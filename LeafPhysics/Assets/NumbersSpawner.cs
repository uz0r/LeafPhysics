using System;
using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class NumbersSpawner : MonoBehaviour
{
    public DamageNumber numberPrefab;
    private DamageNumber damageNumber;
    
    private void Awake()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount % 59 == 1)
            damageNumber = numberPrefab.Spawn(transform.position, Random.Range(1,100));
    }
}
