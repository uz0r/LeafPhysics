using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeController : MonoBehaviour
{
    public Button btnGolden;
    public TextMeshProUGUI textGolden;
    public Button btnRed;
    public TextMeshProUGUI textRed;

    public ParticleSystem psGolden;
    public ParticleSystem psRed;

    private int lvlGonden = 1;
    private int lvlRed = 1;
    void Start()
    {
        RefreshGolden();
        RefreshRed();
        btnGolden.onClick.AddListener(
            () =>
            {
                if (lvlGonden * 100 < ParticlesCatcher.count)
                {
                    ParticlesCatcher.count -= lvlGonden * 100;
                    lvlGonden++;
                    var e = psGolden.emission;
                    e.rateOverTimeMultiplier = lvlGonden * 5;
                    RefreshGolden();
                }
            }
            );
        btnRed.onClick.AddListener(
            () =>
            {
                if (lvlRed * 5000 < ParticlesCatcher.count)
                {
                    ParticlesCatcher.count -= lvlRed * 5000;
                    lvlRed++;
                    var e = psRed.emission;
                    e.rateOverTimeMultiplier = lvlRed * 2;
                    RefreshRed();
                }
            }
        );
    }

    public void RefreshGolden()
    {
        textGolden.text = "Upgrade golden leafs income " + lvlGonden + " => " + (lvlGonden + 1) + "\n(Cost "+ lvlGonden * 100 + ")";
    }

    public void RefreshRed()
    {
        if(lvlRed <= 1)
            textRed.text = "Unlock red leafs\n(Cost " + lvlRed * 5000 + ")";
        else
            textRed.text = "Upgrade golden leafs income " + lvlRed + " => " + (lvlRed + 1) + "\n(Cost "+ lvlRed * 5000 + ")";
 
    }

    // Update is called once per frame
    void Update()
    {
        btnGolden.interactable = lvlGonden * 100 <= ParticlesCatcher.count;
        btnRed.interactable = lvlRed * 5000 <= ParticlesCatcher.count;

    }
}
