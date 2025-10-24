using UnityEngine;

[CreateAssetMenu(fileName = "SplineDataContainer", menuName = "Curvy/Spline Data Container", order = 1)]
public class SplineDataContainer : ScriptableObject
{
    [Header("Cached Spline Data")]
    public Vector3[] positions;
    public Vector3[] tangents;
    public Vector3[] upVectors;
    public float[] distances;
    
    [Header("Spline Metadata")]
    public float totalDistance;
    public int sampleCount;
    public float resolution;
    
    // Version field to help detect when cache needs updating
    public string version = "1.0";
    
    // Method to initialize arrays with a specific size
    public void InitializeArrays(int size)
    {
        positions = new Vector3[size];
        tangents = new Vector3[size];
        upVectors = new Vector3[size];
        distances = new float[size];
        sampleCount = size;
    }
    
    // Method to check if the data is valid
    public bool IsValid()
    {
        return positions != null && tangents != null && upVectors != null && 
               distances != null && positions.Length > 0 && 
               positions.Length == tangents.Length && 
               positions.Length == upVectors.Length && 
               positions.Length == distances.Length;
    }
}