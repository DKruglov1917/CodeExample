using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public abstract class StoreInterface : MonoBehaviour
{
    public delegate void storeIntefaceAction(Inventory inventory);
    public static event storeIntefaceAction OnFirearmEquip;
    public static event storeIntefaceAction OnFirearmUnequip;

    [HideInInspector] public Inventory inventory;
    [HideInInspector] public List<InterfaceSlot> slotsUnderTheGhostObj = new List<InterfaceSlot>();
    [HideInInspector] public List<InterfaceSlot> placeForAnItem        = new List<InterfaceSlot>();
    [HideInInspector] public List<InterfaceSlot> interfaceSlots;

    [SerializeField]  protected GameObject SlotsParentObj;

    #region Ghost object properties

    [SerializeField] protected GameObject _ghostObjPrefab;

    protected Canvas _canvas;
    protected GameObject _ghostObject;
    protected RectTransform _ghostObjRectTransform;
    protected GraphicRaycaster _graphicRaycaster;
    protected StoreInterface _targetStoreInterface;

    protected List<InterfaceSlot> _slotsToUpdate = new List<InterfaceSlot>();
    
    protected bool _canPlaceInSlot;
    protected bool _canStack;
    protected bool _areStoresSame;


    private Vector2 _ghostObjOriginSize;

    #endregion    

    protected void InitializeAsSI()
    {
        _canvas = GetComponentInParent<Canvas>();
        _graphicRaycaster = _canvas.GetComponent<GraphicRaycaster>();

        interfaceSlots.Clear();

        for (int i = 0; i < SlotsParentObj.transform.childCount; i++)
        {
            interfaceSlots.Add(SlotsParentObj.transform.GetChild(i).GetComponent<InterfaceSlot>());
        }
    }

    protected void UpdateSlots(List<InterfaceSlot> interfaceSlots, InventorySlot inventorySlot)
    {
        if (interfaceSlots.Count == 1)
        {
            interfaceSlots[0].inventorySlot = inventorySlot;
            UpdateSlotVisual(interfaceSlots[0], true);

            if (interfaceSlots[0] is InventoryInterfaceSlot)
                (interfaceSlots[0] as InventoryInterfaceSlot).isThisHeadSlot = true;

            if (interfaceSlots[0] is EquipmentInterfaceSlot)
                if ((interfaceSlots[0] as EquipmentInterfaceSlot).allowedType == ItemEquipment.ItemType.FIREARMS)
                    (interfaceSlots[0].storeInterface.inventory as UnicellularInventory).hasFirearm = true;
        }
        else
        {
            for (int i = 0; i < interfaceSlots.Count; i++)
            {               
                interfaceSlots[i].inventorySlot = inventorySlot;

                foreach (var slot in interfaceSlots)
                {
                    if (interfaceSlots[i] != slot)
                        interfaceSlots[i].relatedSlots.Add(slot);
                }

                if (i == interfaceSlots.Count - 1)
                {
                    if (interfaceSlots[i] is InventoryInterfaceSlot)
                        (interfaceSlots[i] as InventoryInterfaceSlot).isThisHeadSlot = true;

                    UpdateSlotVisual(interfaceSlots[i], false);
                }
            }
        }
    }

    protected void UpdateSlotVisual(InterfaceSlot headSlot, bool isOneCellItem)
    {
        if (isOneCellItem)
        {
            headSlot.transform.GetChild(1).GetComponent<Image>().sprite = headSlot.inventorySlot.item.sprite;
            headSlot.transform.GetChild(1).GetComponent<Image>().color = new Color(255, 255, 255, 1);
            
        }
        else
        {
            var imageObj = headSlot.transform.GetChild(1);
            var image = imageObj.GetComponent<Image>();
            var imageObjRectTrans = imageObj.GetComponent<RectTransform>();
            var xScale = headSlot.inventorySlot.item.sizeX;
            var yScale = headSlot.inventorySlot.item.sizeY;

            if (image.sprite == null)
            {
                image.sprite = headSlot.inventorySlot.item.sprite;
                image.color = new Color(255, 255, 255, 1);
                imageObj.localScale = new Vector2(xScale, yScale);

                for (int j = 1; j < xScale; j++)
                {
                    imageObjRectTrans.localPosition = new Vector2(imageObjRectTrans.localPosition.x - 32, imageObjRectTrans.localPosition.y);
                }

                for (int j = 1; j < yScale; j++)
                {
                    imageObjRectTrans.localPosition = new Vector2(imageObjRectTrans.localPosition.x, imageObjRectTrans.localPosition.y + 32);
                }
            }
            
            if (!imageObj.GetComponent<Canvas>())
            {
                Canvas imageCanvas = imageObj.gameObject.AddComponent<Canvas>();
                imageCanvas.sortingOrder = 1;
            }
        }

        if (headSlot.storeInterface is InventoryInterface && headSlot.inventorySlot.item.stackable)
        {
            headSlot.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = headSlot.inventorySlot.amount.ToString();
        }
    }

    protected void ClearSingleSlot(InterfaceSlot interfaceSlot, bool wasThisSlotUpdatedLast)
    {
        if(interfaceSlot is EquipmentInterfaceSlot)
        {
            var equipmentSlot = interfaceSlot as EquipmentInterfaceSlot;
            interfaceSlot.transform.GetChild(1).GetComponent<Image>().sprite = equipmentSlot.sprite;
            interfaceSlot.transform.GetChild(1).GetComponent<Image>().color = new Color(0, 0, 0, .5f);

            if(!(this as EquipmentInterface).windowIsClosingNow)
                if((interfaceSlot as EquipmentInterfaceSlot).allowedType == ItemEquipment.ItemType.FIREARMS)
                    (inventory as UnicellularInventory).hasFirearm = false;

            if (this is EquipmentInterface)
            {
                if(!(this as EquipmentInterface).windowIsClosingNow)
                    (this as EquipmentInterface).RemoveCharacterAttribute(interfaceSlot.inventorySlot.item);
            }
        }
        else if(interfaceSlot is InventoryInterfaceSlot)
        {
            (interfaceSlot as InventoryInterfaceSlot).isThisHeadSlot = false;

            interfaceSlot.transform.GetChild(1).GetComponent<Image>().sprite = null;
            interfaceSlot.transform.GetChild(1).GetComponent<Image>().color = new Color(255, 255, 255, 0);
            interfaceSlot.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = null;
        }        

        if (wasThisSlotUpdatedLast)
        {
            var imageObj = interfaceSlot.transform.GetChild(1);
            var imageObjRectTrans = imageObj.GetComponent<RectTransform>();

            var xScale = interfaceSlot.inventorySlot.item.sizeX;
            var yScale = interfaceSlot.inventorySlot.item.sizeY;
            imageObj.localScale = Vector2.one;

            for (int j = 1; j < xScale; j++)
            {
                imageObjRectTrans.localPosition = new Vector2(imageObjRectTrans.localPosition.x + 32, imageObjRectTrans.localPosition.y);
            }
            for (int j = 1; j < yScale; j++)
            {
                imageObjRectTrans.localPosition = new Vector2(imageObjRectTrans.localPosition.x, imageObjRectTrans.localPosition.y - 32);
            }

            Destroy(imageObj.GetComponent<Canvas>());
        }

        interfaceSlot.relatedSlots.Clear();
        interfaceSlot.inventorySlot = new InventorySlot();
    }

    protected void CreateGhostObject(PointerEventData eventData, InterfaceSlot interfaceSlot)
    {
        InventorySlot inventorySlot = interfaceSlot.inventorySlot;
        _ghostObjOriginSize = new Vector2(inventorySlot.item.sizeX, inventorySlot.item.sizeY);

        _ghostObject = Instantiate(_ghostObjPrefab, SlotsParentObj.transform);
        _ghostObject.GetComponent<Image>().color = Color.red;
        _ghostObject.transform.localScale = _ghostObjOriginSize;
        _ghostObjRectTransform = _ghostObject.GetComponent<RectTransform>();

        _ghostObjRectTransform.position = eventData.position;

        _ghostObject.GetComponent<Image>().sprite = inventorySlot.item.sprite;
        _ghostObject.transform.SetParent(_canvas.transform);
    }

    protected void DestroyGhostObject()
    {
        if (!_ghostObject) return;

        Destroy(_ghostObject);
        _ghostObjRectTransform = null;
    }

    protected void DragGhostObject(PointerEventData eventData, InventorySlot inventorySlot)
    {
        #region variables

        _ghostObjRectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;

        PointerEventData ped = new PointerEventData(null);
        List<RaycastResult> results = new List<RaycastResult>();

        Vector3[] corners = new Vector3[4];
        _ghostObjRectTransform.GetWorldCorners(corners);

        float distX = Vector3.Distance(corners[1], corners[2]);

        float step = distX / _ghostObjRectTransform.localScale.x / 2;

        float sizeX = _ghostObjRectTransform.localScale.x;
        float sizeY = _ghostObjRectTransform.localScale.y;
        float size = sizeX * sizeY;

        int dotsSignalizedTrue = 0;
        int dotsSignalizedTheSameItem = 0;

        bool isThisStoreEquipment = false;
        bool isThisStoreInventory = false;

        #endregion        

        FindSlotsUnderGhostObj();

        AssignGhostObjProperties();

        ManageGhostObjSize();

        SetGhostObjColor();

        #region GhostObjMethods

        void FindSlotsUnderGhostObj()
        {
            slotsUnderTheGhostObj.Clear();

            //in the clockwise direction from the lower left corner
            for (int j = 0; j < sizeY; j++)
            {
                Vector2 startDot = new Vector2();

                if (j == sizeY - 1)
                {
                    startDot = new Vector2(corners[0].x + step, corners[0].y + step);
                }
                else
                {
                    startDot = new Vector2(corners[1].x + step, corners[1].y - step - (step * 2 * j));
                }

                for (int k = 0; k < sizeX; k++)
                {
                    if (k == 0)
                    {
                        ped.position = startDot;
                    }
                    else
                    {
                        ped.position = new Vector2(ped.position.x + step * 2, startDot.y);
                    }
                    _graphicRaycaster.Raycast(ped, results);
                }
            }

            foreach (var item in results)
            {
                if (item.gameObject.TryGetComponent(out InterfaceSlot slot))
                {
                    if (slotsUnderTheGhostObj.Contains(slot)) return;

                    if (slot.inventorySlot.item == null)
                    {
                        dotsSignalizedTrue += 1;
                        slotsUnderTheGhostObj.Add(slot);
                    }
                    if (slot.inventorySlot.item == inventorySlot.item && slot.inventorySlot != inventorySlot)
                    {
                        dotsSignalizedTheSameItem += 1;
                        slotsUnderTheGhostObj.Add(slot);
                    }
                }
            }
            results.Clear();
        }

        void ManageGhostObjSize()
        {            
            foreach (var slot in slotsUnderTheGhostObj)
            {
                if (slot.GetComponentInParent<EquipmentInterface>())
                {
                    isThisStoreEquipment = true;
                }
                else
                {
                    isThisStoreEquipment = false;
                }
                if (!isThisStoreEquipment)
                {
                    if (slot.GetComponentInParent<InventoryInterface>())
                    {
                        isThisStoreInventory = true;
                    }
                    else
                    {
                        isThisStoreInventory = false;
                    }
                }
            }

            if (isThisStoreEquipment)
            {
                _ghostObjRectTransform.localScale = new Vector2(1, 1);
            }
            else if(isThisStoreInventory)
            {
                _ghostObjRectTransform.localScale = _ghostObjOriginSize;
            }
        }

        void AssignGhostObjProperties()
        {
            for (int i = 0; i < slotsUnderTheGhostObj.Count; i++)
            {
                if (slotsUnderTheGhostObj[i].storeInterface.inventory == inventory)
                {
                    _areStoresSame = true;
                }
                else
                {
                    _areStoresSame = false;
                }
            }

            if (slotsUnderTheGhostObj.Count > 0)
            {
                if (!_areStoresSame)
                {
                    _targetStoreInterface = slotsUnderTheGhostObj[0].storeInterface;
                }
                else
                {
                    _targetStoreInterface = this;
                }
            }

            if (_targetStoreInterface is InventoryInterface)
            {
                if (inventorySlot.item.stackable)
                {
                    if (dotsSignalizedTheSameItem == size)
                    {
                        _canStack = true;
                    }
                    else
                    {
                        _canStack = false;
                    }
                }
            }

            CanPlaceInSlot();
            
            void CanPlaceInSlot()
            {
                if (dotsSignalizedTrue == size)
                {
                    if (inventorySlot.item is ItemEquipment)
                    {
                        foreach (var slot in slotsUnderTheGhostObj)
                        {
                            if (slot is EquipmentInterfaceSlot)
                            {
                                var slotEquipment = slot as EquipmentInterfaceSlot;
                                var itemEquipment = inventorySlot?.item as ItemEquipment;
                                if (itemEquipment.itemType == slotEquipment.allowedType)
                                {
                                    _canPlaceInSlot = true;
                                }
                            }

                            else if (slot is InventoryInterfaceSlot)
                            {
                                _canPlaceInSlot = true;
                            }

                            else
                            {
                                _canPlaceInSlot = false;
                            }
                        }
                    }
                    else if( inventorySlot.item is ItemInventory)
                    {
                        foreach (var slot in slotsUnderTheGhostObj)
                        {
                            if (slot is EquipmentInterfaceSlot)
                            {
                                _canPlaceInSlot = false;
                            }

                            else if (slot is InventoryInterfaceSlot)
                            {
                                _canPlaceInSlot = true;
                            }

                            else
                            {
                                _canPlaceInSlot = false;
                            }
                        }
                    }
                }
                else
                {
                    _canPlaceInSlot = false;
                }
            }
        }

        void SetGhostObjColor()
        {
            if (_canPlaceInSlot)
            {
                _ghostObject.GetComponent<Image>().color = Color.white;                
            }
            else if (_canStack)
            {
                _ghostObject.GetComponent<Image>().color = Color.cyan;
            }
            else
            {
                _ghostObject.GetComponent<Image>().color = Color.red;
            }
        }

        #endregion
    }

    protected void EndDragGhostObject(PointerEventData eventData, InterfaceSlot interfaceSlot)
    {
        InventorySlot _inventorySlot = interfaceSlot.inventorySlot;        

        if (_canPlaceInSlot && eventData.pointerEnter)
        {
            if (!_areStoresSame)
            {
                _targetStoreInterface.inventory.slots.Add(_inventorySlot);
                inventory.slots.Remove(_inventorySlot);
            }

            if (_inventorySlot.item.sizeX == 1 && _inventorySlot.item.sizeY == 1)
            {
                _slotsToUpdate.Add(eventData.pointerEnter.transform.GetComponentInParent<InterfaceSlot>());
                UpdateSlots(_slotsToUpdate, _inventorySlot);
            }
            else
            {
                UpdateSlots(slotsUnderTheGhostObj, _inventorySlot);
            }
        }
        else if (_canStack)
        {
            inventory.AddAmountOfItems(eventData.pointerEnter.transform.GetComponentInParent<InterfaceSlot>().inventorySlot, _inventorySlot.amount);
            UpdateSlotVisual(slotsUnderTheGhostObj.Find(x => (x as InventoryInterfaceSlot).isThisHeadSlot == true), false);
            inventory.RemoveSlot(_inventorySlot);
        }

        if (_canPlaceInSlot || _canStack)
        {
            if (_targetStoreInterface is EquipmentInterface)
            {
                (_targetStoreInterface as EquipmentInterface).AddCharacterAttribites(_inventorySlot.item);
            }

            if (interfaceSlot.relatedSlots.Count > 0)
            {
                InterfaceSlot _headSlot;

                if (interfaceSlot.transform.GetChild(1).GetComponent<Image>().sprite != null)
                    _headSlot = interfaceSlot;
                else
                    _headSlot = interfaceSlot.relatedSlots.Find((x) => x.transform.GetChild(1).GetComponent<Image>().sprite != null);               

                foreach (var slot in _headSlot?.relatedSlots)
                {
                    ClearSingleSlot(slot, false);
                    slot.relatedSlots.Clear();
                }

                ClearSingleSlot(_headSlot, true);
                _headSlot.relatedSlots.Clear();
            }
            else
            {
                ClearSingleSlot(interfaceSlot, false);
            }

            _slotsToUpdate.Clear();
        }
    }

    protected void OpenItemSubmenu(PointerEventData eventData, InterfaceSlot interfaceSlot)
    {
        if (this is InventoryInterface)
        {
            (this as InventoryInterface).itemSubmenu.Activate(eventData, interfaceSlot);
        }            
    }

    protected void RemoveItem(InterfaceSlot interfaceSlot)
    {
        inventory.RemoveSlot(interfaceSlot.inventorySlot);

        if (this is InventoryInterface)
        {
            if (interfaceSlot.relatedSlots.Count > 0)
            {
                var _headSlot = (this as InventoryInterface).FindHeadSlot(interfaceSlot);

                foreach (var slot in _headSlot.relatedSlots)
                {
                    ClearSingleSlot(slot, false);
                }

                ClearSingleSlot(_headSlot, true);
            }
            else
            {
                ClearSingleSlot(interfaceSlot, false);
            }
        }
        else
        {
            ClearSingleSlot(interfaceSlot, false);
        }
    }    

    protected void SubscribeToSlotEvents()
    {
        foreach (var slot in interfaceSlots)
        {
            slot.onGhostObjCreation += CreateGhostObject;
            slot.onGhostObjDestruction += DestroyGhostObject;
            slot.onGhostObjectDrag += DragGhostObject;
            slot.onGhostObjectEndDrag += EndDragGhostObject;

            if (this is InventoryInterface)
                slot.onOpenSubmenu += OpenItemSubmenu;
        }

        if (this is InventoryInterface)
            (this as InventoryInterface).itemSubmenu.onRemove += RemoveItem;
    }

    protected void UnsubscribeFromSlotEvents()
    {
        foreach (var slot in interfaceSlots)
        {
            slot.onGhostObjCreation -= CreateGhostObject;
            slot.onGhostObjDestruction -= DestroyGhostObject;
            slot.onGhostObjectDrag -= DragGhostObject;
            slot.onGhostObjectEndDrag -= EndDragGhostObject;

            if (this is InventoryInterface)
                slot.onOpenSubmenu -= OpenItemSubmenu;
        }

        if (this is InventoryInterface)
            (this as InventoryInterface).itemSubmenu.onRemove -= RemoveItem;
    }

    private void OnDestroy()
    {
        UnsubscribeFromSlotEvents();
    }
}
