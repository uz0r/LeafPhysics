using System;
using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class ParticlesCatcher : MonoBehaviour
{
    public int leafCost = 1;
    public TMPro.TextMeshProUGUI counterText;
    public DamageNumber numberPrefab;
    public static int count = 0;
    private DamageNumber damageNumber;

    ParticleSystem ps;
    List<ParticleSystem.Particle> enter = new List<ParticleSystem.Particle>();
    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        //numberPrefab = FindObjectOfType<DamageNumber>();
    }
    private void OnParticleTrigger()
    {
        int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);
        for (int i = 0; i < numEnter; i++)
        {
            ParticleSystem.Particle p = enter[i];
            p.remainingLifetime = 0;
            enter[i] = p;
            var reward = (int)Random.Range(leafCost, leafCost * 1.05f);
            damageNumber = numberPrefab.Spawn(p.position, "+");
            damageNumber.number = reward;
            damageNumber.enableRightText = true;
            damageNumber.rightText = " leaf";
            count += reward;
        }
        
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        counterText.text = "leafs count: " + count;
    }
}
