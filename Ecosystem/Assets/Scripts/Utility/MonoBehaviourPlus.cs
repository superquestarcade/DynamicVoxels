using UnityEngine;

/// <summary>
/// Adds simple debugging filtering
/// </summary>
public class MonoBehaviourPlus : MonoBehaviour
{
	public DebugLevel debugLevel;
	public bool DebugMessages => debugLevel > DebugLevel.None;
}

public enum DebugLevel
{
	None,
	Minimum,
	Moderate,
	All
}