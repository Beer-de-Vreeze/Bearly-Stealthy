using UnityEngine;
using VInspector;

public class ShaderApplier : MonoBehaviour
{
    //grab the material of the object turn it into a texture
    //and apply the shader to it
    //add the texture to the shader
    //do it to all the objects in the scene
    public Shader shader;
    public Texture texture;

    [Button("Apply Shader")]
    private void ApplyShader()
    {
        //get all the objects in the scene
        GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsSortMode.None
        );
        foreach (GameObject obj in allObjects)
        {
            //get the material of the object
            Material mat = obj.GetComponent<Renderer>().material;
            //create a new texture from the material
            Texture2D tex = new Texture2D(mat.mainTexture.width, mat.mainTexture.height);
            //apply the material to the texture
            tex = mat.mainTexture as Texture2D;
            //apply the shader to the material
            mat.shader = shader;
            //apply the texture to the shader
            mat.SetTexture("_MainTex", tex);
        }
    }
}
