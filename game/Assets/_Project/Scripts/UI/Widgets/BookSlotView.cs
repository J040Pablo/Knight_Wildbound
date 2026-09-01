using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Roguelite.Items;
using Roguelite.Inventory;

namespace Roguelite.UI.Widgets
{
    /// <summary>
    /// Component attached to each UI slot in the Ancient RPG Book interface.
    /// Handles pointer hover, left/right clicks, drag initiation, dragging, dropping,
    /// and visual updates (icon, quantity count, rarity frame).
    /// </summary>
    public class BookSlotView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        [Header("Slot Configuration")]
        public bool isEquipSlot = false;
        public int gridIndex = -1;
        public EquipmentSlot equipSlot = EquipmentSlot.Weapon;

        [Header("Visual Child References")]
        public Image bgFrame;
        public Image borderFrame;
        public Image iconImage;
        public TextMeshProUGUI glyphText;
        public TextMeshProUGUI quantityText;
        public Image slotCategoryWatermark;

        [HideInInspector] public InventoryBookUI owner;

        public void SetOwner(InventoryBookUI bookUI)
        {
            owner = bookUI;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.OnSlotPointerEnter(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.OnSlotPointerExit(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                owner?.OnSlotLeftClick(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                owner?.OnSlotRightClick(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.OnSlotBeginDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.OnSlotDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.OnSlotEndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            owner?.OnSlotDrop(this);
        }
    }
}
