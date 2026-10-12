using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Shows the whole player as a dark silhouette while any part of their body is hidden behind geometry
    /// (PlayerMovement.IsBodyHidden), and reveals them all at once when their whole body is in view.
    ///
    /// The change is faded with DOTween: the player's own colors are tinted toward the silhouette color and back.
    /// If a silhouette material is assigned, it is swapped in once the fade to dark has finished,
    /// and swapped out again before the fade back starts.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerSilhouette : MonoBehaviour
    {
        [Tooltip("Optional. For example a transparent URP Unlit material. Leave empty to only tint the player's own materials.")]
        [SerializeField] private Material silhouetteMaterial;
        [Tooltip("The color the player fades to.")]
        [SerializeField] private Color silhouetteColor = new Color(0.1f, 0.1f, 0.15f, 1f);
        [Tooltip("How long the fade in and the fade out take, in seconds.")]
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;

        //URP shaders call their main color _BaseColor, older shaders call it _Color.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private PlayerMovement playerMovement;
        private Renderer[] playerMeshes;
        private Material[][] originalMaterials;
        private Color[][] originalColors;
        private MaterialPropertyBlock tintBlock;
        private MaterialPropertyBlock emptyBlock;

        //0 = the player's normal look, 1 = full silhouette. The tween moves this between the two.
        private float silhouetteAmount;
        private bool isShowingSilhouette;
        private bool isUsingSilhouetteMaterial;
        private Tween fadeTween;

        void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();

            //only meshes, so particle effects like the footsteps keep their own look.
            //true = also find the meshes of forms that are switched off right now.
            List<Renderer> meshes = new List<Renderer>();
            meshes.AddRange(GetComponentsInChildren<MeshRenderer>(true));
            meshes.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>(true));
            playerMeshes = meshes.ToArray();

            //remember every material and its color, so the fade knows where to come back to.
            originalMaterials = new Material[playerMeshes.Length][];
            originalColors = new Color[playerMeshes.Length][];

            for (int i = 0; i < playerMeshes.Length; i++)
            {
                originalMaterials[i] = playerMeshes[i].sharedMaterials;
                originalColors[i] = new Color[originalMaterials[i].Length];

                for (int slot = 0; slot < originalMaterials[i].Length; slot++)
                {
                    originalColors[i][slot] = GetMainColor(originalMaterials[i][slot]);
                }
            }

            tintBlock = new MaterialPropertyBlock();
            emptyBlock = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            bool bodyHidden = playerMovement.IsBodyHidden;

            if (bodyHidden == isShowingSilhouette)
                return;

            isShowingSilhouette = bodyHidden;

            if (bodyHidden)
            {
                FadeTo(1f);
            }
            else
            {
                //put the normal materials back first, still fully tinted, so the fade back starts from dark.
                SwapToSilhouetteMaterial(false);
                FadeTo(0f);
            }
        }

        void OnDisable()
        {
            //snap straight back to the normal look.
            if (fadeTween != null && fadeTween.IsActive())
                fadeTween.Kill();

            isShowingSilhouette = false;
            SwapToSilhouetteMaterial(false);
            silhouetteAmount = 0f;
            ApplyTint();
        }

        void OnDestroy()
        {
            DOTween.Kill(this);
        }

        /// <summary>
        /// Starts a fade from wherever the silhouette is right now, so a quick change of mind fades smoothly back.
        /// </summary>
        private void FadeTo(float targetAmount)
        {
            if (fadeTween != null && fadeTween.IsActive())
                fadeTween.Kill();

            fadeTween = DOTween.To(() => silhouetteAmount, value => { silhouetteAmount = value; ApplyTint(); }, targetAmount, fadeDuration)
                .SetEase(fadeEase)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    if (isShowingSilhouette)
                        SwapToSilhouetteMaterial(true);
                });
        }

        /// <summary>
        /// Tints every material slot part of the way from its own color to the silhouette color.
        /// </summary>
        private void ApplyTint()
        {
            if (isUsingSilhouetteMaterial)
                return;

            for (int i = 0; i < playerMeshes.Length; i++)
            {
                if (playerMeshes[i] == null)
                    continue;

                for (int slot = 0; slot < originalColors[i].Length; slot++)
                {
                    if (silhouetteAmount <= 0f)
                    {
                        //an empty block removes the tint completely.
                        playerMeshes[i].SetPropertyBlock(emptyBlock, slot);
                        continue;
                    }

                    Color tintedColor = Color.Lerp(originalColors[i][slot], silhouetteColor, silhouetteAmount);
                    tintBlock.SetColor(BaseColorId, tintedColor);
                    tintBlock.SetColor(ColorId, tintedColor);
                    playerMeshes[i].SetPropertyBlock(tintBlock, slot);
                }
            }
        }

        /// <summary>
        /// Swaps every mesh to the silhouette material, or back to its own materials.
        /// Does nothing when no silhouette material is assigned.
        /// </summary>
        private void SwapToSilhouetteMaterial(bool useSilhouetteMaterial)
        {
            if (silhouetteMaterial == null || useSilhouetteMaterial == isUsingSilhouetteMaterial)
                return;

            isUsingSilhouetteMaterial = useSilhouetteMaterial;

            for (int i = 0; i < playerMeshes.Length; i++)
            {
                Renderer playerMesh = playerMeshes[i];

                if (playerMesh == null)
                    continue;

                if (useSilhouetteMaterial)
                {
                    for (int slot = 0; slot < originalMaterials[i].Length; slot++)
                    {
                        playerMesh.SetPropertyBlock(emptyBlock, slot);
                    }

                    Material[] silhouetteMaterials = new Material[originalMaterials[i].Length];
                    for (int slot = 0; slot < silhouetteMaterials.Length; slot++)
                    {
                        silhouetteMaterials[slot] = silhouetteMaterial;
                    }

                    playerMesh.sharedMaterials = silhouetteMaterials;
                }
                else
                {
                    playerMesh.sharedMaterials = originalMaterials[i];
                }
            }

            if (!useSilhouetteMaterial)
                ApplyTint();
        }

        private static Color GetMainColor(Material material)
        {
            if (material == null)
                return Color.white;

            if (material.HasProperty(BaseColorId))
                return material.GetColor(BaseColorId);

            if (material.HasProperty(ColorId))
                return material.GetColor(ColorId);

            return Color.white;
        }
    }
}
