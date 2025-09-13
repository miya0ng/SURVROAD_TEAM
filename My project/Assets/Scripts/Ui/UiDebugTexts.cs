using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UiDebugTexts : MonoBehaviour
{
    public TextMeshProUGUI hp;
    public TextMeshProUGUI speed;
    public TextMeshProUGUI rotation;


    public PlayerController player;
    public PlayerBehaviour playerHp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        rotation.text = "Rotation: " + player.rotationSpeed;
        speed.text = "Speed: " + player.curMoveSpeed;
        hp.text = "Hp: " + playerHp.curHp + "/" + playerHp.maxHp;
    }
}
