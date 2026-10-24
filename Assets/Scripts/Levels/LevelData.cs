using UnityEngine;

namespace SayItAgain
{
    /// <summary>One level as content. Level select lists these; locked ones show "Coming soon".</summary>
    [CreateAssetMenu(menuName = "Say It Again/Level Data", fileName = "Level_")]
    public sealed class LevelData : ScriptableObject
    {
        public int number = 1;
        public string nameKey = "level1_name";
        public string lessonKey = "level1_lesson";
        public bool playable = true;
        public string gameplayScene = "Gameplay_Test";

        public WeatherConfig weather;
        public OrderData orders;
        [Tooltip("Seconds on the storm clock.")]
        public float stormSeconds = 300f;
        public int villagerCount = 6;
        [Tooltip("Saved villagers needed to win, as a fraction of villagerCount.")]
        [Range(0f, 1f)] public float winFraction = 0.6f;

        [Header("Rumours")]
        public bool rumorsEnabled;
        public Vector2 rumorIntervalSeconds = new Vector2(20f, 35f);
        [Tooltip("Sends one fake 'HQ change of plan' message with a wrong code word.")]
        public bool fakeHqMessage;

        [Header("Tools")]
        [Tooltip("How many times the Aid Tent can ask HQ to confirm one slot.")]
        public int askHqUses = 2;
    }
}
