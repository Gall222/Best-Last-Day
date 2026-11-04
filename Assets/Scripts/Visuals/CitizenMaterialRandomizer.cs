
using UnityEngine;

namespace Visuals
{
    public class CitizenMaterialRandomizer : MonoBehaviour
    {
        [Header("Material folders in Resources")]
        [SerializeField] private string tshirtFolder = "Materials/Tshirt";
        [SerializeField] private string pantsFolder = "Materials/Pants";
        
        private static Material[] _tshirtMaterials;
        private static Material[] _pantsMaterials;

        private static bool _materialsLoaded;

        private void Awake()
        {
            LoadMaterials();
            ApplyRandomMaterials();
        }

        private static void LoadMaterials()
        {
            if (_materialsLoaded)
                return;

            _tshirtMaterials = Resources.LoadAll<Material>(
                "Materials/Tshirt"
            );

            _pantsMaterials = Resources.LoadAll<Material>(
                "Materials/Pants"
            );

            _materialsLoaded = true;
        }

        private void ApplyRandomMaterials()
        {
            SkinnedMeshRenderer renderer =
                GetComponentInChildren<SkinnedMeshRenderer>();

            if (renderer == null)
            {
                Debug.LogError(
                    "CitizenMaterialRandomizer: SkinnedMeshRenderer not found!",
                    this
                );

                return;
            }

            // Получаем копию массива материалов.
            Material[] materials = renderer.sharedMaterials;

            if (materials.Length <= 10)
            {
                Debug.LogError(
                    "CitizenMaterialRandomizer: недостаточно слотов материалов!",
                    this
                );

                return;
            }

            AssignRandomMaterial(materials, 0, _tshirtMaterials);
            AssignRandomMaterial(materials, 10, _pantsMaterials);

            renderer.sharedMaterials = materials;
        }

        private void AssignRandomMaterial(
            Material[] materials,
            int index,
            Material[] variants
        )
        {
            if (variants == null || variants.Length == 0)
            {
                Debug.LogWarning(
                    $"CitizenMaterialRandomizer: нет материалов для слота {index}",
                    this
                );

                return;
            }

            materials[index] = variants[Random.Range(0, variants.Length)];
        }
    }
}
