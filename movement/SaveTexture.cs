using UnityEngine;

public class SaveTexture : MonoBehaviour
{
    // Folder name where materials are located
    public string materialsFolderName = "";

    // Start is called before the first frame update
    void Start()
    {
        // Load all materials from the specified folder
        Material[] materials = Resources.LoadAll<Material>(materialsFolderName);

        // Loop through each material
        foreach (Material mat in materials)
        {
            // Extract material number from its name
            string materialName = mat.name.Replace("Material_", "");
            // Load texture using naming convention
            Texture2D texture = Resources.Load<Texture2D>("Image_" + materialName);
            if (texture != null)
            {
                // Assign texture to material's main texture property
                mat.mainTexture = texture;
                Debug.Log("Texture assigned to " + mat.name);

                // Save changes to the material asset
                UnityEditor.EditorUtility.SetDirty(mat);
            }
            else
            {
                Debug.LogError("Texture Image_" + materialName + " not found.");
            }
        }

        // Save changes to the assets
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
}