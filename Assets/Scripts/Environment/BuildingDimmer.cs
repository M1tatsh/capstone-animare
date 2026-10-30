using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Rotation;
using UnityEngine;

namespace Gameplay.Environment
{
    /// <summary>
    /// Dims background buildings (everything under Buildings GO)
    /// </summary>
    public class BuildingDimmer : MonoBehaviour
    {
        /// <summary>
        /// One building material and the dimmed copy made from it.
        /// </summary>
        private class DimmedMaterial
        {
            public Material copy;
            public Color originalColor;
            public int colorPropertyId;
            public bool canUseHaze;
            public Color originalEmission;
        }

        //URP shaders call their main color _BaseColor, older shaders call it _Color.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Buildings")]
        [Tooltip("Optional. The parent of the buildings to dim. Leave empty to use the BlockGrid's Buildings Parent.")]
        [SerializeField] private Transform buildingsParent;

        [Header("Dimming")]
        [Tooltip("How much the buildings are dimmed. 0 = their normal look, 1 = fully tinted by Dim Color.")]
        [Range(0f, 1f)]
        [SerializeField] private float dimAmount = 0.5f;

        [Tooltip("The buildings' colors are multiplied by this. Darker means dimmer; a slightly blue grey looks like distance.")]
        [SerializeField] private Color dimColor = new Color(0.55f, 0.6f, 0.72f, 1f);

        [Tooltip("Lifts the buildings' darkest parts toward Haze Color, like fog in front of far-away objects. 0 = no haze. " +
            "Only works on materials whose shader has emission.")]
        [Range(0f, 1f)]
        [SerializeField] private float hazeAmount = 0.15f;

        [SerializeField] private Color hazeColor = new Color(0.45f, 0.5f, 0.6f, 1f);

        [Tooltip("Seconds SetDimAmount takes to fade to a new amount, unless another time is given.")]
        [SerializeField] private float fadeDuration = 0.5f;

        
        

        //each original material is copied only once, however many buildings use it.
        private readonly Dictionary<Material, DimmedMaterial> dimmedMaterials = new Dictionary<Material, DimmedMaterial>();

        //every building renderer and the materials it had before, so they can be put back.
        private readonly List<Renderer> buildingRenderers = new List<Renderer>();
        private readonly List<Material[]> originalMaterials = new List<Material[]>();

        //sprites in the buildings are tinted through their color instead of a material.
        private readonly List<SpriteRenderer> buildingSprites = new List<SpriteRenderer>();
        private readonly List<Color> originalSpriteColors = new List<Color>();

        //the amount being shown right now (it differs from dimAmount while fading).
        private float currentDimAmount;
        private Tween fadeTween;
        private bool isApplied;

        public float DimAmount => dimAmount;

        void OnEnable()
        {
            if (buildingsParent == null)
            {
                BlockGrid grid = FindAnyObjectByType<BlockGrid>();
                if (grid != null)
                    buildingsParent = grid.BuildingsParent;
            }

            if (buildingsParent == null)
            {
                Debug.LogWarning("BuildingDimmer: No buildings to dim. Set Buildings Parent here or on the BlockGrid.", this);
                return;
            }

            currentDimAmount = dimAmount;
            ApplyDimming();
        }

        void OnDisable()
        {
            if (fadeTween != null && fadeTween.IsActive())
                fadeTween.Kill();

            RestoreBuildings();
        }

        //changing the sliders in the Inspector while playing shows the result right away.
        void OnValidate()
        {
            if (!Application.isPlaying || !isApplied)
                return;

            if (fadeTween == null || !fadeTween.IsActive())
                currentDimAmount = dimAmount;

            UpdateColors();
        }

        /// <summary>
        /// Fades the dimming to a new amount
        /// </summary>
        /// <param name="duration">Seconds for the fade.</param>
        public void SetDimAmount(float amount, float duration)
        {
            dimAmount = Mathf.Clamp01(amount);

            if (duration < 0f)
                duration = fadeDuration;

            if (fadeTween != null && fadeTween.IsActive())
                fadeTween.Kill();

            if (duration <= 0f || !isApplied)
            {
                currentDimAmount = dimAmount;
                UpdateColors();
                return;
            }

            fadeTween = DOTween.To(() => currentDimAmount, value => { currentDimAmount = value; UpdateColors(); }, dimAmount, duration)
                .SetTarget(this);
        }

        /// <summary>
        /// For Unity event calls.
        /// </summary>
        public void SetDimAmount(float amount)
        {
            SetDimAmount(amount, -1f);
        }

        /// <summary>
        /// Looks for buildings again in case buildings are added at runtime.
        /// </summary>
        public void Refresh()
        {
            RestoreBuildings();
            ApplyDimming();
        }

