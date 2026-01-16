using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Tooltip("The universal t value for interpolation. Lower is more accurate, higher is smoother. Too high may feel sluggish.")]
    public float smoothingConstant;
    public static float InterpolationConstant => instance.smoothingConstant;

    [Tooltip("The amount by which to extend certain raycasts to avoid clipping issues.")]
    public float raycastPad = 0.001f;
    public static float RaycastPad => instance.raycastPad;

    public Vector3 gravity = new(0, -30, 0);
    public static Vector3 Gravity => instance.gravity;

    void Awake()
    {
        // Singleton system
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
}