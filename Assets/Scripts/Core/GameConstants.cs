using UnityEngine;

public static class GameConstants
{
    // ================== LANE & MOVEMENT ==================
    public const float LaneDistance = 3.8f;           // Khoảng cách giữa các lane
    public const int   MaxLaneIndex = 1;              // Lane từ -1 đến +1

    // ================== LAYERS ==================
    public static class Layers
    {
        public const string Player   = "Player";
        public const string Ground   = "Ground";
        public const string Obstacle = "Obstacle";
        public const string Coin     = "Coin";
        public const string PowerUp  = "PowerUp";
    }

    // ================== TAGS ==================
    public static class Tags
    {
        public const string Player   = "Player";
        public const string Obstacle = "Obstacle";
        public const string Coin     = "Coin";
        public const string PowerUp  = "PowerUp";
    }

    // ================== POOL KEYS ==================
    public static class PoolKeys
    {
        public const string Coin = "Coin";
    }

    // ================== ANIMATION HASH (đã sửa) ==================
    public static class Anim
    {
        public static readonly int IsRunning  = Animator.StringToHash("IsRunning");
        public static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        public static readonly int Jump       = Animator.StringToHash("Jump");
        public static readonly int Roll       = Animator.StringToHash("Roll");
        public static readonly int Death      = Animator.StringToHash("Death");
    }
}