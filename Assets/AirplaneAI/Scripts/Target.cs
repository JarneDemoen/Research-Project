#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    [SerializeField]
    GameObject original;
    bool collideswithSurface = false;

    public GameObject InstantiateTarget()
    {
        float randomX = Random.Range(-1500,1200);
        float randomY = Random.Range(300,700);
        float randomZ = Random.Range(-900, 1400);
        GameObject target = Instantiate(original, new Vector3(randomX, randomY, randomZ), Quaternion.Euler(0, 0, 0));

        // if the object collides with the tag "Surface", change the position of the target again
        if (collideswithSurface == true)
        {
            InstantiateTarget();
        }
        return target;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Surface"))
        {
            collideswithSurface = true;
        }
    }
}
#endif