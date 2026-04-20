using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// 挂在 cell_container 上，接收 UGUI 触摸/鼠标事件并转发给 DragController。
    /// 支持移动端多点触控（单指拖拽）。
    /// </summary>
    public class GridInputHandler : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private DragController _dragController;

        public void Setup(DragController dragController)
        {
            _dragController = dragController;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _dragController?.OnPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _dragController?.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _dragController?.OnPointerUp(eventData);
        }
    }
}
