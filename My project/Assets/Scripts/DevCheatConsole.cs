using UnityEngine;

public class DevCheatConsole : MonoBehaviour
{
    [Header("Bindings")]
    public EquipManager equip;
    public WaveManager wave;
    public EnemySpawner spawner;
    public PlayerBehaviour playerHp;
    public WeaponLibrary weaponLibrary;

    [Header("Toggles")]
    public bool visible = false;
    public bool godMode = false;

    // inputs
    private float addPartsValue = 10f;
    private int addEnemies = 10;
    private float healAmount = 50f;
    private float timeScale = 1f;

    // 무기 선택 (인스펙터에서 SO 드래그해서 쓰는 게 제일 간단)
    [Header("Pick Weapon")]
    public WeaponSO pickSO;     // 인스펙터에서 직접 드래그
    [Range(1, 10)] public int pickLevel = 1; // 레벨 선택

    void Awake()
    {
        if (!equip)
        {
            var player = GameObject.FindWithTag("Player");
            if (player) equip = player.GetComponentInChildren<EquipManager>();
            if (player && !playerHp) playerHp = player.GetComponent<PlayerBehaviour>();
        }
        if (!wave)
        {
            var wm = GameObject.FindWithTag("WaveManager");
            if (wm) wave = wm.GetComponent<WaveManager>();
        }
        if (!spawner)
        {
            var es = GameObject.FindWithTag("EnemySpawner");
            if (es) spawner = es.GetComponent<EnemySpawner>();
        }
        timeScale = Time.timeScale;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;
        if (godMode && playerHp) playerHp.curHp = playerHp.maxHp; // 간단 갓모드
#endif
    }

    void OnGUI()
    {
#if UNITY_EDITOR
        if (!visible) return;

        const int w = 380;
        const int h = 440;
        Rect r = new Rect(10, 10, w, h);
        GUILayout.BeginArea(r, GUI.skin.box);
        GUILayout.Label("<b>DEV CHEAT CONSOLE</b>");

        GUILayout.Space(6);
        GUILayout.Label($"TimeScale: {timeScale:F2}");
        timeScale = GUILayout.HorizontalSlider(timeScale, 0.1f, 3f);
        if (GUILayout.Button("Apply TimeScale")) Time.timeScale = timeScale;

        GUILayout.Space(6);
        godMode = GUILayout.Toggle(godMode, "God Mode (keep HP full)");

        GUILayout.Space(6);
        GUILayout.Label($"Add Parts: {addPartsValue:F0}");
        addPartsValue = Mathf.Round(GUILayout.HorizontalSlider(addPartsValue, 1f, 100f));
        using (new GUILayout.HorizontalScope())
        {
            GUI.enabled = equip != null;
            if (GUILayout.Button("+ Parts")) equip?.AddParts(addPartsValue);
            GUI.enabled = true;
        }

        GUILayout.Space(6);
        using (new GUILayout.HorizontalScope())
        {
            GUI.enabled = playerHp != null;
            GUILayout.Label($"Heal: {healAmount:F0}");
            healAmount = Mathf.Round(GUILayout.HorizontalSlider(healAmount, 10f, 200f));
            if (GUILayout.Button("Heal")) playerHp?.Heal(healAmount);
            GUI.enabled = true;
        }

        GUILayout.Space(6);
        using (new GUILayout.HorizontalScope())
        {
            GUI.enabled = wave != null;
            // 네 프로젝트 함수명에 맞게 교체: NextWave() / StartNextWave() 등
            if (GUILayout.Button("Next Wave")) wave?.NextWave();
            GUI.enabled = true;
        }

        GUILayout.Space(6);
        using (new GUILayout.HorizontalScope())
        {
            GUI.enabled = spawner != null;
            GUILayout.Label($"Spawn Enemies: {addEnemies}");
            addEnemies = Mathf.RoundToInt(GUILayout.HorizontalSlider(addEnemies, 1, 50));
            // 네 스포너에 디버그 스폰 함수가 없다면 하나 추가(아래 참고)
            if (GUILayout.Button("Spawn Now"))
            {
                // 예시 1) spawner.DebugSpawn(addEnemies);
                // 예시 2) spawner.ForceSpawn(addEnemies);
            }
            GUI.enabled = true;
        }

        GUILayout.Space(10);
        GUILayout.Label("<b>Equip New Weapon</b>");
        using (new GUILayout.HorizontalScope())
        {
            GUI.enabled = equip != null && (pickSO != null || (weaponLibrary != null && weaponLibrary.weapons.Count > 0));

            if (GUILayout.Button("Equip (pickSO or Library[0])"))
            {
                var so = pickSO;
                if (so == null && weaponLibrary != null && weaponLibrary.weapons.Count > 0)
                    so = weaponLibrary.weapons[0];

                if (so != null)
                {
                    // 팝업/게이지 무시하고 즉시 장착
                    equip.ForceEquipNew(so, pickLevel);

                    // 팝업 흐름(게이지 꽉 찼을 때 선택 적용)
                    // equip.ApplyLevelUpChoice_EquipNew(so, pickLevel);
                }
            }
            GUI.enabled = true;
        }

        GUILayout.EndArea();
#endif
    }
}
