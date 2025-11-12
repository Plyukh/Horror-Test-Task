using UnityEngine;

/// <summary>
/// Helper class for managing outline effects on GameObjects
/// </summary>
public static class OutlineHelper
{
    /// <summary>
    /// Sets outline width recursively on all Outline components in the GameObject hierarchy
    /// </summary>
    public static void SetOutlineWidthRecursive(GameObject target, float width)
    {
        if (target == null) return;

        var parentOutline = target.GetComponentInParent<Outline>();
        if (parentOutline != null)
        {
            try 
            { 
                parentOutline.OutlineWidth = width; 
            }
            catch 
            { 
                // Outline component may not be properly initialized
            }
        }

        var outlines = target.GetComponentsInChildren<Outline>(true);
        if (outlines != null && outlines.Length > 0)
        {
            foreach (var outline in outlines)
            {
                try 
                { 
                    outline.OutlineWidth = width; 
                }
                catch 
                { 
                    // Outline component may not be properly initialized
                }
            }
        }
    }
}


