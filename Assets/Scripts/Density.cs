
using UnityEngine;
public abstract class Density
{
    public abstract float GetValue(int x, int y, int z);
    public abstract Vector3 GetBounds();
}
