using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggleSwitch : MonoBehaviour
{
    [Header("开关信息")]
    public string switchName = "SW-A";

    public bool activated=false;
    private Light childLight;
    private void Start()
    {
        childLight = transform.GetChild(0).GetComponent<Light>();
        Debug.Log(childLight.name);
       childLight.gameObject.GetComponent<Light>().enabled=false;
    }

    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player"))
        { return; }
        Activate(this.gameObject.name);

    }
    public void Activate(String souce)
    {
        if(activated)
        {
            return;
        }
        activated = true;
        childLight.gameObject.GetComponent<Light>().enabled=true;
        Debug.Log("开关" + souce + "被激活");

    }
    public void ResetSwitch()
    {
        activated = false;
        childLight.gameObject.GetComponent<Light>().enabled = false;
    }
}
