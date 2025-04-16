using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Creates curved text along a rainbow-like arc for TMP_Text components.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class CurveText : MonoBehaviour
{
    [Tooltip("Radius of the rainbow curve in world units")]
    [Range(50f, 500f)]
    public float radius = 200f;

    [Tooltip("Flip the direction of the curve")]
    public bool flip = false;

    [Tooltip("Angle in degrees for text distribution (180 = half circle)")]
    [Range(0f, 360f)]
    public float arcAngle = 180f;

    [Tooltip("Curvature of the rainbow (higher = more curved)")]
    [Range(0.1f, 2f)]
    public float curvature = 1f;

    private TMP_Text _tmpText;
    private TMP_Text TmpText => _tmpText ??= GetComponent<TMP_Text>();

    private void OnEnable()
    {
        _tmpText = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        CurveTextMesh();
    }

    /// <summary>
    /// Curves the text mesh along a rainbow arc
    /// </summary>
    private void CurveTextMesh()
    {
        if (!TmpText)
            return;

        TmpText.ForceMeshUpdate();
        var textInfo = TmpText.textInfo;
        var charCount = textInfo.characterCount;

        if (charCount == 0)
            return;

        // Calculate width of the rainbow
        float width = radius * 2;
        float direction = flip ? -1 : 1;

        for (int i = 0; i < charCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
                continue;

            // Calculate normalized position along the rainbow (0 to 1)
            float t = charCount <= 1 ? 0.5f : (float)i / (charCount - 1);

            // Map t to range -1 to 1
            float x = (t * 2 - 1) * direction;

            // Calculate rainbow position using parabolic function
            // y = -curvature * x²
            float y = -curvature * (x * x) + 1;

            // Scale to desired size
            Vector3 charPos = new Vector3(x * width * 0.5f, y * radius * 0.5f, 0);

            // Calculate tangent angle for character rotation
            float tangentAngle = Mathf.Atan(-2 * curvature * x) * Mathf.Rad2Deg * direction;

            int vertexIndex = charInfo.vertexIndex;
            for (int j = 0; j < 4; j++)
            {
                Vector3 orig = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices[
                    vertexIndex + j
                ];
                textInfo.meshInfo[charInfo.materialReferenceIndex].vertices[vertexIndex + j] =
                    Quaternion.Euler(0, 0, tangentAngle) * (orig - charInfo.bottomLeft) + charPos;
            }
        }

        // Apply mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            TmpText.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}
