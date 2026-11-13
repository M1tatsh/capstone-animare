using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Gameplay.Player
{
    [RequireComponent(typeof(RadialSelection))]
    public class RadialMenuIcons : MonoBehaviour
    {
        [System.Serializable]
        public class SliceIcon
        {
            public Sprite icon;
            public PlayerForm form = PlayerForm.Fox;
        }

        private const float CenterSelectionRadius = 60f;

        [Header("Icons")]
        [SerializeField] private Sprite centerIcon;
        [SerializeField] private List<SliceIcon> sliceIcons = new List<SliceIcon>();

        [Header("Layout")]
        [SerializeField] private float iconDistance = 150f;
        [SerializeField] private Vector2 iconSize = new Vector2(72f, 72f);
        [SerializeField] private Vector2 centerIconSize = new Vector2(64f, 64f);
        [SerializeField] private bool preserveAspect = true;

        [Header("Look")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        [SerializeField] private float selectedScale = 1.25f;

        private RadialSelection radialSelection;
        private PlayerFormManager formManager;

        private RectTransform iconContainer;
        private Image centerImage;
        private List<Vector2> sliceIconPositions = new List<Vector2>();
        private List<Transform> sliceTransforms = new List<Transform>();
        private readonly List<Image> sliceImages = new List<Image>();
        private int builtSliceCount = -1;

        void Awake()
        {
            radialSelection = GetComponent<RadialSelection>();
        }

        void LateUpdate()
        {
            if (radialSelection.canvasTransform == null || !radialSelection.IsMenuOpen)
                return;

            if (iconContainer == null || builtSliceCount != radialSelection.numberOfRadialPart)
                BuildIcons();

            iconContainer.position = radialSelection.canvasTransform.position;
            iconContainer.SetAsLastSibling();

            if (formManager == null)
                formManager = FindAnyObjectByType<PlayerFormManager>();

            FindSliceTransforms();
            UpdateIcons(FindHoveredSlot());
        }

        private void BuildIcons()
        {
            if (iconContainer != null)
                Destroy(iconContainer.gameObject);

            sliceImages.Clear();
            sliceIconPositions.Clear();

            GameObject containerObject = new GameObject("RadialIcons", typeof(RectTransform));

            
            iconContainer = containerObject.GetComponent<RectTransform>();
            iconContainer.SetParent(radialSelection.canvasTransform, false);
            iconContainer.anchorMin = new Vector2(0.5f, 0.5f);
            iconContainer.anchorMax = new Vector2(0.5f, 0.5f);
            iconContainer.sizeDelta = Vector2.zero;
            iconContainer.localRotation = Quaternion.identity;
            iconContainer.localScale = Vector3.one;

            centerImage = CreateIconImage("CenterIcon", centerIcon, centerIconSize, Vector2.zero);

            int sliceCount = radialSelection.numberOfRadialPart;

            for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
            {
                float middleAngle = (sliceIndex + 0.5f) * 360f / sliceCount * Mathf.Deg2Rad;
                Vector2 position = new Vector2(Mathf.Sin(middleAngle), Mathf.Cos(middleAngle)) * iconDistance;

                Sprite sprite = sliceIndex < sliceIcons.Count ? sliceIcons[sliceIndex].icon : null;
                sliceImages.Add(CreateIconImage("SliceIcon" + (sliceIndex + 1), sprite, iconSize, position));
                sliceIconPositions.Add(position);
            }
            builtSliceCount = sliceCount;
        }

        private Image CreateIconImage(string objectName, Sprite sprite, Vector2 size, Vector2 position)
        {
            GameObject iconObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            RectTransform iconTransform = iconObject.GetComponent<RectTransform>();
            iconTransform.SetParent(iconContainer, false);
            iconTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconTransform.sizeDelta = size;
            iconTransform.anchoredPosition = position;

            Image image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            image.enabled = sprite != null;

            return image;
        }

        private void FindSliceTransforms()
        {
            sliceTransforms.Clear();
            Transform canvasTransform = radialSelection.canvasTransform;
            for (int childIndex = 0; childIndex < canvasTransform.childCount; childIndex++)
            {
                Transform child = canvasTransform.GetChild(childIndex);

                if (child == iconContainer)
                    continue;

                Image childImage = child.GetComponent<Image>();

                if (childImage != null && childImage.type == Image.Type.Filled)
                    sliceTransforms.Add(child);
            }

            int extraTransforms = sliceTransforms.Count - radialSelection.numberOfRadialPart;

            if (extraTransforms > 0)
                sliceTransforms.RemoveRange(0, extraTransforms);
        }

        private float GetSliceScale(int sliceIndex)
        {
            if (sliceIndex >= sliceTransforms.Count || sliceTransforms[sliceIndex] == null)
                return 1f;

            return sliceTransforms[sliceIndex].localScale.x;
        }

        private int FindHoveredSlot()
        {
            if (Mouse.current == null)
                return -1;

            Transform canvasTransform = radialSelection.canvasTransform;
            Vector3 centerToCursor = (Vector3)Mouse.current.position.ReadValue() - canvasTransform.position;
            Vector3 centerToCursorFlat = Vector3.ProjectOnPlane(centerToCursor, canvasTransform.forward);

            if (centerToCursorFlat.magnitude < CenterSelectionRadius)
                return 0;

            float angle = Vector3.SignedAngle(canvasTransform.up, centerToCursorFlat, -canvasTransform.forward);

            if (angle < 0f)
                angle += 360f;

            int slot = (int)(angle * radialSelection.numberOfRadialPart / 360f) + 1;

            if (slot > radialSelection.numberOfRadialPart)
                slot = 1;

            return slot;
        }

        private void UpdateIcons(int hoveredSlot)
        {
            if (centerImage != null)
            {
                centerImage.color = normalColor;
                centerImage.rectTransform.localScale = Vector3.one * (hoveredSlot == 0 ? selectedScale : 1f);
            }

            for (int sliceIndex = 0; sliceIndex < sliceImages.Count; sliceIndex++)
            {
                Image image = sliceImages[sliceIndex];
                bool isLocked = IsSliceLocked(sliceIndex);
                bool isHovered = hoveredSlot == sliceIndex + 1;

                image.color = isLocked ? lockedColor : normalColor;
                image.rectTransform.localScale = Vector3.one * (isHovered && !isLocked ? selectedScale : 1f);

                if (sliceIndex < sliceIconPositions.Count)
                    image.rectTransform.anchoredPosition = sliceIconPositions[sliceIndex] * GetSliceScale(sliceIndex);
            }
        }

        private bool IsSliceLocked(int sliceIndex)
        {
            if (formManager == null || sliceIndex >= sliceIcons.Count || sliceIcons[sliceIndex].icon == null)
                return false;

            return !formManager.IsFormUnlocked(sliceIcons[sliceIndex].form);
        }

        void OnDestroy()
        {
            if (iconContainer != null)
                Destroy(iconContainer.gameObject);
        }
    }
}
