using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentSpawner : MonoBehaviour
{
    [SerializeField] GameObject agentPrefab;
    [SerializeField] int numberOfAgents = 39;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < numberOfAgents; i++)
        {
            Instantiate(agentPrefab, transform.position, transform.rotation);
        }
    }
}