        /// <summary>
        /// Gives every building renderer dimmed copies of its materials.
        /// </summary>
        private void ApplyDimming()
        {
            if (buildingsParent == null)
                return;

            foreach (Renderer buildingRenderer in buildingsParent.GetComponentsInChildren<Renderer>(true))
            {
                if (buildingRenderer is SpriteRenderer sprite)
                {
                    buildingSprites.Add(sprite);
                    originalSpriteColors.Add(sprite.color);
                    continue;
                }

                //skip particles and trails
                if (!(buildingRenderer is MeshRenderer) && !(buildingRenderer is SkinnedMeshRenderer))
                    continue;

                Material[] materials = buildingRenderer.sharedMaterials;
                Material[] dimmedCopies = new Material[materials.Length];

                for (int slot = 0; slot < materials.Length; slot++)
                {
                    dimmedCopies[slot] = GetDimmedCopy(materials[slot]);
                }

                buildingRenderers.Add(buildingRenderer);
                originalMaterials.Add(materials);
                buildingRenderer.sharedMaterials = dimmedCopies;
            }

            isApplied = true;
            UpdateColors();
        }

        /// <summary>
        /// The dimmed copy of a material, made the first time it is asked for.
        /// </summary>
        private Material GetDimmedCopy(Material original)
        {
            if (original == null)
                return null;

            if (dimmedMaterials.TryGetValue(original, out DimmedMaterial existing))
                return existing.copy;

            DimmedMaterial dimmed = new DimmedMaterial();
            dimmed.copy = new Material(original);
            dimmed.copy.name = original.name + " (Dimmed)";

            if (original.HasProperty(BaseColorId))
                dimmed.colorPropertyId = BaseColorId;
            else if (original.HasProperty(ColorId))
                dimmed.colorPropertyId = ColorId;
            else
                dimmed.colorPropertyId = -1;
                

            dimmed.originalColor = dimmed.colorPropertyId != -1 ? original.GetColor(dimmed.colorPropertyId) : Color.white;

            //haze uses the emission color. A material that already glows keeps its own emission.
            bool alreadyGlows = original.IsKeywordEnabled("_EMISSION");
            dimmed.canUseHaze = original.HasProperty(EmissionColorId) && !alreadyGlows;
            dimmed.originalEmission = original.HasProperty(EmissionColorId) ? original.GetColor(EmissionColorId) : Color.black;

            if (dimmed.canUseHaze)
            {
                dimmed.copy.EnableKeyword("_EMISSION");
                //only the color is used, not baked lighting.
                dimmed.copy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }



            dimmedMaterials[original] = dimmed;
            return dimmed.copy;
        }

        /// <summary>
        /// Sets the colors of every dimmed copy (and sprite) for the current amount.
        /// </summary>
        private void UpdateColors()
        {
            Color tint = Color.Lerp(Color.white, dimColor, currentDimAmount);
            Color haze = hazeColor * (hazeAmount * currentDimAmount);

            foreach (DimmedMaterial dimmed in dimmedMaterials.Values)
            {
                if (dimmed.colorPropertyId != -1)
                {
                    Color tintedColor = dimmed.originalColor * tint;
                    tintedColor.a = dimmed.originalColor.a;
                    dimmed.copy.SetColor(dimmed.colorPropertyId, tintedColor);
                }

                if (dimmed.canUseHaze)
                    dimmed.copy.SetColor(EmissionColorId, haze);
            }

            for (int i = 0; i < buildingSprites.Count; i++)
            {
                if (buildingSprites[i] == null)
                    continue;

                Color tintedColor = originalSpriteColors[i] * tint;
                tintedColor.a = originalSpriteColors[i].a;
                buildingSprites[i].color = tintedColor;
            }
        }

        /// <summary>
        /// Puts every building's own materials and colors back, and throws the copies away.
        /// </summary>
        private void RestoreBuildings()
        {
            for (int i = 0; i < buildingRenderers.Count; i++)
            {
                if (buildingRenderers[i] != null)
                    buildingRenderers[i].sharedMaterials = originalMaterials[i];
            }
            for (int i = 0; i < buildingSprites.Count; i++)
            {
                if (buildingSprites[i] != null)
                    buildingSprites[i].color = originalSpriteColors[i];
            }

            foreach (DimmedMaterial dimmed in dimmedMaterials.Values)
            {
                if (dimmed.copy != null)
                    Destroy(dimmed.copy);
            }

            buildingRenderers.Clear();
            originalMaterials.Clear();
            buildingSprites.Clear();
            originalSpriteColors.Clear();
            dimmedMaterials.Clear();
            isApplied = false;
        }
    }
}
