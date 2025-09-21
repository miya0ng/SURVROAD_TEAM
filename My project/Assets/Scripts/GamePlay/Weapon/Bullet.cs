using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float timer;
    private float lifeTime = 0.05f;

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        gameObject.SetActive(false);
    }
}

