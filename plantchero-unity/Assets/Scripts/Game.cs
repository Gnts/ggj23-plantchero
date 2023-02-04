using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public enum GameState {
    MENU,
    IN_PROGRESS,
    END
}
public class Game : MonoBehaviour
{
    public GameState state;
    public const int MaxTime = 60;
    public float time;
    public TextMeshProUGUI ui_timer;
    
    void Start()
    {
        time = MaxTime;
    }
    void Update()
    {
        time -= Time.deltaTime;
        time = Math.Clamp(time, 0, MaxTime);
        ui_timer.text = ((int) time).ToString(CultureInfo.InvariantCulture);
    }
}