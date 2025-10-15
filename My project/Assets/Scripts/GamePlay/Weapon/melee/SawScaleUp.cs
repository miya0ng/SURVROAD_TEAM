using UnityEngine;

public class SawScaleUp : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (GetComponent<WeaponDriver>().CurLevelData.Level == 5)
        {
            gameObject.transform.localScale = new Vector3(5f, 5f, 5f);
        }
    }
}
